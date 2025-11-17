namespace EventBookingSystem.Infrastructure.Models;

/// <summary>
/// Data transfer object for VenueSection table.
/// </summary>
public class VenueSectionDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the venue section.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the venue ID to which this section belongs.
    /// </summary>
    public int VenueId { get; set; }

    /// <summary>
    /// Gets or sets the name of the venue section.
    /// </summary>
    public required string Name { get; set; }
}