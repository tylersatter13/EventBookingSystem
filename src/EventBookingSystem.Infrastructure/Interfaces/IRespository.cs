using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Infrastructure.Interfaces
{
    public interface IRespository<TEntity> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    }
}
