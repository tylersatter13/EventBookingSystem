namespace EventBookingSystem.Infrastructure.Models;

/// <summary>
/// DTO for Event entity using Table Per Hierarchy pattern.
/// Maps to the Events table with discriminator column.
/// </summary>
public class EventDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the event.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the venue ID associated with the event.
    /// </summary>
    public int VenueId { get; set; }

    /// <summary>
    /// Gets or sets the name of the event.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start date/time of the event.
    /// </summary>
    public DateTime StartsAt { get; set; }

    /// <summary>
    /// Gets or sets the end date/time of the event (optional).
    /// </summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>
    /// Gets or sets the estimated attendance for the event.
    /// </summary>
    public int EstimatedAttendance { get; set; }

    /// <summary>
    /// Gets or sets the event type discriminator (e.g., GeneralAdmission, SectionBased, ReservedSeating).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the capacity for general admission events (optional).
    /// </summary>
    public int? GA_Capacity { get; set; }

    /// <summary>
    /// Gets or sets the number of attendees for general admission events (optional).
    /// </summary>
    public int? GA_Attendees { get; set; }

    /// <summary>
    /// Gets or sets the price for general admission events (optional).
    /// </summary>
    public decimal? GA_Price { get; set; }

    /// <summary>
    /// Gets or sets the capacity override for general admission events (optional).
    /// </summary>
    public int? GA_CapacityOverride { get; set; }

    /// <summary>
    /// Gets or sets the capacity override for section-based events (optional).
    /// </summary>
    public int? SB_CapacityOverride { get; set; }
}
