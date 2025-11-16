using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventBookingSystem.Domain.Entities
{
    public class Venue
    {
        /// <summary>
        /// Gets or sets the name associated with this instance.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the address associated with a venue.
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Gets or sets the sections associated with a venue.
        /// </summary>
        public List<VenueSection> VenueSections { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the collection of events scheduled at the venue.
        /// </summary>
        public List<EventBase> Events { get; set; } = new();
        
        /// <summary>
        /// Gets the total capacity of the venue based on all sections and seats.
        /// If no sections are defined, returns 0.
        /// </summary>
        public int MaxCapacity => VenueSections?.Sum(s => s.Capacity) ?? 0;
        
        /// <summary>
        /// Gets the total number of individual seats across all sections.
        /// </summary>
        public int TotalSeats => VenueSections?.Sum(s => s.VenueSeats?.Count ?? 0) ?? 0;
        
        /// <summary>
        /// Gets all general admission events at this venue.
        /// </summary>
        public IEnumerable<GeneralAdmissionEvent> GetGeneralAdmissionEvents()
        {
            return Events.OfType<GeneralAdmissionEvent>();
        }
        
        /// <summary>
        /// Gets all section-based events at this venue.
        /// </summary>
        public IEnumerable<SectionBasedEvent> GetSectionBasedEvents()
        {
            return Events.OfType<SectionBasedEvent>();
        }
        
        /// <summary>
        /// Gets all reserved seating events at this venue.
        /// </summary>
        public IEnumerable<ReservedSeatingEvent> GetReservedSeatingEvents()
        {
            return Events.OfType<ReservedSeatingEvent>();
        }

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
        internal void BookEvent(EventBase evnt)
        {         
            Events.Add(evnt);
        }
    }
}
