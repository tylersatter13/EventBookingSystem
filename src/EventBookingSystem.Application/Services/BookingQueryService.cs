using EventBookingSystem.Application.DTOs;
using EventBookingSystem.Application.Interfaces;

namespace EventBookingSystem.Application.Services
{
    /// <summary>
    /// Service for querying booking information.
    /// Orchestrates data retrieval from repositories and maps to DTOs.
    /// Follows SRP - single responsibility of reading booking data.
    /// Follows DIP - depends on repository abstractions.
    /// </summary>
    public class BookingQueryService : IBookingQueryService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;

        public BookingQueryService(
            IBookingRepository bookingRepository,
            IEventRepository eventRepository)
        {
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        }

        /// <summary>
        /// Retrieves all bookings for a specific user.
        /// </summary>
        public async Task<IEnumerable<BookingDto>> GetBookingsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("User ID must be greater than zero", nameof(userId));
            }

            var bookings = await _bookingRepository.GetByUserIdAsync(userId, cancellationToken);
            
            return bookings.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Retrieves all bookings for events at a specific venue.
        /// </summary>
        public async Task<IEnumerable<BookingDto>> GetBookingsByVenueIdAsync(int venueId, CancellationToken cancellationToken = default)
        {
            if (venueId <= 0)
            {
                throw new ArgumentException("Venue ID must be greater than zero", nameof(venueId));
            }

            // Get all events at the venue
            var events = await _eventRepository.GetByVenueIdAsync(venueId, cancellationToken);
            var eventIds = events.Select(e => e.Id).ToList();

            if (!eventIds.Any())
            {
                return Enumerable.Empty<BookingDto>();
            }

            // Load venue information once for all bookings
            var venue = events.FirstOrDefault()?.Venue;
            var venueName = venue?.Name ?? "Unknown Venue";

            // Get all bookings for those events
            var allBookings = new List<Domain.Entities.Booking>();
            foreach (var eventId in eventIds)
            {
                var eventBookings = await _bookingRepository.GetByEventIdAsync(eventId, cancellationToken);
                allBookings.AddRange(eventBookings);
            }

            // Map bookings and ensure venue name is set correctly
            return allBookings.Select(b => MapToDto(b, venueName)).ToList();
        }

        /// <summary>
        /// Retrieves a specific booking by its ID.
        /// </summary>
        public async Task<BookingDto?> GetBookingByIdAsync(int bookingId, CancellationToken cancellationToken = default)
        {
            if (bookingId <= 0)
            {
                throw new ArgumentException("Booking ID must be greater than zero", nameof(bookingId));
            }

            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            
            return booking != null ? MapToDto(booking) : null;
        }

        /// <summary>
        /// Finds bookings for users who have at least one successfully paid booking at the specified venue.
        /// Returns all bookings (paid and unpaid) for qualifying users at the specified venue.
        /// 
        /// Use cases:
        /// - Customer loyalty analysis
        /// - VIP customer identification
        /// - Revenue attribution and analysis
        /// - Support ticket prioritization
        /// </summary>
        public async Task<IEnumerable<BookingDto>> GetBookingsForPaidUsersAtVenueAsync(int venueId, CancellationToken cancellationToken = default)
        {
            if (venueId <= 0)
            {
                throw new ArgumentException("Venue ID must be greater than zero", nameof(venueId));
            }

            var bookings = await _bookingRepository.FindBookingsForPaidUsersAtVenueAsync(venueId, cancellationToken);
            
            return bookings.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Finds all user IDs who have no bookings whatsoever at the specified venue.
        /// 
        /// Use cases:
        /// - Target marketing campaigns to non-customers
        /// - Identify potential new customers for venue promotions
        /// - Customer acquisition analytics
        /// - Cold outreach campaigns
        /// </summary>
        public async Task<IEnumerable<int>> GetUsersWithoutBookingsAtVenueAsync(int venueId, CancellationToken cancellationToken = default)
        {
            if (venueId <= 0)
            {
                throw new ArgumentException("Venue ID must be greater than zero", nameof(venueId));
            }

            return await _bookingRepository.FindUsersWithoutBookingsInVenueAsync(venueId, cancellationToken);
        }

        /// <summary>
        /// Maps a domain Booking entity to a BookingDto.
        /// Follows SRP - single responsibility of DTO mapping.
        /// </summary>
        private static BookingDto MapToDto(Domain.Entities.Booking booking)
        {
            return new BookingDto
            {
                Id = booking.Id,
                UserId = booking.User.Id,
                UserName = booking.User.Name,
                UserEmail = booking.User.Email,
                EventId = booking.Event.Id,
                EventName = booking.Event.Name,
                EventStartsAt = booking.Event.StartsAt,
                VenueId = booking.Event.VenueId,
                VenueName = booking.Event.Venue?.Name ?? "Unknown Venue",
                BookingType = booking.BookingType.ToString(),
                PaymentStatus = booking.PaymentStatus.ToString(),
                TotalAmount = booking.TotalAmount,
                CreatedAt = booking.CreatedAt,
                BookingItems = booking.BookingItems?.Select(MapBookingItemToDto).ToList() ?? new List<BookingItemDto>()
            };
        }

        /// <summary>
        /// Maps a domain Booking entity to a BookingDto with explicit venue name.
        /// Overload used when venue name is known from context.
        /// </summary>
        private static BookingDto MapToDto(Domain.Entities.Booking booking, string venueName)
        {
            var dto = MapToDto(booking);
            dto.VenueName = venueName;
            return dto;
        }

        /// <summary>
        /// Maps a domain BookingItem entity to a BookingItemDto.
        /// </summary>
        private static BookingItemDto MapBookingItemToDto(Domain.Entities.BookingItem item)
        {
            return new BookingItemDto
            {
                Id = item.Id,
                EventSeatId = item.EventSeat?.Id,
                EventSectionInventoryId = item.EventSection?.Id,
                Quantity = item.Quantity,
                SeatLabel = item.EventSeat?.VenueSeat?.SeatLabel,
                SectionName = item.EventSection?.VenueSection?.Name
            };
        }
    }
}
