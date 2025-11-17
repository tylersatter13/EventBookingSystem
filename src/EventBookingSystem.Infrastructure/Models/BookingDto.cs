namespace EventBookingSystem.Infrastructure.Models
{
    /// <summary>
    /// Data Transfer Object for Booking entity.
    /// </summary>
    public class BookingDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int EventId { get; set; }
        public string BookingType { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string CreatedAt { get; set; } = string.Empty; // ISO 8601 format in SQLite
    }
}
