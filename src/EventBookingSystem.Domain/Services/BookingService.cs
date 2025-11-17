using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Domain service responsible for creating and managing bookings.
    /// Orchestrates validation and booking creation following SOLID principles.
    /// </summary>
    public class BookingService : IBookingService
    {
        private readonly EventReservationService _reservationService;
        private readonly IEnumerable<IBookingValidator> _bookingValidators;

        public BookingService(
            EventReservationService reservationService,
            IEnumerable<IBookingValidator> bookingValidators = null)
        {
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _bookingValidators = bookingValidators ?? Enumerable.Empty<IBookingValidator>();
        }

        /// <summary>
        /// Creates a new booking after validating all business rules.
        /// </summary>
        /// <param name="user">The user making the booking.</param>
        /// <param name="evnt">The event being booked.</param>
        /// <param name="reservationRequest">The reservation details.</param>
        /// <returns>The created booking.</returns>
        /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
        public Booking CreateBooking(User user, EventBase evnt, ReservationRequest reservationRequest)
        {
            // Validate the booking
            var validationResult = ValidateBooking(user, evnt, reservationRequest);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }

            // Determine booking type based on event type
            var bookingType = DetermineBookingType(evnt, reservationRequest);

            // Calculate total amount (would normally come from pricing service)
            var totalAmount = CalculateTotalAmount(evnt, reservationRequest);

            // Create the booking entity
            var booking = new Booking
            {
                User = user,
                Event = evnt,
                BookingType = bookingType,
                TotalAmount = totalAmount,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            // Create booking items based on reservation type
            CreateBookingItems(booking, evnt, reservationRequest);

            return booking;
        }

        /// <summary>
        /// Validates if a booking can be created.
        /// Orchestrates multiple validators following the Composite pattern.
        /// </summary>
        /// <param name="user">The user making the booking.</param>
        /// <param name="evnt">The event being booked.</param>
        /// <param name="reservationRequest">The reservation details.</param>
        /// <returns>A ValidationResult indicating success or failure.</returns>
        public ValidationResult ValidateBooking(User user, EventBase evnt, ReservationRequest reservationRequest)
        {
            // 1. Validate event capacity (delegate to event)
            var capacityValidation = ValidateEventCapacity(evnt, reservationRequest);
            if (!capacityValidation.IsValid)
            {
                return capacityValidation;
            }

            // 2. Validate reservation-specific rules (delegate to reservation service)
            var reservationValidation = ValidateReservationRules(evnt, reservationRequest);
            if (!reservationValidation.IsValid)
            {
                return reservationValidation;
            }

            // 3. Run custom booking validators
            foreach (var validator in _bookingValidators)
            {
                var result = validator.Validate(user, evnt, reservationRequest);
                if (!result.IsValid)
                {
                    return result;
                }
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates event capacity by delegating to the event entity.
        /// </summary>
        private ValidationResult ValidateEventCapacity(EventBase evnt, ReservationRequest request)
        {
            // Event knows its own capacity rules
            return evnt.ValidateCapacity(request.Quantity);
        }

        /// <summary>
        /// Validates reservation-specific rules by delegating to reservation service.
        /// </summary>
        private ValidationResult ValidateReservationRules(EventBase evnt, ReservationRequest request)
        {
            return evnt switch
            {
                GeneralAdmissionEvent ga => 
                    _reservationService.ValidateGeneralAdmission(ga.Venue, ga, request.Quantity),
                
                SectionBasedEvent sb when request.SectionId.HasValue => 
                    _reservationService.ValidateSectionBased(sb.Venue, sb, request.SectionId.Value, request.Quantity),
                
                ReservedSeatingEvent rs when request.SeatId.HasValue => 
                    _reservationService.ValidateReservedSeating(rs.Venue, rs, request.SeatId.Value),
                
                SectionBasedEvent _ => 
                    ValidationResult.Failure("Section ID is required for section-based events."),
                
                ReservedSeatingEvent _ => 
                    ValidationResult.Failure("Seat ID is required for reserved seating events."),
                
                _ => ValidationResult.Failure($"Unsupported event type: {evnt.GetType().Name}")
            };
        }

        /// <summary>
        /// Determines the booking type based on the event type and reservation request.
        /// </summary>
        private BookingType DetermineBookingType(EventBase evnt, ReservationRequest request)
        {
            return evnt switch
            {
                GeneralAdmissionEvent => BookingType.GA,
                SectionBasedEvent => BookingType.Section,
                ReservedSeatingEvent => BookingType.Seat,
                _ => throw new InvalidOperationException($"Unknown event type: {evnt.GetType().Name}")
            };
        }

        /// <summary>
        /// Calculates the total amount for the booking.
        /// This is a simplified implementation - in reality, would delegate to a PricingService.
        /// </summary>
        private decimal CalculateTotalAmount(EventBase evnt, ReservationRequest request)
        {
            // Simplified - in reality, this would be more complex
            return evnt switch
            {
                GeneralAdmissionEvent ga => (ga.Price ?? 0m) * request.Quantity,
                SectionBasedEvent sb when request.SectionId.HasValue => 
                    (sb.GetSection(request.SectionId.Value)?.Price ?? 0m) * request.Quantity,
                ReservedSeatingEvent rs => 
                    // For reserved seating, pricing would come from a pricing service
                    0m, // TODO: Implement pricing service
                _ => 0m
            };
        }

        /// <summary>
        /// Creates booking items based on the event type and reservation request.
        /// GA bookings don't need BookingItems since capacity is tracked on the event itself.
        /// </summary>
        private void CreateBookingItems(Booking booking, EventBase evnt, ReservationRequest request)
        {
            switch (evnt)
            {
                case GeneralAdmissionEvent ga:
                    // GA bookings don't need BookingItems
                    // Capacity is tracked on GeneralAdmissionEvent.Attendees
                    // The booking itself records the transaction
                    break;

                case SectionBasedEvent sb when request.SectionId.HasValue:
                    // For section-based, create item for the section
                    var section = sb.GetSection(request.SectionId.Value);
                    booking.BookingItems.Add(new BookingItem
                    {
                        Booking = booking,
                        EventSection = section,
                        Quantity = request.Quantity
                    });
                    break;

                case ReservedSeatingEvent rs when request.SeatId.HasValue:
                    // For reserved seating, create item for specific seat
                    var seat = rs.GetSeat(request.SeatId.Value);
                    var eventSeat = rs.Seats.First(s => s.VenueSeatId == request.SeatId.Value);
                    booking.BookingItems.Add(new BookingItem
                    {
                        Booking = booking,
                        EventSeat = eventSeat,
                        Quantity = 1
                    });
                    break;
            }
        }
    }
}
