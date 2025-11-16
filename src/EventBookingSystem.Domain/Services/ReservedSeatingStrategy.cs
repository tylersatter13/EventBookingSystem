using EventBookingSystem.Domain.Entities;
using System;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Handles reserved seating where customers select specific seats.
    /// Each seat is individually tracked and can be in Available, Reserved, or Locked status.
    /// </summary>
    public class ReservedSeatingReservationStrategy : IReservationStrategy<ReservedSeatingEvent>
    {
        /// <summary>
        /// Validates if a specific seat can be reserved.
        /// </summary>
        public ValidationResult ValidateReservation(Venue venue, ReservedSeatingEvent evnt, ReservationRequest request)
        {
            if (!request.SeatId.HasValue)
            {
                return ValidationResult.Failure("Seat ID is required for reserved seating.");
            }
            
            return evnt.ValidateSeatReservation(request.SeatId.Value);
        }
        
        /// <summary>
        /// Reserves a specific seat.
        /// </summary>
        public void Reserve(Venue venue, ReservedSeatingEvent evnt, ReservationRequest request)
        {
            var validation = ValidateReservation(venue, evnt, request);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.ErrorMessage);
            }
            
            evnt.ReserveSeat(request.SeatId!.Value);
        }
    }
}
