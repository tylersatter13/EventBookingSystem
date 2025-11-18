namespace EventBookingSystem.Application.DTOs
{
    /// <summary>
    /// Result DTO for booking operations.
    /// Follows immutability pattern with init-only properties.
    /// </summary>
    public class BookingResultDto
    {
        public bool IsSuccessful { get; init; }
        public int? BookingId { get; init; }
        public decimal TotalAmount { get; init; }
        public string Message { get; init; } = string.Empty;
        public List<string> Errors { get; init; } = new();

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