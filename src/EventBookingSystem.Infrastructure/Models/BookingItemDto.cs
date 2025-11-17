namespace EventBookingSystem.Infrastructure.Models
{
    /// <summary>
    /// Data Transfer Object for BookingItem entity.
    /// </summary>
    public class BookingItemDto
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int? EventSeatId { get; set; }
        public int? EventSectionInventoryId { get; set; }
        public int Quantity { get; set; }
    }
}
