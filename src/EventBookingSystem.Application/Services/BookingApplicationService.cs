using EventBookingSystem.Application.DTOs;
using EventBookingSystem.Application.Models;
using EventBookingSystem.Domain;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;
using EventBookingSystem.Infrastructure.Interfaces;

namespace EventBookingSystem.Application.Services
{
    /// <summary>
    /// Application service that orchestrates booking operations.
    /// Follows SOLID principles by delegating to specialized services and validators.
    /// </summary>
    public class BookingApplicationService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IUserRepository _userRepository;
        private readonly IVenueRepository _venueRepository;
        private readonly IBookingService _bookingService;
        private readonly IEnumerable<IBookingCommandValidator> _commandValidators;

        public BookingApplicationService(
            IBookingRepository bookingRepository,
            IEventRepository eventRepository,
            IUserRepository userRepository,
            IVenueRepository venueRepository,
            IBookingService bookingService,
            IEnumerable<IBookingCommandValidator> commandValidators = null)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _userRepository = userRepository;
            _venueRepository = venueRepository;
            _bookingService = bookingService;
            _commandValidators = commandValidators ?? Enumerable.Empty<IBookingCommandValidator>();
        }

        /// <summary>
        /// Creates a booking from a command.
        /// Orchestrates the workflow: validate → load → create → persist → map.
        /// </summary>
        public async Task<BookingResultDto> CreateBookingAsync(CreateBookingCommand command)
        {
            // 1. Validate command (application-level validation)
            var commandValidation = await ValidateCommandAsync(command);
            if (!commandValidation.IsValid)
            {
                return BookingResultDto.Failure(commandValidation.ErrorMessage);
            }

            // 2. Load required entities
            var loadResult = await LoadRequiredEntitiesAsync(command);
            if (!loadResult.IsSuccess)
            {
                return BookingResultDto.Failure(loadResult.ErrorMessage);
            }

            // 3. Build domain request
            var request = BuildReservationRequest(command);

            // 4. Validate booking (domain-level validation)
            var domainValidation = _bookingService.ValidateBooking(
                loadResult.User, 
                loadResult.Event, 
                request);
            
            if (!domainValidation.IsValid)
            {
                return BookingResultDto.Failure(domainValidation.ErrorMessage);
            }

            // 5. Create booking (domain logic)
            var booking = _bookingService.CreateBooking(
                loadResult.User, 
                loadResult.Event, 
                request);

           

            // 6. Persist changes (infrastructure)
            var persistResult = await PersistBookingAsync(booking, loadResult.Event);
            if (!persistResult.IsSuccess)
            {
                return BookingResultDto.Failure(persistResult.ErrorMessage);
            }

            // 7. Map to DTO
            return MapToResultDto(booking);
        }

        /// <summary>
        /// Validates the command using registered validators.
        /// Follows OCP - new validators can be added without modifying this class.
        /// </summary>
        private async Task<ValidationResult> ValidateCommandAsync(CreateBookingCommand command)
        {
            foreach (var validator in _commandValidators)
            {
                var result = await validator.ValidateAsync(command);
                if (!result.IsValid)
                {
                    return result;
                }
            }
            return ValidationResult.Success();
        }

        /// <summary>
        /// Loads all required entities for the booking.
        /// Follows SRP - single responsibility of entity loading.
        /// </summary>
        private async Task<EntityLoadResult> LoadRequiredEntitiesAsync(CreateBookingCommand command)
        {
            var user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
            {
                return EntityLoadResult.Failure("User not found");
            }

            var evnt = await _eventRepository.GetByIdAsync(command.EventId);
            if (evnt == null)
            {
                return EntityLoadResult.Failure("Event not found");
            }

            var venue = await _venueRepository.GetByIdAsync(evnt.VenueId);
            if (venue == null)
            {
                return EntityLoadResult.Failure("Venue not found");
            }

            return EntityLoadResult.Success(user, evnt, venue);
        }

        /// <summary>
        /// Builds the reservation request from the command.
        /// Follows SRP - single responsibility of mapping.
        /// </summary>
        private static ReservationRequest BuildReservationRequest(CreateBookingCommand command)
        {
            return new ReservationRequest
            {
                Quantity = command.Quantity,
                CustomerId = command.UserId,
                SectionId = command.SectionId,
                SeatId = command.SeatId,
            };
        }

        /// <summary>
        /// Persists the booking and updates the event.
        /// Follows SRP - single responsibility of persistence.
        /// TODO: Wrap in transaction (Unit of Work pattern).
        /// </summary>
        private async Task<PersistenceResult> PersistBookingAsync(Booking booking, EventBase evnt)
        {
            try
            {
                var bookingResult = await _bookingRepository.AddAsync(booking);
                if (bookingResult == null)
                {
                    return PersistenceResult.Failure("Failed to create booking");
                }

                var eventResult = await _eventRepository.UpdateAsync(evnt);
                if (eventResult == null)
                {
                    return PersistenceResult.Failure("Failed to update event after booking");
                }

                return PersistenceResult.Success();
            }
            catch (Exception ex)
            {
                return PersistenceResult.Failure($"Persistence error: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps a domain Booking to a BookingResultDto.
        /// Follows SRP - single responsibility of DTO mapping.
        /// </summary>
        private static BookingResultDto MapToResultDto(Booking booking)
        {
            return new BookingResultDto
            {
                IsSuccessful = true,
                BookingId = booking.Id,
                TotalAmount = booking.TotalAmount,
                Message = "Booking created successfully"
            };
        }

        /// <summary>
        /// Internal result type for entity loading.
        /// Encapsulates success/failure with loaded entities.
        /// </summary>
        private class EntityLoadResult
        {
            public bool IsSuccess { get; init; }
            public string ErrorMessage { get; init; } = string.Empty;
            public User User { get; init; } = null!;
            public EventBase Event { get; init; } = null!;
            public Venue Venue { get; init; } = null!;

            public static EntityLoadResult Success(User user, EventBase evnt, Venue venue)
            {
                return new EntityLoadResult
                {
                    IsSuccess = true,
                    User = user,
                    Event = evnt,
                    Venue = venue
                };
            }

            public static EntityLoadResult Failure(string errorMessage)
            {
                return new EntityLoadResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }
        }

        /// <summary>
        /// Internal result type for persistence operations.
        /// </summary>
        private class PersistenceResult
        {
            public bool IsSuccess { get; init; }
            public string ErrorMessage { get; init; } = string.Empty;

            public static PersistenceResult Success()
            {
                return new PersistenceResult { IsSuccess = true };
            }

            public static PersistenceResult Failure(string errorMessage)
            {
                return new PersistenceResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }
        }
    }

    /// <summary>
    /// Interface for command-level validators.
    /// Follows ISP - focused interface for command validation.
    /// Follows OCP - new validators can be added without modifying existing code.
    /// </summary>
    public interface IBookingCommandValidator
    {
        Task<ValidationResult> ValidateAsync(CreateBookingCommand command);
    }
}
