using EventBookingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Infrastructure.Interfaces
{
    public interface IGeneralAdmissionEventRepository: IRespository<GeneralAdmissionEvent>
    {
        /// <summary>
        /// Updates an existing event.
        /// </summary>
        /// <param name="entity">The event to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated event.</returns>
        Task<GeneralAdmissionEvent> UpdateAsync(EventBase entity, CancellationToken cancellationToken = default);
    }
}
