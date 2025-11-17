namespace EventBookingSystem.Infrastructure.Models;

/// <summary>
/// DTO for EventSectionInventory entity.
/// </summary>
public class EventSectionInventoryDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the event section inventory.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the event ID associated with the section inventory.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Gets or sets the venue section ID associated with the section inventory.
    /// </summary>
    public int VenueSectionId { get; set; }

    /// <summary>
    /// Gets or sets the capacity for the section inventory.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Gets or sets the number of booked seats in the section inventory.
    /// </summary>
    public int Booked { get; set; }

    /// <summary>
    /// Gets or sets the price for the section inventory (optional).
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Gets or sets the allocation mode for the section inventory (e.g., GeneralAdmission).
    /// </summary>
    public string AllocationMode { get; set; } = "GeneralAdmission";
}
