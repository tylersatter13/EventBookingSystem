namespace EventBookingSystem.Application.Services
{
    /// <summary>
    /// Simulated payment service for testing and development.
    /// Implements payment validation rules without actual payment processing.
    /// </summary>
    public class SimulatedPaymentService : IPaymentService
    {
        private readonly Random _random = new();

        /// <summary>
        /// Processes a payment with simulated validation rules.
        /// Payment is approved if:
        /// - Amount is positive and less than $10,000
        /// - UserId is positive
        /// - 90% success rate (simulates occasional failures)
        /// </summary>
        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
        {
            // Simulate API call delay
            await Task.Delay(100, cancellationToken);

            // Validation rules
            if (request.Amount <= 0)
            {
                return PaymentResult.Failure("Payment amount must be greater than zero");
            }

            if (request.Amount > 10000)
            {
                return PaymentResult.Failure("Payment amount exceeds maximum limit of $10,000");
            }

            if (request.UserId <= 0)
            {
                return PaymentResult.Failure("Invalid user ID");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return PaymentResult.Failure("Payment description is required");
            }

            // Simulate 90% success rate
            var isApproved = _random.Next(100) < 90;
            
            if (!isApproved)
            {
                return PaymentResult.Failure("Payment was declined by the payment processor");
            }

            // Generate simulated transaction ID
            var transactionId = $"TXN-{Guid.NewGuid():N}";
            
            return PaymentResult.Success(transactionId);
        }
    }

    /// <summary>
    /// Deterministic payment service for testing.
    /// Always approves valid payments without randomization.
    /// </summary>
    public class DeterministicPaymentService : IPaymentService
    {
        /// <summary>
        /// Processes a payment with deterministic validation rules.
        /// Payment is approved if:
        /// - Amount is positive and less than $10,000
        /// - UserId is positive
        /// </summary>
        public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
        {
            // Validation rules (same as simulated, but deterministic)
            if (request.Amount <= 0)
            {
                return Task.FromResult(PaymentResult.Failure("Payment amount must be greater than zero"));
            }

            if (request.Amount > 10000)
            {
                return Task.FromResult(PaymentResult.Failure("Payment amount exceeds maximum limit of $10,000"));
            }

            if (request.UserId <= 0)
            {
                return Task.FromResult(PaymentResult.Failure("Invalid user ID"));
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return Task.FromResult(PaymentResult.Failure("Payment description is required"));
            }

            // Always approve valid payments (deterministic for testing)
            var transactionId = $"TXN-{Guid.NewGuid():N}";
            
            return Task.FromResult(PaymentResult.Success(transactionId));
        }
    }
}
