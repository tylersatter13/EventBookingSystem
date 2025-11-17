using EventBookingSystem.Application.DTOs;

namespace EventBookingSystem.Application.Services
{
    /// <summary>
    /// Service for querying event information.
    /// Follows SRP - single responsibility of reading event data.
    /// Follows ISP - focused interface for event queries only.
    /// </summary>
    public interface IEventQueryService
    {
        /// <summary>
        /// Retrieves all future events with available seat/capacity information.
        /// Events are filtered to only include those starting after the current date.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of event DTOs with availability details.</returns>
        Task<IEnumerable<EventAvailabilityDto>> GetFutureEventsWithAvailabilityAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves future events at a specific venue with availability information.
        /// </summary>
        /// <param name="venueId">The ID of the venue.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of event DTOs with availability details.</returns>
        Task<IEnumerable<EventAvailabilityDto>> GetFutureEventsByVenueAsync(int venueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves availability details for a specific event.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Event availability details if found, null otherwise.</returns>
        Task<EventAvailabilityDto?> GetEventAvailabilityAsync(int eventId, CancellationToken cancellationToken = default);
    }
}
