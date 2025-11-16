using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    public class Event
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public string Name { get; set; }
        public EventType EventType { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }

        public int EstimatedAttendance { get; set; }

        public int SeatsReservered { get; set; } = 0;
        ///  public int? CapacityOverride { get; set; }
        // Navigation properties
        public Venue Venue { get; set; }

        internal void BookSeat()
        {
           SeatsReservered++;
        }
       // public ICollection<EventSeat> EventSeats { get; set; }
       // public ICollection<EventSectionInventory> EventSectionInventories { get; set; }
       // public ICollection<EventCapacity> EventCapacities { get; set; }
       // public ICollection<Booking> Bookings { get; set; }
    }
}
