using EventBookingSystem.Application.DTOs;
using EventBookingSystem.Application.Interfaces;
using EventBookingSystem.Application.Models;
using EventBookingSystem.Domain;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;

namespace EventBookingSystem.Application.Services
{
    /// <summary>
    /// Application service that orchestrates booking operations.
    /// Follows SOLID principles by delegating to specialized services and validators.
    /// </summary>
    public class BookingApplicationService : IBookingApplicationService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IUserRepository _userRepository;
        private readonly IVenueRepository _venueRepository;
        private readonly IBookingService _bookingService;
        private readonly IPaymentService _paymentService;
        private readonly IEnumerable<IBookingCommandValidator> _commandValidators;

        public BookingApplicationService(
            IBookingRepository bookingRepository,
            IEventRepository eventRepository,
            IUserRepository userRepository,
            IVenueRepository venueRepository,
            IBookingService bookingService,
            IPaymentService paymentService,
            IEnumerable<IBookingCommandValidator> commandValidators = null)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _userRepository = userRepository;
            _venueRepository = venueRepository;
            _bookingService = bookingService;
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _commandValidators = commandValidators ?? Enumerable.Empty<IBookingCommandValidator>();
        }

        /// <summary>
        /// Creates a booking from a command.
        /// Orchestrates the workflow: validate → load → create → process payment → persist → map.
        /// </summary>
        public async Task<BookingResultDto> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken = default)
        {
            // 1. Validate command (application-level validation)
            var commandValidation = await ValidateCommandAsync(command);
            if (!commandValidation.IsValid)
            {
                return BookingResultDto.Failure(commandValidation.ErrorMessage);
            }

            // 2. Load required entities
            var loadResult = await LoadRequiredEntitiesAsync(command, cancellationToken);
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

            // 6. Process payment BEFORE persisting the booking
            var paymentResult = await ProcessPaymentAsync(booking, loadResult.Event, cancellationToken);
            if (!paymentResult.IsSuccessful)
            {
                // Payment failed - rollback the event reservations
                RollbackEventReservations(loadResult.Event, request);
                return BookingResultDto.Failure($"Payment failed: {paymentResult.ErrorMessage}");
            }

            // Update booking payment status
            booking.PaymentStatus = PaymentStatus.Paid;

            // 7. Persist changes (infrastructure) - only if payment succeeded
            var persistResult = await PersistBookingAsync(booking, loadResult.Event, cancellationToken);
            if (!persistResult.IsSuccess)
            {
                // NOTE: In production, this would require a compensating transaction to refund the payment
                return BookingResultDto.Failure($"Booking creation failed after payment: {persistResult.ErrorMessage}. Please contact support with transaction ID: {paymentResult.TransactionId}");
            }

            // 8. Map to DTO
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
        private async Task<EntityLoadResult> LoadRequiredEntitiesAsync(CreateBookingCommand command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
            if (user == null)
            {
                return EntityLoadResult.Failure("User not found");
            }

            // Use GetByIdWithDetailsAsync to load related data (Seats, SectionInventories, etc.)
            var evnt = await _eventRepository.GetByIdWithDetailsAsync(command.EventId, cancellationToken);
            if (evnt == null)
            {
                return EntityLoadResult.Failure("Event not found");
            }

            var venue = await _venueRepository.GetByIdAsync(evnt.VenueId, cancellationToken);
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
        /// Processes payment for the booking.
        /// Follows SRP - single responsibility of payment processing.
        /// </summary>
        private async Task<PaymentResult> ProcessPaymentAsync(Booking booking, EventBase evnt, CancellationToken cancellationToken)
        {
            var paymentRequest = new PaymentRequest
            {
                UserId = booking.User.Id,
                Amount = booking.TotalAmount,
                Description = $"Booking for {evnt.Name} on {evnt.StartsAt:yyyy-MM-dd}",
                PaymentMethod = "CreditCard"
            };

            return await _paymentService.ProcessPaymentAsync(paymentRequest, cancellationToken);
        }

        /// <summary>
        /// Rolls back event reservations if payment fails.
        /// This ensures event capacity is released if the booking cannot be completed.
        /// </summary>
        private void RollbackEventReservations(EventBase evnt, ReservationRequest request)
        {
            try
            {
                switch (evnt)
                {
                    case GeneralAdmissionEvent ga:
                        // Release tickets back to the event
                        // Note: GeneralAdmissionEvent doesn't have a ReleaseTickets method,
                        // so the rollback happens because we don't persist the event
                        break;

                    case SectionBasedEvent sb when request.SectionId.HasValue:
                        sb.ReleaseFromSection(request.SectionId.Value, request.Quantity);
                        break;

                    case ReservedSeatingEvent rs when request.SeatId.HasValue:
                        // For reserved seating, the seat status needs to be reverted
                        // Since ReserveSeat() changes status to Reserved, we need to handle rollback
                        var seat = rs.GetSeat(request.SeatId.Value);
                        if (seat != null && seat.Status == SeatStatus.Reserved)
                        {
                            // Manually revert the seat status for rollback
                            // This is a special case for payment failure
                            var eventSeat = rs.Seats.FirstOrDefault(s => s.VenueSeatId == request.SeatId.Value);
                            if (eventSeat != null)
                            {
                                eventSeat.Status = SeatStatus.Available;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log rollback failure but don't throw - payment already failed
                // In production, this would be logged for manual intervention
                System.Diagnostics.Debug.WriteLine($"Rollback failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Persists the booking and updates the event.
        /// Follows SRP - single responsibility of persistence.
        /// TODO: Wrap in transaction (Unit of Work pattern).
        /// </summary>
        private async Task<PersistenceResult> PersistBookingAsync(Booking booking, EventBase evnt, CancellationToken cancellationToken)
        {
            try
            {
                var bookingResult = await _bookingRepository.AddAsync(booking, cancellationToken);
                if (bookingResult == null)
                {
                    return PersistenceResult.Failure("Failed to create booking");
                }

                var eventResult = await _eventRepository.UpdateAsync(evnt, cancellationToken);
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
                Message = "Booking created successfully and payment processed"
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
