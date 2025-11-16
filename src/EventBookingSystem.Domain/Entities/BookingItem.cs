using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    public class BookingItem
    {
        public int Id { get; set; }
        public Booking Booking { get; set; }
        public EventSeat EventSeat { get; set; }
        public EventSectionInventory EventSection { get; set; }
        public int Quantity { get; set; }

        // Navigation properties


    }
}
