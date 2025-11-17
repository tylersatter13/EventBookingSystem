using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Mapping;

/// <summary>
/// Maps between User domain entity and UserDto.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Converts a UserDto to a User domain entity.
    /// </summary>
    public static User ToDomain(UserDto dto)
    {
        return new User
        {
            Id = dto.Id,
            Name = dto.Name,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Bookings = new List<Booking>()
        };
    }

    /// <summary>
    /// Converts a User domain entity to a UserDto.
    /// </summary>
    public static UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        };
    }
}
