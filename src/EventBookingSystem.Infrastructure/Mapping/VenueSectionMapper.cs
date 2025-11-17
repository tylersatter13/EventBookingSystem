using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Mapping;

/// <summary>
/// Maps between VenueSection domain entity and VenueSectionDto.
/// </summary>
public static class VenueSectionMapper
{
    public static VenueSection ToDomain(VenueSectionDto dto)
    {
        return new VenueSection
        {
            Id = dto.Id,
            VenueId = dto.VenueId,
            Name = dto.Name
            // VenueSeats loaded separately if needed
        };
    }

    public static VenueSectionDto ToDto(VenueSection section)
    {
        return new VenueSectionDto
        {
            Id = section.Id,
            VenueId = section.VenueId,
            Name = section.Name
        };
    }
}