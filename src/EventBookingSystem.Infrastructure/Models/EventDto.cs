namespace EventBookingSystem.Infrastructure.Models;

/// <summary>
/// DTO for Event entity using Table Per Hierarchy pattern.
/// Maps to the Events table with discriminator column.
/// </summary>
public class EventDto
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int EstimatedAttendance { get; set; }
    
    // Discriminator column
    public string EventType { get; set; } = string.Empty;
    
    // GeneralAdmissionEvent specific fields
    public int? GA_Capacity { get; set; }
    public int? GA_Attendees { get; set; }
    public decimal? GA_Price { get; set; }
    public int? GA_CapacityOverride { get; set; }
    
    // SectionBasedEvent specific fields
    public int? SB_CapacityOverride { get; set; }
}
