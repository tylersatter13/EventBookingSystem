using System;

namespace EventBookingSystem.Domain.Entities
{
    /// <summary>
    /// Represents a simple general admission event with no sections or assigned seats.
    /// Examples: Standing-room concerts, festival lawn seating, club events.
    /// </summary>
    public class GeneralAdmissionEvent : EventBase
    {
        /// <summary>
        /// The total capacity for this general admission event.
        /// </summary>
        public int Capacity { get; set; }
        
        private int _attendees = 0;
        
        /// <summary>
        /// Gets the number of attendees currently reserved.
        /// </summary>
        public int Attendees => _attendees;
        
        /// <summary>
        /// Optional price for admission.
        /// </summary>
        public decimal? Price { get; set; }
        
        /// <summary>
        /// Optional capacity override (e.g., stage setup reducing capacity).
        /// </summary>
        public int? CapacityOverride { get; set; }
        
        public override int TotalCapacity => CapacityOverride ?? Capacity;
        public override int TotalReserved => _attendees;
        public override bool IsSoldOut => _attendees >= TotalCapacity;
        
        /// <summary>
        /// Reserves general admission tickets.
        /// </summary>
        /// <param name="quantity">The number of tickets to reserve.</param>
        /// <exception cref="InvalidOperationException">Thrown when insufficient capacity is available.</exception>
        public void ReserveTickets(int quantity)
        {
            var validation = ValidateCapacity(quantity);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.ErrorMessage);
            
            _attendees += quantity;
        }
        
        /// <summary>
        /// Releases previously reserved tickets.
        /// </summary>
        /// <param name="quantity">The number of tickets to release.</param>
        /// <exception cref="ArgumentException">Thrown when quantity is not positive.</exception>
        /// <exception cref="InvalidOperationException">Thrown when trying to release more tickets than are reserved.</exception>
        public void ReleaseTickets(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));
            
            if (_attendees < quantity)
                throw new InvalidOperationException(
                    $"Cannot release {quantity} tickets. Only {_attendees} are reserved.");
            
            _attendees -= quantity;
        }
    }
}
