using AwesomeAssertions;
using EventBookingSystem.Application.Services;

namespace EventBookingSystem.Application.Tests.Services
{
    [TestClass]
    public class DeterministicPaymentServiceTests
    {
        private DeterministicPaymentService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _service = new DeterministicPaymentService();
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new PaymentRequest
            {
                UserId = 1,
                Amount = 100m,
                Description = "Test booking payment",
                PaymentMethod = "CreditCard"
            };

            // Act
            var result = await _service.ProcessPaymentAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.TransactionId.Should().NotBeNullOrEmpty();
            result.TransactionId.Should().StartWith("TXN-");
            result.ErrorMessage.Should().BeEmpty();
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_ZeroAmount_ReturnsFailure()
        {
            // Arrange
            var request = new PaymentRequest
            {
                UserId = 1,
                Amount = 0m,
                Description = "Test booking payment"
            };

            // Act
            var result = await _service.ProcessPaymentAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("must be greater than zero");
            result.TransactionId.Should().BeEmpty();
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_NegativeAmount_ReturnsFailure()
        {
            // Arrange
            var request = new PaymentRequest
            {
                UserId = 1,
                Amount = -50m,
                Description = "Test booking payment"
            };

            // Act
            var result = await _service.ProcessPaymentAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("must be greater than zero");
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_AmountExceedsMaximum_ReturnsFailure()
        {
            // Arrange
            var request = new PaymentRequest
            {
                UserId = 1,
                Amount = 15000m,
                Description = "Test booking payment"
            };

            // Act
            var result = await _service.ProcessPaymentAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("exceeds maximum limit");
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_InvalidUserId_ReturnsFailure()
        {
            // Arrange
            var request = new PaymentRequest
            {
                UserId = 0,
                Amount = 100m,
                Description = "Test booking payment"
            };

            // Act
            var result = await _service.ProcessPaymentAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Invalid user ID");
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_EmptyDescription_ReturnsFailure()
        {
            // Arrange
            var request = new PaymentRequest
            {
                UserId = 1,
                Amount = 100m,
                Description = ""
            };

            // Act
            var result = await _service.ProcessPaymentAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("description is required");
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_ValidRequestAtMaximum_ReturnsSuccess()
        {
            // Arrange
            var request = new PaymentRequest
            {
                UserId = 1,
                Amount = 10000m, // Exactly at maximum
                Description = "Test booking payment"
            };

            // Act
            var result = await _service.ProcessPaymentAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.TransactionId.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_MultipleCalls_GenerateUniqueTransactionIds()
        {
            // Arrange
            var request1 = new PaymentRequest
            {
                UserId = 1,
                Amount = 100m,
                Description = "First payment"
            };

            var request2 = new PaymentRequest
            {
                UserId = 2,
                Amount = 200m,
                Description = "Second payment"
            };

            // Act
            var result1 = await _service.ProcessPaymentAsync(request1);
            var result2 = await _service.ProcessPaymentAsync(request2);

            // Assert
            result1.IsSuccessful.Should().BeTrue();
            result2.IsSuccessful.Should().BeTrue();
            result1.TransactionId.Should().NotBe(result2.TransactionId, 
                because: "each transaction should have a unique ID");
        }
    }

    [TestClass]
    public class SimulatedPaymentServiceTests
    {
        private SimulatedPaymentService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _service = new SimulatedPaymentService();
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_ValidRequest_EventuallySucceeds()
        {
            // Arrange
            var request = new PaymentRequest
            {
                UserId = 1,
                Amount = 100m,
                Description = "Test booking payment"
            };

            // Act - Try multiple times due to randomization (90% success rate)
            PaymentResult? successResult = null;
            for (int i = 0; i < 20; i++)
            {
                var result = await _service.ProcessPaymentAsync(request);
                if (result.IsSuccessful)
                {
                    successResult = result;
                    break;
                }
            }

            // Assert
            successResult.Should().NotBeNull(because: "with 90% success rate, should succeed within 20 attempts");
            successResult!.TransactionId.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_ValidRequest_HasDelay()
        {
            // Arrange
            var request = new PaymentRequest
            {
                UserId = 1,
                Amount = 100m,
                Description = "Test booking payment"
            };

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _service.ProcessPaymentAsync(request);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(100, 
                because: "simulated payment should have a delay");
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_InvalidAmount_ReturnsImmediateFailure()
        {
            // Arrange
            var request = new PaymentRequest
            {
                UserId = 1,
                Amount = -50m,
                Description = "Test booking payment"
            };

            // Act
            var result = await _service.ProcessPaymentAsync(request);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Contain("must be greater than zero");
        }
    }
}
