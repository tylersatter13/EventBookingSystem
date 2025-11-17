namespace EventBookingSystem.Application.DTOs
{
    /// <summary>
    /// Result DTO for booking operations.
    /// </summary>
    public class BookingResultDto
    {
        public bool IsSuccessful { get; set; }
        public int? BookingId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();

        public static BookingResultDto Success(int bookingId, decimal totalAmount, string message)
        {
            return new BookingResultDto
            {
                IsSuccessful = true,
                BookingId = bookingId,
                TotalAmount = totalAmount,
                Message = message
            };
        }

        public static BookingResultDto Failure(string error)
        {
            return new BookingResultDto
            {
                IsSuccessful = false,
                Message = error,
                Errors = new List<string> { error }
            };
        }
    }
}