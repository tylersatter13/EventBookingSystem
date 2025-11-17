namespace EventBookingSystem.Infrastructure.Models;

/// <summary>
/// Data transfer object for VenueSection table.
/// </summary>
public class VenueSectionDto
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public required string Name { get; set; }
    
}