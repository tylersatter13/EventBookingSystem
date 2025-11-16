using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    public enum PaymentStatus
    {
        Pending,
        Paid,
        Refunded,
        Failed
    }
}
