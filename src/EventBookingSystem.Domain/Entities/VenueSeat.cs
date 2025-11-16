using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    public class VenueSeat
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        public required int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the venue section this seat belongs to.
        /// </summary>
        public int VenueSectionId { get; set; }
        
        /// <summary>
        /// Gets or sets the row identifier for this seat.
        /// </summary>
        public required string Row { get; set; }
        
        /// <summary>
        /// Gets or sets the seat number within the row.
        /// </summary>
        public required string SeatNumber { get; set; }
        
        /// <summary>
        /// Gets or sets an optional label for the seat (e.g., "A1", "VIP-12").
        /// </summary>
        public string? SeatLabel { get; set; }

        // Navigation properties
        /// <summary>
        /// Gets or sets the section this seat belongs to.
        /// </summary>
        public VenueSection? Section { get; set; }
        
        /// <summary>
        /// Gets or sets the event seats associated with this venue seat across different events.
        /// </summary>
        public List<EventSeat>? EventSeats { get; set; }
    }
}
