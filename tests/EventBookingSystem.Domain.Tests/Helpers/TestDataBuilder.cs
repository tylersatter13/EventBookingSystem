using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Tests.Helpers
{
    /// <summary>
    /// Helper class for creating test data with proper structure.
    /// </summary>
    public static class TestDataBuilder
    {
        /// <summary>
        /// Creates a venue with a single section containing the specified capacity.
        /// </summary>
        public static Venue CreateVenueWithCapacity(string name, string address, int capacity)
        {
            var venue = new Venue
            {
                Name = name,
                Address = address,
                VenueSections = new List<VenueSection>
                {
                    new VenueSection
                    {
                        Name = "Main Section",
                        VenueSeats = CreateSeats(capacity)
                    }
                }
            };

            return venue;
        }

        /// <summary>
        /// Creates a venue with multiple sections.
        /// </summary>
        public static Venue CreateVenueWithSections(string name, string address, params (string sectionName, int capacity)[] sections)
        {
            var venue = new Venue
            {
                Name = name,
                Address = address,
                VenueSections = new List<VenueSection>()
            };

            foreach (var (sectionName, capacity) in sections)
            {
                venue.VenueSections.Add(new VenueSection
                {
                    Name = sectionName,
                    VenueSeats = CreateSeats(capacity)
                });
            }

            return venue;
        }

        /// <summary>
        /// Creates the specified number of venue seats.
        /// </summary>
        private static List<VenueSeat> CreateSeats(int count)
        {
            var seats = new List<VenueSeat>();
            int seatsPerRow = 10;

            for (int i = 1; i <= count; i++)
            {
                int row = (i - 1) / seatsPerRow + 1;
                int seatNumber = (i - 1) % seatsPerRow + 1;

                seats.Add(new VenueSeat
                {
                    Id = i,
                    Row = $"{(char)('A' + (row - 1) % 26)}",
                    SeatNumber = seatNumber.ToString(),
                    SeatLabel = $"{(char)('A' + (row - 1) % 26)}{seatNumber}"
                });
            }

            return seats;
        }

        // ========== NEW: Create GeneralAdmissionEvent ========= =

        /// <summary>
        /// Creates a simple general admission event.
        /// </summary>
        public static GeneralAdmissionEvent CreateGeneralAdmissionEvent(
            Venue venue,
            string eventName,
            DateTime startsAt,
            int? capacity = null,
            decimal? price = null)
        {
            return new GeneralAdmissionEvent
            {
                Name = eventName,
                StartsAt = startsAt,
                Venue = venue,
                VenueId = 1,
                Capacity = capacity ?? venue.MaxCapacity,
                Price = price,
                EstimatedAttendance = capacity ?? venue.MaxCapacity
            };
        }

        // ========== NEW: Create SectionBasedEvent ========= =

        /// <summary>
        /// Creates a section-based event with inventories matching the venue's sections.
        /// </summary>
        public static SectionBasedEvent CreateSectionBasedEvent(
            Venue venue,
            string eventName,
            DateTime startsAt,
            params (int sectionId, decimal price)[] sectionPricing)
        {
            var evnt = new SectionBasedEvent
            {
                Name = eventName,
                StartsAt = startsAt,
                Venue = venue,
                VenueId = 1,
                EstimatedAttendance = venue.MaxCapacity,
                SectionInventories = new List<EventSectionInventory>()
            };

            // Create section inventories matching venue sections
            int sectionId = 1;
            foreach (var section in venue.VenueSections)
            {
                var pricing = sectionPricing.FirstOrDefault(sp => sp.sectionId == sectionId);

                evnt.SectionInventories.Add(new EventSectionInventory
                {
                    VenueSectionId = sectionId,
                    Capacity = section.Capacity,
                    VenueSection = section,
                    Price = pricing != default ? pricing.price : null
                });

                sectionId++;
            }

            return evnt;
        }

        // ========== NEW: Create ReservedSeatingEvent ========= =

        /// <summary>
        /// Creates a reserved seating event with EventSeats for all venue seats.
        /// </summary>
        public static ReservedSeatingEvent CreateReservedSeatingEvent(
            Venue venue,
            string eventName,
            DateTime startsAt)
        {
            var evnt = new ReservedSeatingEvent
            {
                Name = eventName,
                StartsAt = startsAt,
                Venue = venue,
                VenueId = 1,
                EstimatedAttendance = venue.MaxCapacity,
                Seats = new List<EventSeat>()
            };

            // Create EventSeats for all venue seats
            int eventSeatId = 1;
            foreach (var section in venue.VenueSections)
            {
                foreach (var venueSeat in section.VenueSeats)
                {
                    evnt.Seats.Add(new EventSeat
                    {
                        Id = eventSeatId++,
                        VenueSeatId = venueSeat.Id,
                        Status = SeatStatus.Available,
                        VenueSeat = venueSeat
                    });
                }
            }

            return evnt;
        }
    }  
}
