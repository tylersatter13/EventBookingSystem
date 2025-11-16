using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    public class EventSeat
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int VenueSeatId { get; set; }
        public SeatStatus Status { get; set; }
    }
}
