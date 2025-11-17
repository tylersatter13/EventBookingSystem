using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Mapping;

/// <summary>
/// Maps between EventSeat domain entity and EventSeatDto.
/// </summary>
public static class EventSeatMapper
{
    /// <summary>
    /// Converts an EventSeatDto to an EventSeat domain entity.
    /// </summary>
    public static EventSeat ToDomain(EventSeatDto dto)
    {
        return new EventSeat
        {
            Id = dto.Id,
            EventId = dto.EventId,
            VenueSeatId = dto.VenueSeatId,
            Status = Enum.Parse<SeatStatus>(dto.Status)
        };
    }

    /// <summary>
    /// Converts an EventSeat domain entity to an EventSeatDto.
    /// </summary>
    public static EventSeatDto ToDto(EventSeat seat)
    {
        return new EventSeatDto
        {
            Id = seat.Id,
            EventId = seat.EventId,
            VenueSeatId = seat.VenueSeatId,
            Status = seat.Status.ToString()
        };
    }
}
