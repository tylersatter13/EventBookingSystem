namespace EventBookingSystem.Infrastructure.Models
{
    /// <summary>
    /// Data Transfer Object for Booking entity.
    /// </summary>
    public class BookingDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the booking.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID associated with the booking.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the event ID associated with the booking.
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// Gets or sets the type of booking (e.g., GeneralAdmission, SectionBased, ReservedSeating).
        /// </summary>
        public string BookingType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the payment status for the booking.
        /// </summary>
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total amount for the booking.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the creation date/time of the booking (ISO 8601 format).
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty; // ISO 8601 format in SQLite
    }
}
