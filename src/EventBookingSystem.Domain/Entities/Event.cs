using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    public class Event
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public string Name { get; set; }
        public EventType EventType { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }

        public int EstimatedAttendance { get; set; }

        /// <summary>
        /// Optional capacity override for this event (e.g., stage setup reducing capacity).
        /// When set, this overrides the venue's default capacity calculation.
        /// </summary>
        public int? CapacityOverride { get; set; }

        // Navigation properties
        public Venue Venue { get; set; }
        public List<EventSeat> EventSeats { get; set; } = new();
        public List<EventSectionInventory> SectionInventories { get; set; } = new();

        /// <summary>
        /// Gets the total capacity for this event based on event type and configuration.
        /// </summary>
        public int TotalCapacity
        {
            get
            {
                // For reserved seating, capacity is based on EventSeats
                if (EventType == EventType.Reserved && EventSeats?.Any() == true)
                    return EventSeats.Count;
                
                // For events using section inventories
                if (SectionInventories?.Any() == true)
                    return SectionInventories.Sum(si => si.Capacity);
                
                // For simple general admission or when no inventories exist
                return CapacityOverride ?? Venue?.MaxCapacity ?? 0;
            }
        }

        /// <summary>
        /// Gets the total number of reserved/booked seats across all allocation methods.
        /// </summary>
        public int TotalReserved
        {
            get
            {
                // For reserved seating, count EventSeats with Reserved status
                if (EventType == EventType.Reserved && EventSeats?.Any() == true)
                    return EventSeats.Count(s => s.Status == SeatStatus.Reserved);
                
                // For events using section inventories, sum booked seats
                if (SectionInventories?.Any() == true)
                    return SectionInventories.Sum(si => si.Booked);
                
                // Fallback: count reserved EventSeats if they exist
                return EventSeats?.Count(s => s.Status == SeatStatus.Reserved) ?? 0;
            }
        }

        /// <summary>
        /// Gets the available capacity remaining for this event.
        /// </summary>
        public int AvailableCapacity => TotalCapacity - TotalReserved;

        /// <summary>
        /// Indicates whether the event is sold out.
        /// </summary>
        public bool IsSoldOut => AvailableCapacity <= 0;

        /// <summary>
        /// Validates if the event can accommodate the specified number of reservations.
        /// </summary>
        /// <param name="quantity">The number of seats to validate.</param>
        /// <returns>A ValidationResult indicating success or failure.</returns>
        public ValidationResult ValidateCapacity(int quantity)
        {
            if (quantity <= 0)
                return ValidationResult.Failure("Quantity must be positive.");
            
            if (IsSoldOut)
                return ValidationResult.Failure($"Event '{Name}' is sold out.");
            
            if (AvailableCapacity < quantity)
                return ValidationResult.Failure(
                    $"Insufficient capacity. Requested: {quantity}, Available: {AvailableCapacity}");
            
            return ValidationResult.Success();
        }

        /// <summary>
        /// Gets a specific section inventory by section ID.
        /// </summary>
        /// <param name="sectionId">The venue section ID.</param>
        /// <returns>The EventSectionInventory if found, null otherwise.</returns>
        public EventSectionInventory? GetSectionInventory(int sectionId)
        {
            return SectionInventories?.FirstOrDefault(si => si.VenueSectionId == sectionId);
        }

        /// <summary>
        /// Reserves seats in a specific section (for general admission with section inventory).
        /// </summary>
        /// <param name="sectionId">The section to reserve seats in.</param>
        /// <param name="quantity">The number of seats to reserve.</param>
        /// <exception cref="InvalidOperationException">Thrown when section not found or insufficient capacity.</exception>
        public void ReserveSectionSeats(int sectionId, int quantity)
        {
            var sectionInventory = GetSectionInventory(sectionId);
            
            if (sectionInventory == null)
                throw new InvalidOperationException($"Section inventory not found for section ID: {sectionId}");
            
            sectionInventory.ReserveSeats(quantity);
        }
    }
}
