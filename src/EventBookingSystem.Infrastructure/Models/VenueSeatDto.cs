namespace EventBookingSystem.Infrastructure.Models;

/// <summary>
/// Data transfer object for VenueSeat table.
/// </summary>
public class VenueSeatDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the venue seat.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the venue section ID to which this seat belongs.
    /// </summary>
    public int VenueSectionId { get; set; }

    /// <summary>
    /// Gets or sets the row identifier for this seat.
    /// </summary>
    public required string Row { get; set; }

    /// <summary>
    /// Gets or sets the seat number within the row.
    /// </summary>
    public required string SeatNumber { get; set; }

    /// <summary>
    /// Gets or sets an optional label for the seat (e.g., "A1", "VIP-12").
    /// </summary>
    public string? SeatLabel { get; set; }
}
