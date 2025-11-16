using System;
using System.Collections.Generic;
using System.Linq;

namespace EventBookingSystem.Domain.Entities
{
    /// <summary>
    /// Represents an event with multiple sections offering different pricing tiers.
    /// Customers select a section but not specific seats within that section.
    /// Examples: Concerts with Floor/Balcony tiers, festivals with VIP/GA areas.
    /// </summary>
    public class SectionBasedEvent : EventBase
    {
        public List<EventSectionInventory> SectionInventories { get; set; } = new();
        
        /// <summary>
        /// Optional capacity override for the entire event.
        /// </summary>
        public int? CapacityOverride { get; set; }
        
        public override int TotalCapacity => 
            CapacityOverride ?? (SectionInventories?.Sum(si => si.Capacity) ?? 0);
        
        public override int TotalReserved => 
            SectionInventories?.Sum(si => si.Booked) ?? 0;
        
        public override bool IsSoldOut => AvailableCapacity <= 0;
        
        /// <summary>
        /// Gets a specific section inventory by section ID.
        /// </summary>
        /// <param name="sectionId">The venue section ID.</param>
        /// <returns>The EventSectionInventory if found, null otherwise.</returns>
        public EventSectionInventory? GetSection(int sectionId)
        {
            return SectionInventories?.FirstOrDefault(si => si.VenueSectionId == sectionId);
        }
        
        /// <summary>
        /// Validates if reservation can be made in specific section.
        /// </summary>
        /// <param name="sectionId">The section to validate.</param>
        /// <param name="quantity">The number of tickets to reserve.</param>
        /// <returns>A ValidationResult indicating success or failure.</returns>
        public ValidationResult ValidateSectionReservation(int sectionId, int quantity)
        {
            var section = GetSection(sectionId);
            if (section == null)
                return ValidationResult.Failure($"Section with ID {sectionId} not found.");
            
            return section.ValidateReservation(quantity);
        }
        
        /// <summary>
        /// Reserves tickets in a specific section.
        /// </summary>
        /// <param name="sectionId">The section to reserve in.</param>
        /// <param name="quantity">The number of tickets to reserve.</param>
        /// <exception cref="InvalidOperationException">Thrown when section not found or insufficient capacity.</exception>
        public void ReserveInSection(int sectionId, int quantity)
        {
            var validation = ValidateSectionReservation(sectionId, quantity);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.ErrorMessage);
            
            var section = GetSection(sectionId)!;
            section.ReserveSeats(quantity);
        }
        
        /// <summary>
        /// Releases tickets from a specific section.
        /// </summary>
        /// <param name="sectionId">The section to release tickets from.</param>
        /// <param name="quantity">The number of tickets to release.</param>
        /// <exception cref="InvalidOperationException">Thrown when section not found or invalid release attempt.</exception>
        public void ReleaseFromSection(int sectionId, int quantity)
        {
            var section = GetSection(sectionId);
            if (section == null)
                throw new InvalidOperationException($"Section with ID {sectionId} not found.");
            
            section.ReleaseSeats(quantity);
        }
        
        /// <summary>
        /// Gets all sections with available capacity.
        /// </summary>
        /// <returns>A collection of sections that are not sold out.</returns>
        public IEnumerable<EventSectionInventory> GetAvailableSections()
        {
            return SectionInventories.Where(s => !s.IsSoldOut);
        }
        
        /// <summary>
        /// Gets all sections that are sold out.
        /// </summary>
        /// <returns>A collection of sold out sections.</returns>
        public IEnumerable<EventSectionInventory> GetSoldOutSections()
        {
            return SectionInventories.Where(s => s.IsSoldOut);
        }
    }
}
