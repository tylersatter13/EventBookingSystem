namespace EventBookingSystem.Infrastructure.Models;

/// <summary>
/// DTO for EventSeat entity.
/// </summary>
public class EventSeatDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int VenueSeatId { get; set; }
    public string Status { get; set; } = "Available";
}
