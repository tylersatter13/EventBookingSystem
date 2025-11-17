namespace EventBookingSystem.Infrastructure.Models;

/// <summary>
/// DTO for EventSectionInventory entity.
/// </summary>
public class EventSectionInventoryDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int VenueSectionId { get; set; }
    public int Capacity { get; set; }
    public int Booked { get; set; }
    public decimal? Price { get; set; }
    public string AllocationMode { get; set; } = "GeneralAdmission";
}
