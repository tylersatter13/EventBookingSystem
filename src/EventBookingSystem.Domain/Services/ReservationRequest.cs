namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Encapsulates reservation request details.
    /// </summary>
    public class ReservationRequest
    {
        /// <summary>
        /// Gets or sets the number of tickets/seats to reserve.
        /// </summary>
        public int Quantity { get; set; } = 1;
        
        /// <summary>
        /// Gets or sets the section ID for section-based reservations.
        /// Required for SectionBasedEvent.
        /// </summary>
        public int? SectionId { get; set; }
        
        /// <summary>
        /// Gets or sets the specific seat ID for reserved seating.
        /// Required for ReservedSeatingEvent.
        /// </summary>
        public int? SeatId { get; set; }
        
        /// <summary>
        /// Gets or sets optional customer/booking information.
        /// </summary>
        public int? CustomerId { get; set; }
    }
}
