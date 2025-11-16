using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    /// <summary>
    /// Represents the available inventory for a specific section of an event.
    /// This allows event-specific capacity overrides and section-level management.
    /// </summary>
    public class EventSectionInventory
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int VenueSectionId { get; set; }
        
        /// <summary>
        /// The total capacity available for this section for this specific event.
        /// May differ from the physical section capacity due to staging, setup, etc.
        /// </summary>
        public int Capacity { get; set; }

        private int _booked = 0;
        
        /// <summary>
        /// Gets the number of seats currently booked in this section.
        /// </summary>
        public int Booked => _booked;
        
        /// <summary>
        /// Gets the remaining available capacity.
        /// </summary>
        public int Remaining => Capacity - _booked;
        
        /// <summary>
        /// Indicates if this section is sold out.
        /// </summary>
        public bool IsSoldOut => Remaining <= 0;
        
        /// <summary>
        /// Optional price for this section for this specific event.
        /// </summary>
        public decimal? Price { get; set; }
        
        /// <summary>
        /// Seating allocation mode for this section (can vary per event).
        /// </summary>
        public SeatAllocationMode AllocationMode { get; set; } = SeatAllocationMode.GeneralAdmission;

        // Navigation properties
        public EventBase? Event { get; set; }
        public VenueSection? VenueSection { get; set; }
        public ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();
        
        /// <summary>
        /// Attempts to reserve seats in this section inventory.
        /// </summary>
        /// <param name="quantity">The number of seats to reserve.</param>
        /// <exception cref="ArgumentException">Thrown when quantity is not positive.</exception>
        /// <exception cref="InvalidOperationException">Thrown when insufficient capacity is available.</exception>
        public void ReserveSeats(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));
                
            if (Remaining < quantity)
                throw new InvalidOperationException(
                    $"Insufficient capacity in section. Requested: {quantity}, Available: {Remaining}");
            
            _booked += quantity;
        }
        
        /// <summary>
        /// Attempts to release previously reserved seats.
        /// </summary>
        /// <param name="quantity">The number of seats to release.</param>
        /// <exception cref="ArgumentException">Thrown when quantity is not positive.</exception>
        /// <exception cref="InvalidOperationException">Thrown when trying to release more seats than are booked.</exception>
        public void ReleaseSeats(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));
                
            if (_booked < quantity)
                throw new InvalidOperationException(
                    $"Cannot release more seats than are booked. Requested: {quantity}, Booked: {_booked}");
            
            _booked -= quantity;
        }
        
        /// <summary>
        /// Validates if a reservation can be made for the specified quantity.
        /// </summary>
        /// <param name="quantity">The number of seats to validate.</param>
        /// <returns>A ValidationResult indicating success or failure with an error message.</returns>
        public ValidationResult ValidateReservation(int quantity)
        {
            if (quantity <= 0)
                return ValidationResult.Failure("Quantity must be positive.");
                
            if (IsSoldOut)
                return ValidationResult.Failure($"Section '{VenueSection?.Name ?? "Unknown"}' is sold out.");
                
            if (Remaining < quantity)
                return ValidationResult.Failure(
                    $"Insufficient capacity. Requested: {quantity}, Available: {Remaining}");
            
            return ValidationResult.Success();
        }
    }
}
