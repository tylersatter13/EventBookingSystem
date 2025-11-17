namespace EventBookingSystem.Infrastructure.Models
{
    /// <summary>
    /// Data Transfer Object for BookingItem entity.
    /// </summary>
    public class BookingItemDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the booking item.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the booking ID to which this item belongs.
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// Gets or sets the event seat ID if this item is for a reserved seat (optional).
        /// </summary>
        public int? EventSeatId { get; set; }

        /// <summary>
        /// Gets or sets the event section inventory ID if this item is for a section (optional).
        /// </summary>
        public int? EventSectionInventoryId { get; set; }

        /// <summary>
        /// Gets or sets the quantity for this booking item.
        /// </summary>
        public int Quantity { get; set; }
    }
}
