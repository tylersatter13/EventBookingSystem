using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Mapping;

/// <summary>
/// Maps between Booking domain entity and BookingDto.
/// </summary>
public static class BookingMapper
{
    /// <summary>
    /// Converts a BookingDto to a Booking domain entity.
    /// </summary>
    public static Booking ToDomain(BookingDto dto)
    {
        return new Booking
        {
            Id = dto.Id,
            BookingType = Enum.Parse<BookingType>(dto.BookingType),
            PaymentStatus = Enum.Parse<PaymentStatus>(dto.PaymentStatus),
            TotalAmount = dto.TotalAmount,
            CreatedAt = DateTime.Parse(dto.CreatedAt),
            BookingItems = new List<BookingItem>()
        };
    }

    /// <summary>
    /// Converts a Booking domain entity to a BookingDto.
    /// </summary>
    public static BookingDto ToDto(Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            UserId = booking.User?.Id ?? 0,
            EventId = booking.Event?.Id ?? 0,
            BookingType = booking.BookingType.ToString(),
            PaymentStatus = booking.PaymentStatus.ToString(),
            TotalAmount = booking.TotalAmount,
            CreatedAt = booking.CreatedAt.ToString("o") // ISO 8601 format
        };
    }
}
