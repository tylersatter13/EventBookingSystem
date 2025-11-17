namespace EventBookingSystem.Application.Models
{
    /// <summary>
    /// Command for creating a new booking.
    /// Represents the input for the CreateBooking use case.
    /// </summary>
    public class CreateBookingCommand
    {
        public required int UserId { get; set; }
        public required int EventId { get; set; }
        public int Quantity { get; set; } = 1;
        public int? SectionId { get; set; }
        public int? SeatId { get; set; }
    }
}