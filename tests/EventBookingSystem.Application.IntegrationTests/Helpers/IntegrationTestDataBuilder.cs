using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Application.IntegrationTests.Helpers
{
    /// <summary>
    /// Helper class to build test data for integration tests.
    /// </summary>
    public static class IntegrationTestDataBuilder
    {
        /// <summary>
        /// Creates a test user.
        /// </summary>
        public static User CreateUser(int id = 1, string name = "Test User", string email = "test@example.com")
        {
            return new User
            {
                Id = id,
                Name = name,
                Email = email,
                PhoneNumber = "555-1234"
            };
        }

        /// <summary>
        /// Creates a test venue with sections and seats.
        /// </summary>
        public static Venue CreateVenue(int id = 1, string name = "Test Venue", int sectionCount = 2, int seatsPerSection = 100)
        {
            var venue = new Venue
            {
                Id = id,
                Name = name,
                Address = "123 Test St",
                VenueSections = new List<VenueSection>()
            };

            for (int s = 1; s <= sectionCount; s++)
            {
                var section = new VenueSection
                {
                    Id = s,
                    VenueId = venue.Id,
                    Name = $"Section {s}",
                    VenueSeats = new List<VenueSeat>()
                };

                for (int seat = 1; seat <= seatsPerSection; seat++)
                {
                    int row = (seat - 1) / 10 + 1;
                    int seatInRow = (seat - 1) % 10 + 1;
                    
                    section.VenueSeats.Add(new VenueSeat
                    {
                        Id = (s - 1) * seatsPerSection + seat,
                        VenueSectionId = section.Id,
                        Row = $"{(char)('A' + row - 1)}",
                        SeatNumber = seatInRow.ToString(),
                        SeatLabel = $"{(char)('A' + row - 1)}{seatInRow}"
                    });
                }

                venue.VenueSections.Add(section);
            }

            return venue;
        }

        /// <summary>
        /// Creates a general admission event.
        /// </summary>
        public static GeneralAdmissionEvent CreateGeneralAdmissionEvent(
            int id = 1,
            int venueId = 1,
            string name = "Test Concert",
            int capacity = 1000,
            decimal price = 50m)
        {
            return new GeneralAdmissionEvent
            {
                Id = id,
                VenueId = venueId,
                Name = name,
                StartsAt = DateTime.UtcNow.AddDays(30),
                EstimatedAttendance = capacity,
                Capacity = capacity,
                Price = price
            };
        }

        /// <summary>
        /// Creates a section-based event.
        /// NOTE: VenueSection navigation properties should be set AFTER venue is saved to database.
        /// </summary>
        public static SectionBasedEvent CreateSectionBasedEvent(
            int id = 1,
            int venueId = 1,
            string name = "Test Game",
            params (int sectionId, int capacity, decimal price)[] sections)
        {
            var evnt = new SectionBasedEvent
            {
                Id = id,
                VenueId = venueId,
                Name = name,
                StartsAt = DateTime.UtcNow.AddDays(30),
                EstimatedAttendance = sections.Sum(s => s.capacity),
                SectionInventories = new List<EventSectionInventory>()
            };

            foreach (var (sectionId, capacity, price) in sections)
            {
                evnt.SectionInventories.Add(new EventSectionInventory
                {
                    VenueSectionId = sectionId,
                    Capacity = capacity,
                    Price = price
                    // DO NOT set VenueSection navigation property here
                    // It will cause FK constraint errors since these are dummy objects
                });
            }

            return evnt;
        }

        /// <summary>
        /// Creates a reserved seating event.
        /// NOTE: VenueSeat navigation properties should be set AFTER venue is saved to database.
        /// </summary>
        public static ReservedSeatingEvent CreateReservedSeatingEvent(
            int id = 1,
            int venueId = 1,
            string name = "Test Play",
            int numberOfSeats = 100)
        {
            var evnt = new ReservedSeatingEvent
            {
                Id = id,
                VenueId = venueId,
                Name = name,
                StartsAt = DateTime.UtcNow.AddDays(30),
                EstimatedAttendance = numberOfSeats,
                Seats = new List<EventSeat>()
            };

            for (int i = 1; i <= numberOfSeats; i++)
            {
                evnt.Seats.Add(new EventSeat
                {
                    Id = i,
                    VenueSeatId = i,
                    Status = SeatStatus.Available
                    // DO NOT set VenueSeat navigation property here
                    // It will cause FK constraint errors since these are dummy objects
                });
            }

            return evnt;
        }
    }
}
