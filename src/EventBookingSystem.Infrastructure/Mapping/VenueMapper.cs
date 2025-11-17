using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Mapping;

/// <summary>
/// Maps between Venue domain entity and VenueDto.
/// </summary>
public static class VenueMapper
{
    /// <summary>
    /// Converts a VenueDto to a Venue domain entity.
    /// </summary>
    public static Venue ToDomain(VenueDto dto)
    {
        return new Venue
        {
            Id = dto.Id,
            Name = dto.Name,
            Address = dto.Address
            // Note: VenueSections and Events are loaded separately and added later if needed
        };
    }

    /// <summary>
    /// Converts a VenueDto with its sections to a Venue domain entity.
    /// </summary>
    public static Venue ToDomain(VenueDto dto, IEnumerable<VenueSectionDto> sectionDtos)
    {
        var venue = ToDomain(dto);
        venue.VenueSections = sectionDtos.Select(VenueSectionMapper.ToDomain).ToList();
        return venue;
    }

    /// <summary>
    /// Converts a Venue domain entity to a VenueDto.
    /// </summary>
    public static VenueDto ToDto(Venue venue)
    {
        return new VenueDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Address = venue.Address
        };
    }
}