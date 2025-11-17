using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Mapping;

/// <summary>
/// Maps between BookingItem domain entity and BookingItemDto.
/// </summary>
public static class BookingItemMapper
{
    /// <summary>
    /// Converts a BookingItemDto to a BookingItem domain entity.
    /// </summary>
    public static BookingItem ToDomain(BookingItemDto dto)
    {
        return new BookingItem
        {
            Id = dto.Id,
            Quantity = dto.Quantity
            // Navigation properties (EventSeat, EventSection, Booking) are loaded separately
        };
    }

    /// <summary>
    /// Converts a BookingItem domain entity to a BookingItemDto.
    /// </summary>
    public static BookingItemDto ToDto(BookingItem bookingItem)
    {
        return new BookingItemDto
        {
            Id = bookingItem.Id,
            BookingId = bookingItem.Booking?.Id ?? 0,
            EventSeatId = bookingItem.EventSeat?.Id,
            EventSectionInventoryId = bookingItem.EventSection?.Id,
            Quantity = bookingItem.Quantity
        };
    }
}
