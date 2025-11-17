using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Mapping;

/// <summary>
/// Maps between EventSectionInventory domain entity and EventSectionInventoryDto.
/// </summary>
public static class EventSectionInventoryMapper
{
    /// <summary>
    /// Converts an EventSectionInventoryDto to an EventSectionInventory domain entity.
    /// </summary>
    public static EventSectionInventory ToDomain(EventSectionInventoryDto dto)
    {
        var inventory = new EventSectionInventory
        {
            Id = dto.Id,
            EventId = dto.EventId,
            VenueSectionId = dto.VenueSectionId,
            Capacity = dto.Capacity,
            Price = dto.Price,
            AllocationMode = Enum.Parse<SeatAllocationMode>(dto.AllocationMode)
        };

        // Use reflection to set the private _booked field if needed
        if (dto.Booked > 0)
        {
            var bookedField = typeof(EventSectionInventory).GetField("_booked", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bookedField?.SetValue(inventory, dto.Booked);
        }

        return inventory;
    }

    /// <summary>
    /// Converts an EventSectionInventory domain entity to an EventSectionInventoryDto.
    /// </summary>
    public static EventSectionInventoryDto ToDto(EventSectionInventory inventory)
    {
        return new EventSectionInventoryDto
        {
            Id = inventory.Id,
            EventId = inventory.EventId,
            VenueSectionId = inventory.VenueSectionId,
            Capacity = inventory.Capacity,
            Booked = inventory.Booked,
            Price = inventory.Price,
            AllocationMode = inventory.AllocationMode.ToString()
        };
    }
}
