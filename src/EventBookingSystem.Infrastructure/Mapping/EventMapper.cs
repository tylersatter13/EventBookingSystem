using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Mapping;

/// <summary>
/// Maps between Event domain entities and EventDto using Table Per Hierarchy pattern.
/// </summary>
public static class EventMapper
{
    /// <summary>
    /// Converts an EventDto to the appropriate EventBase domain entity based on discriminator.
    /// </summary>
    public static EventBase ToDomain(EventDto dto)
    {
        return dto.EventType switch
        {
            "GeneralAdmission" => MapToGeneralAdmissionEvent(dto),
            "SectionBased" => MapToSectionBasedEvent(dto),
            "ReservedSeating" => MapToReservedSeatingEvent(dto),
            _ => throw new InvalidOperationException($"Unknown event type: {dto.EventType}")
        };
    }

    /// <summary>
    /// Converts an EventBase domain entity to EventDto.
    /// </summary>
    public static EventDto ToDto(EventBase eventBase)
    {
        var dto = new EventDto
        {
            Id = eventBase.Id,
            VenueId = eventBase.VenueId,
            Name = eventBase.Name,
            StartsAt = eventBase.StartsAt,
            EndsAt = eventBase.EndsAt,
            EstimatedAttendance = eventBase.EstimatedAttendance
        };

        switch (eventBase)
        {
            case GeneralAdmissionEvent gaEvent:
                dto.EventType = "GeneralAdmission";
                dto.GA_Capacity = gaEvent.Capacity;
                dto.GA_Attendees = gaEvent.Attendees;
                dto.GA_Price = gaEvent.Price;
                dto.GA_CapacityOverride = null; // Not used in current domain model
                break;

            case SectionBasedEvent sbEvent:
                dto.EventType = "SectionBased";
                dto.SB_CapacityOverride = sbEvent.CapacityOverride;
                break;

            case ReservedSeatingEvent rsEvent:
                dto.EventType = "ReservedSeating";
                break;

            default:
                throw new InvalidOperationException($"Unknown event type: {eventBase.GetType().Name}");
        }

        return dto;
    }

    private static GeneralAdmissionEvent MapToGeneralAdmissionEvent(EventDto dto)
    {
        var gaEvent = new GeneralAdmissionEvent
        {
            Id = dto.Id,
            VenueId = dto.VenueId,
            Name = dto.Name,
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt,
            EstimatedAttendance = dto.EstimatedAttendance,
            Capacity = dto.GA_Capacity ?? 0,
            Price = dto.GA_Price
        };

        // Use reflection to set the private _attendees field
        if (dto.GA_Attendees.HasValue)
        {
            var attendeesField = typeof(GeneralAdmissionEvent).GetField("_attendees", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            attendeesField?.SetValue(gaEvent, dto.GA_Attendees.Value);
        }

        return gaEvent;
    }

    private static SectionBasedEvent MapToSectionBasedEvent(EventDto dto)
    {
        return new SectionBasedEvent
        {
            Id = dto.Id,
            VenueId = dto.VenueId,
            Name = dto.Name,
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt,
            EstimatedAttendance = dto.EstimatedAttendance,
            CapacityOverride = dto.SB_CapacityOverride
            // SectionInventories will be loaded separately
        };
    }

    private static ReservedSeatingEvent MapToReservedSeatingEvent(EventDto dto)
    {
        return new ReservedSeatingEvent
        {
            Id = dto.Id,
            VenueId = dto.VenueId,
            Name = dto.Name,
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt,
            EstimatedAttendance = dto.EstimatedAttendance
            // Seats will be loaded separately
        };
    }
}
