namespace EventBookingSystem.Domain.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public BookingType BookingType { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public User User { get; set; }
        public EventBase Event { get; set; }
        public ICollection<BookingItem>? BookingItems { get; set; }
    }
}
