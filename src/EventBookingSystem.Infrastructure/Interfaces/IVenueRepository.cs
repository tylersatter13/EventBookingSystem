using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Infrastructure.Interfaces
{
    /// <summary>
    /// Repository interface for <see cref="Venue"/> entity operations.
    /// </summary>
    public interface IVenueRepository : IRespository<Venue>
    {
        // Inherits CRUD operations from IRespository<Venue>
    }
}
