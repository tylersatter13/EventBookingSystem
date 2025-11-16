using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    public class EventCapacity
    {
        public int EventId { get; set; }
        public int Capacity { get; set; }
        public int Remaining { get; set; }

        // Navigation property
        public EventBase Event { get; set; }
    }
}
