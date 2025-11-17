namespace EventBookingSystem.Application.Services
{
    /// <summary>
    /// Interface for payment processing service.
    /// Follows ISP - focused interface for payment operations.
    /// Follows DIP - depends on abstraction, not concrete implementation.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Processes a payment for a booking.
        /// </summary>
        /// <param name="request">The payment request containing booking details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A PaymentResult indicating success or failure.</returns>
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Request for processing a payment.
    /// </summary>
    public class PaymentRequest
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "CreditCard"; // Default payment method
    }

    /// <summary>
    /// Result of a payment processing operation.
    /// </summary>
    public class PaymentResult
    {
        public bool IsSuccessful { get; init; }
        public string TransactionId { get; init; } = string.Empty;
        public string ErrorMessage { get; init; } = string.Empty;
        public DateTime ProcessedAt { get; init; }

        public static PaymentResult Success(string transactionId)
        {
            return new PaymentResult
            {
                IsSuccessful = true,
                TransactionId = transactionId,
                ProcessedAt = DateTime.UtcNow
            };
        }

        public static PaymentResult Failure(string errorMessage)
        {
            return new PaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = errorMessage,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }
}
