using EventBookingSystem.Domain.Entities;
using System;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Handles section-based general admission where customers choose a section 
    /// (e.g., Floor vs. Balcony) but not specific seats within that section.
    /// This allows for different pricing tiers while maintaining first-come, first-served seating.
    /// </summary>
    public class SectionBasedReservationStrategy : IReservationStrategy<SectionBasedEvent>
    {
        /// <summary>
        /// Validates if tickets can be reserved in the specified section.
        /// </summary>
        public ValidationResult ValidateReservation(Venue venue, SectionBasedEvent evnt, ReservationRequest request)
        {
            // Section ID is required for section-based admission
            if (!request.SectionId.HasValue)
            {
                return ValidationResult.Failure("Section ID is required for section-based admission.");
            }
            
            return evnt.ValidateSectionReservation(request.SectionId.Value, request.Quantity);
        }
        
        /// <summary>
        /// Reserves tickets in a specific section.
        /// </summary>
        public void Reserve(Venue venue, SectionBasedEvent evnt, ReservationRequest request)
        {
            var validation = ValidateReservation(venue, evnt, request);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.ErrorMessage);
            }
            
            evnt.ReserveInSection(request.SectionId!.Value, request.Quantity);
        }
    }
}
