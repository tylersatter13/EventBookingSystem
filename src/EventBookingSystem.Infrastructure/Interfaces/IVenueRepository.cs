using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Infrastructure.Interfaces
{
    /// <summary>
    /// Repository interface for <see cref="Venue"/> entity operations.
    /// </summary>
    public interface IVenueRepository : IRepository<Venue>
    {
        // Inherits CRUD operations from IRepository<Venue>
    }
}
