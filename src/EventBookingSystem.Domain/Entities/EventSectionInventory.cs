using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    public class EventSectionInventory
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int VenueSectionId { get; set; }
        public int Capacity { get; set; }
        public int Remaining { get; set; }

        // Navigation properties
        public Event Event { get; set; }
        public VenueSection VenueSection { get; set; }
        public ICollection<BookingItem> BookingItems { get; set; }
    }
}
