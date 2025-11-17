using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Mapping;

/// <summary>
/// Maps between VenueSection domain entity and VenueSectionDto.
/// </summary>
public static class VenueSectionMapper
{
    /// <summary>
    /// Converts a VenueSectionDto to a VenueSection domain entity.
    /// </summary>
    /// <param name="dto">The VenueSectionDto to convert.</param>
    /// <returns>A VenueSection domain entity.</returns>
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

    /// <summary>
    /// Converts a VenueSection domain entity to a VenueSectionDto.
    /// </summary>
    /// <param name="section">The VenueSection domain entity to convert.</param>
    /// <returns>A VenueSectionDto.</returns>
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