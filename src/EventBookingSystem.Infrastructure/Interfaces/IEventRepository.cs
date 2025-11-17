using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Infrastructure.Interfaces;

/// <summary>
/// Repository interface for Event entities supporting polymorphic event types.
/// </summary>
public interface IEventRepository : IRespository<EventBase>
{
    /// <summary>
    /// Gets an event by ID with all related data (sections, seats, inventories).
    /// </summary>
    /// <param name="id">The event ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event with all related data, or null if not found.</returns>
    Task<EventBase?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events for a specific venue.
    /// </summary>
    /// <param name="venueId">The venue ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of events for the venue.</returns>
    Task<IEnumerable<EventBase>> GetByVenueIdAsync(int venueId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events within a date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of events within the date range.</returns>
    Task<IEnumerable<EventBase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="entity">The event to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated event.</returns>
    Task<EventBase> UpdateAsync(EventBase entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an event by ID.
    /// </summary>
    /// <param name="id">The event ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates section inventories for a section-based event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="inventories">The section inventories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveSectionInventoriesAsync(int eventId, IEnumerable<EventSectionInventory> inventories, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates event seats for a reserved seating event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="seats">The event seats.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveEventSeatsAsync(int eventId, IEnumerable<EventSeat> seats, CancellationToken cancellationToken = default);
}
