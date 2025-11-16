using EventBookingSystem.Domain.Entities;
using System;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Handles simple general admission reservations where no specific seat or section assignments are made.
    /// This is for events like standing-room concerts, festival lawn seating, or any event with open admission.
    /// </summary>
    public class GeneralAdmissionReservationStrategy : IReservationStrategy<GeneralAdmissionEvent>
    {
        /// <summary>
        /// Validates if tickets can be reserved for the general admission event.
        /// </summary>
        public ValidationResult ValidateReservation(Venue venue, GeneralAdmissionEvent evnt, ReservationRequest request)
        {
            return evnt.ValidateCapacity(request.Quantity);
        }
        
        /// <summary>
        /// Reserves general admission tickets.
        /// </summary>
        public void Reserve(Venue venue, GeneralAdmissionEvent evnt, ReservationRequest request)
        {
            var validation = ValidateReservation(venue, evnt, request);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.ErrorMessage);
            }
            
            evnt.ReserveTickets(request.Quantity);
        }
    }
}
