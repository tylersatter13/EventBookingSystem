using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Infrastructure.Tests.Helpers;

/// <summary>
/// Helper class for creating test event data.
/// </summary>
public static class EventTestDataBuilder
{
    /// <summary>
    /// Creates a GeneralAdmissionEvent with default values.
    /// </summary>
    public static GeneralAdmissionEvent CreateGeneralAdmissionEvent(
        string name = "Test GA Event",
        int venueId = 1,
        int capacity = 100,
        DateTime? startsAt = null)
    {
        return new GeneralAdmissionEvent
        {
            Name = name,
            VenueId = venueId,
            Capacity = capacity,
            StartsAt = startsAt ?? DateTime.Now.AddDays(1),
            EndsAt = startsAt?.AddHours(3) ?? DateTime.Now.AddDays(1).AddHours(3),
            EstimatedAttendance = capacity / 2,
            Price = 50.00m
        };
    }

    /// <summary>
    /// Creates a SectionBasedEvent with section inventories.
    /// </summary>
    public static SectionBasedEvent CreateSectionBasedEvent(
        string name = "Test Section Event",
        int venueId = 1,
        DateTime? startsAt = null,
        params (int sectionId, int capacity, decimal price)[] sections)
    {
        var sbEvent = new SectionBasedEvent
        {
            Name = name,
            VenueId = venueId,
            StartsAt = startsAt ?? DateTime.Now.AddDays(1),
            EndsAt = startsAt?.AddHours(3) ?? DateTime.Now.AddDays(1).AddHours(3),
            EstimatedAttendance = sections.Sum(s => s.capacity)
        };

        foreach (var (sectionId, capacity, price) in sections)
        {
            sbEvent.SectionInventories.Add(new EventSectionInventory
            {
                VenueSectionId = sectionId,
                Capacity = capacity,
                Price = price,
                AllocationMode = SeatAllocationMode.GeneralAdmission
            });
        }

        return sbEvent;
    }

    /// <summary>
    /// Creates a ReservedSeatingEvent with event seats.
    /// </summary>
    public static ReservedSeatingEvent CreateReservedSeatingEvent(
        string name = "Test Reserved Event",
        int venueId = 1,
        DateTime? startsAt = null,
        params int[] venueSeatIds)
    {
        var rsEvent = new ReservedSeatingEvent
        {
            Name = name,
            VenueId = venueId,
            StartsAt = startsAt ?? DateTime.Now.AddDays(1),
            EndsAt = startsAt?.AddHours(3) ?? DateTime.Now.AddDays(1).AddHours(3),
            EstimatedAttendance = venueSeatIds.Length
        };

        foreach (var venueSeatId in venueSeatIds)
        {
            rsEvent.Seats.Add(new EventSeat
            {
                VenueSeatId = venueSeatId,
                Status = SeatStatus.Available
            });
        }

        return rsEvent;
    }
}
