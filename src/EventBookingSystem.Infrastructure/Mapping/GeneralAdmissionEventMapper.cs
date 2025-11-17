using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Mapping;

/// <summary>
/// Maps between GeneralAdmissionEvent domain entity and GeneralAdmissionEventDto.
/// Provides focused mapping for GA-specific operations.
/// </summary>
public static class GeneralAdmissionEventMapper
{
    /// <summary>
    /// Converts a GeneralAdmissionEventDto to a GeneralAdmissionEvent domain entity.
    /// </summary>
    public static GeneralAdmissionEvent ToDomain(GeneralAdmissionEventDto dto)
    {
        var gaEvent = new GeneralAdmissionEvent
        {
            Id = dto.Id,
            VenueId = dto.VenueId,
            Name = dto.Name,
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt,
            EstimatedAttendance = dto.EstimatedAttendance,
            Capacity = dto.Capacity,
            Price = dto.Price
        };

        // Use reflection to set the private _attendees field
        var attendeesField = typeof(GeneralAdmissionEvent).GetField("_attendees", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        attendeesField?.SetValue(gaEvent, dto.Attendees);

        return gaEvent;
    }

    /// <summary>
    /// Converts a GeneralAdmissionEvent domain entity to a GeneralAdmissionEventDto.
    /// </summary>
    public static GeneralAdmissionEventDto ToDto(GeneralAdmissionEvent gaEvent)
    {
        return new GeneralAdmissionEventDto
        {
            Id = gaEvent.Id,
            VenueId = gaEvent.VenueId,
            Name = gaEvent.Name,
            StartsAt = gaEvent.StartsAt,
            EndsAt = gaEvent.EndsAt,
            EstimatedAttendance = gaEvent.EstimatedAttendance,
            Capacity = gaEvent.Capacity,
            Attendees = gaEvent.Attendees,
            Price = gaEvent.Price
        };
    }

    /// <summary>
    /// Converts a GeneralAdmissionEventDto to EventDto (for TPH storage).
    /// </summary>
    public static EventDto ToEventDto(GeneralAdmissionEventDto gaDto)
    {
        return new EventDto
        {
            Id = gaDto.Id,
            VenueId = gaDto.VenueId,
            Name = gaDto.Name,
            StartsAt = gaDto.StartsAt,
            EndsAt = gaDto.EndsAt,
            EstimatedAttendance = gaDto.EstimatedAttendance,
            EventType = "GeneralAdmission",
            GA_Capacity = gaDto.Capacity,
            GA_Attendees = gaDto.Attendees,
            GA_Price = gaDto.Price,
            GA_CapacityOverride = null // Not used in current domain model
        };
    }

    /// <summary>
    /// Converts an EventDto to GeneralAdmissionEventDto (from TPH storage).
    /// </summary>
    public static GeneralAdmissionEventDto FromEventDto(EventDto eventDto)
    {
        if (eventDto.EventType != "GeneralAdmission")
        {
            throw new InvalidOperationException(
                $"Cannot convert EventDto of type '{eventDto.EventType}' to GeneralAdmissionEventDto. Expected 'GeneralAdmission'.");
        }

        return new GeneralAdmissionEventDto
        {
            Id = eventDto.Id,
            VenueId = eventDto.VenueId,
            Name = eventDto.Name,
            StartsAt = eventDto.StartsAt,
            EndsAt = eventDto.EndsAt,
            EstimatedAttendance = eventDto.EstimatedAttendance,
            Capacity = eventDto.GA_Capacity ?? 0,
            Attendees = eventDto.GA_Attendees ?? 0,
            Price = eventDto.GA_Price
        };
    }
}
