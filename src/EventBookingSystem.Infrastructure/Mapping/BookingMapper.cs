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
    /// Note: Creates stub User and Event with only IDs populated.
    /// Repository is responsible for loading full entities.
    /// </summary>
    public static Booking ToDomain(BookingDto dto)
    {
        return new Booking
        {
            Id = dto.Id,
            // Create stub User - will be replaced by repository with full entity
            User = new User
            {
                Id = dto.UserId,
                Name = string.Empty,
                Email = string.Empty,
                PhoneNumber = string.Empty,
                Bookings = new List<Booking>()
            },
            // Create stub Event - will be replaced by repository with full entity
            // Using GeneralAdmissionEvent as a placeholder since we can't determine type from DTO
            Event = new GeneralAdmissionEvent
            {
                Id = dto.EventId,
                Name = string.Empty,
                StartsAt = DateTime.MinValue,
                EstimatedAttendance = 0,
                Capacity = 0
            },
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
