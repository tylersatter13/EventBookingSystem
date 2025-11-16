using System;

namespace EventBookingSystem.Domain.Entities
{
    /// <summary>
    /// Base class for all event types.
    /// Contains common properties and behavior shared across all events.
    /// </summary>
    public abstract class EventBase
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int EstimatedAttendance { get; set; }
        
        // Navigation properties
        public Venue? Venue { get; set; }
        
        /// <summary>
        /// Gets the total capacity for this event.
        /// </summary>
        public abstract int TotalCapacity { get; }
        
        /// <summary>
        /// Gets the total number of reserved/booked seats.
        /// </summary>
        public abstract int TotalReserved { get; }
        
        /// <summary>
        /// Indicates whether the event is sold out.
        /// </summary>
        public abstract bool IsSoldOut { get; }
        
        /// <summary>
        /// Gets the available capacity remaining for this event.
        /// </summary>
        public int AvailableCapacity => TotalCapacity - TotalReserved;
        
        /// <summary>
        /// Validates if the event can accommodate the specified number of reservations.
        /// </summary>
        /// <param name="quantity">The number of seats to validate.</param>
        /// <returns>A ValidationResult indicating success or failure.</returns>
        public virtual ValidationResult ValidateCapacity(int quantity)
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
    }
}
