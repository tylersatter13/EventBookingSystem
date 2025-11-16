using EventBookingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Domain.Services
{
    public class GeneralAdmissionStrategy : ISeatingStrategy
    {
        public bool CanHandle(EventType eventType)
        {
            return eventType == EventType.GeneralAdmission;
        }
        public ValidationResult ValidateReservation(Venue venue, Event evnt, int? sectionId = null)
        {
           return evnt.SeatsReservered < venue.MaxCapacity
                ? ValidationResult.Success()
                : ValidationResult.Failure("No available seats for this event.");
        }
        public void Reserve(Venue venue, Event evnt, int? sectionId = null)
        {
            var validationResult = ValidateReservation(venue, evnt, sectionId);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }
            evnt.BookSeat();
        }


    }
}
