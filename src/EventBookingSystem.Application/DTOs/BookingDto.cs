namespace EventBookingSystem.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for booking information.
    /// Used for read operations in the application layer.
    /// </summary>
    public class BookingDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventStartsAt { get; set; }
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string BookingType { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<BookingItemDto> BookingItems { get; set; } = new();
    }

    /// <summary>
    /// DTO for individual booking items.
    /// </summary>
    public class BookingItemDto
    {
        public int Id { get; set; }
        public int? EventSeatId { get; set; }
        public int? EventSectionInventoryId { get; set; }
        public int Quantity { get; set; }
        public string? SeatLabel { get; set; }
        public string? SectionName { get; set; }
    }
}
