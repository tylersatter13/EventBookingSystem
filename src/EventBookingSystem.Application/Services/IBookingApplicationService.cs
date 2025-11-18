using EventBookingSystem.Application.DTOs;
using EventBookingSystem.Application.Models;

namespace EventBookingSystem.Application.Services
{
    /// <summary>
    /// Interface for booking application service.
    /// Orchestrates booking creation operations.
    /// Follows ISP - focused interface for command operations.
    /// </summary>
    public interface IBookingApplicationService
    {
        /// <summary>
        /// Creates a booking from a command.
        /// Orchestrates the workflow: validate ? load ? create ? process payment ? persist ? map.
        /// </summary>
        /// <param name="command">The booking creation command.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result DTO containing booking details or error information.</returns>
        Task<BookingResultDto> CreateBookingAsync(CreateBookingCommand command, CancellationToken cancellationToken = default);
    }
}
