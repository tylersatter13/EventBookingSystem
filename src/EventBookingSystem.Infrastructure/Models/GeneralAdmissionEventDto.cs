namespace EventBookingSystem.Infrastructure.Models;

/// <summary>
/// Data Transfer Object specifically for General Admission events.
/// Contains only the properties relevant to GA events.
/// </summary>
public class GeneralAdmissionEventDto
{
    // Base event properties
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int EstimatedAttendance { get; set; }
    
    // General Admission specific properties
    public int Capacity { get; set; }
    public int Attendees { get; set; }
    public decimal? Price { get; set; }
    
    /// <summary>
    /// Gets the remaining available capacity.
    /// </summary>
    public int AvailableCapacity => Capacity - Attendees;
    
    /// <summary>
    /// Indicates whether the event is sold out.
    /// </summary>
    public bool IsSoldOut => Attendees >= Capacity;
}
