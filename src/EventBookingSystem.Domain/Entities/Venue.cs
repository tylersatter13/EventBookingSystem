using System;
using System.Collections.Generic;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    public class Venue
    {
        //public required int Id { get; set; }
        /// <summary>
        /// Gets or sets the name associated with this instance.
        /// </summary>
        public required string Name { get; set; }

        public required int MaxCapacity { get; set; }
        /// <summary>
        /// Gets or sets the address associated with a venue.
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Get or set the sections associated with  a venue
        /// </summary>
        //  public List<VenueSection>? VenueSections { get; set; }
        /// <summary>
        /// Gets or sets the collection of seats available in the venue.
        /// </summary>
       // public List<VenueSeat>? VenueSeats { get; set; }
        /// <summary>
        /// Gets or sets the collection of events scheduled at the venue.
        /// </summary>
        public List<Event> Events { get; set; } = new List<Event>(); // Always have a list even if there is no

       
        /// <summary>
        /// Attempts to add a new event to the venue's schedule, ensuring there are no time conflicts and that the event
        /// does not exceed the venue's maximum capacity.
        /// </summary>
        /// <remarks>This method checks for overlapping event times and capacity constraints before adding
        /// the event. If the event cannot be scheduled due to conflicts or capacity limits, an exception is thrown and
        /// the event is not added.</remarks>
        /// <param name="evnt">The event to be scheduled. Must not be null. The event's time and estimated attendance are validated against
        /// existing events and venue capacity.</param>
        /// <exception cref="InvalidOperationException">Thrown if the event conflicts with an existing scheduled event or if its estimated attendance exceeds the
        /// venue's maximum capacity.</exception>
        public void BookEvent(Event evnt)
        {         
            var conflictingEvents = Events.Where(e =>
                (evnt.StartsAt < e.EndsAt) && (e.StartsAt < evnt.EndsAt));
            if (conflictingEvents.Any())
            {
                throw new InvalidOperationException("The event conflicts with existing scheduled events at the venue.");
            }
            if( evnt.EstimatedAttendance > MaxCapacity)
            {
                throw new InvalidOperationException("The event exceeds the venue's maximum capacity.");
            }
            Events.Add(evnt);
        }
    }
}
