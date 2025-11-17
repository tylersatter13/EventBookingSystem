namespace EventBookingSystem.Infrastructure.Models;

/// <summary>
/// DTO for EventSeat entity.
/// </summary>
public class EventSeatDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the event seat.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the event ID associated with the seat.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Gets or sets the venue seat ID associated with the seat.
    /// </summary>
    public int VenueSeatId { get; set; }

    /// <summary>
    /// Gets or sets the status of the seat (e.g., Available, Reserved).
    /// </summary>
    public string Status { get; set; } = "Available";
}
