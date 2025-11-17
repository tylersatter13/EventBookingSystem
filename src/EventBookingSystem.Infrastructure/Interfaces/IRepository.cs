using System;
using System.Collections.Generic;

namespace EventBookingSystem.Infrastructure.Interfaces
{
    /// <summary>
    /// Generic repository interface for basic CRUD operations on entities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets an entity by its unique identifier.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The entity if found; otherwise, null.</returns>
        Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new entity to the repository.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The added entity.</returns>
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    }
}
