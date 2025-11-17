using AwesomeAssertions;
using EventBookingSystem.Application.IntegrationTests.Fixtures;
using EventBookingSystem.Application.IntegrationTests.Helpers;
using EventBookingSystem.Application.Services;
using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Application.IntegrationTests.Services
{
    /// <summary>
    /// Integration tests for EventQueryService.
    /// Tests the complete workflow from service ? repositories ? database ? response.
    /// </summary>
    [TestClass]
    public class EventQueryServiceIntegrationTests
    {
        private DatabaseFixture _database = null!;
        private EventQueryService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _database = new DatabaseFixture();
            _service = new EventQueryService(_database.EventRepository, _database.VenueRepository);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _database?.Dispose();
        }

        [TestMethod]
        public async Task GetFutureEventsWithAvailabilityAsync_WithMultipleEvents_ReturnsCorrectAvailability()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Main Arena");
            await _database.VenueRepository.AddAsync(venue);

            // Create a past event (should be filtered out)
            var pastEvent = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(1, venue.Id, "Past Concert", 100, 50m);
            pastEvent.Venue = venue;
            pastEvent.StartsAt = DateTime.UtcNow.AddDays(-7);
            await _database.EventRepository.AddAsync(pastEvent);

            // Create future events
            var futureEvent1 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(2, venue.Id, "Future Concert", 200, 75m);
            futureEvent1.Venue = venue;
            futureEvent1.StartsAt = DateTime.UtcNow.AddDays(30);
            futureEvent1.ReserveTickets(50); // 50 tickets booked
            await _database.EventRepository.AddAsync(futureEvent1);

            var futureEvent2 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(3, venue.Id, "Another Concert", 150, 60m);
            futureEvent2.Venue = venue;
            futureEvent2.StartsAt = DateTime.UtcNow.AddDays(45);
            futureEvent2.ReserveTickets(100); // 100 tickets booked
            await _database.EventRepository.AddAsync(futureEvent2);

            // Act
            var result = await _service.GetFutureEventsWithAvailabilityAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2, because: "only future events should be returned");
            
            var event1Dto = result.First(e => e.Name == "Future Concert");
            event1Dto.TotalCapacity.Should().Be(200);
            event1Dto.AvailableCapacity.Should().Be(150);
            event1Dto.ReservedCount.Should().Be(50);
            event1Dto.IsAvailable.Should().BeTrue();
            event1Dto.AvailabilityPercentage.Should().Be(75.0);

            var event2Dto = result.First(e => e.Name == "Another Concert");
            event2Dto.AvailableCapacity.Should().Be(50);
            event2Dto.ReservedCount.Should().Be(100);
        }

        [TestMethod]
        public async Task GetFutureEventsByVenueAsync_WithMultipleVenues_ReturnsOnlyVenueEvents()
        {
            // Arrange
            var venue1 = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Venue One");
            var venue2 = IntegrationTestDataBuilder.CreateVenue(id: 2, name: "Venue Two");
            await _database.VenueRepository.AddAsync(venue1);
            await _database.VenueRepository.AddAsync(venue2);

            // Events at venue1
            var event1 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(1, venue1.Id, "Event at Venue 1", 100, 50m);
            event1.Venue = venue1;
            event1.StartsAt = DateTime.UtcNow.AddDays(10);
            await _database.EventRepository.AddAsync(event1);

            // Events at venue2
            var event2 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(2, venue2.Id, "Event at Venue 2", 150, 60m);
            event2.Venue = venue2;
            event2.StartsAt = DateTime.UtcNow.AddDays(15);
            await _database.EventRepository.AddAsync(event2);

            // Act
            var result = await _service.GetFutureEventsByVenueAsync(venue1.Id);

            // Assert
            result.Should().HaveCount(1);
            result.First().VenueName.Should().Contain("Venue One");
            result.First().Name.Should().Be("Event at Venue 1");
        }

        [TestMethod]
        public async Task GetEventAvailabilityAsync_SectionBasedEvent_ReturnsDetailedSectionInfo()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Stadium", sectionCount: 3);
            await _database.VenueRepository.AddAsync(venue);

            var sectionEvent = IntegrationTestDataBuilder.CreateSectionBasedEvent(
                1,
                venue.Id,
                "Big Game",
                (1, 500, 100m),
                (2, 300, 75m),
                (3, 200, 50m));
            sectionEvent.Venue = venue;
            sectionEvent.StartsAt = DateTime.UtcNow.AddDays(20);

            // Reserve some tickets in each section
            sectionEvent.ReserveInSection(1, 150);
            sectionEvent.ReserveInSection(2, 100);
            
            await _database.EventRepository.AddAsync(sectionEvent);

            // Act
            var result = await _service.GetEventAvailabilityAsync(sectionEvent.Id);

            // Assert
            result.Should().NotBeNull();
            result!.EventType.Should().Be("SectionBased");
            result.Sections.Should().HaveCount(3);
            
            var section1 = result.Sections.FirstOrDefault(s => s.SectionId == 1);
            section1.Should().NotBeNull();
            section1!.Capacity.Should().Be(500);
            section1.Booked.Should().Be(150);
            section1.Available.Should().Be(350);
            section1.Price.Should().Be(100m);
            section1.IsAvailable.Should().BeTrue();
            section1.AvailabilityPercentage.Should().Be(70.0);

            result.TotalCapacity.Should().Be(1000);
            result.AvailableCapacity.Should().Be(750);
            result.ReservedCount.Should().Be(250);
        }

        [TestMethod]
        public async Task GetEventAvailabilityAsync_FullyBookedEvent_ShowsNoAvailability()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Small Venue");
            await _database.VenueRepository.AddAsync(venue);

            var event1 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(1, venue.Id, "Sold Out Show", 50, 80m);
            event1.Venue = venue;
            event1.StartsAt = DateTime.UtcNow.AddDays(7);
            event1.ReserveTickets(50); // Fully booked
            await _database.EventRepository.AddAsync(event1);

            // Act
            var result = await _service.GetEventAvailabilityAsync(event1.Id);

            // Assert
            result.Should().NotBeNull();
            result!.TotalCapacity.Should().Be(50);
            result.AvailableCapacity.Should().Be(0);
            result.ReservedCount.Should().Be(50);
            result.IsAvailable.Should().BeFalse();
            result.AvailabilityPercentage.Should().Be(0.0);
        }

        [TestMethod]
        public async Task GetFutureEventsWithAvailabilityAsync_NoFutureEvents_ReturnsEmpty()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Empty Venue");
            await _database.VenueRepository.AddAsync(venue);

            // Only create past events
            var pastEvent = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(1, venue.Id, "Past Event", 100, 50m);
            pastEvent.Venue = venue;
            pastEvent.StartsAt = DateTime.UtcNow.AddDays(-30);
            await _database.EventRepository.AddAsync(pastEvent);

            // Act
            var result = await _service.GetFutureEventsWithAvailabilityAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetEventAvailabilityAsync_NonExistentEvent_ReturnsNull()
        {
            // Arrange
            var nonExistentId = 999;

            // Act
            var result = await _service.GetEventAvailabilityAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public async Task GetFutureEventsWithAvailabilityAsync_EventsOrderedByDate_ReturnsInCorrectOrder()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Calendar Venue");
            await _database.VenueRepository.AddAsync(venue);

            var event1 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(1, venue.Id, "Event in 45 days", 100, 50m);
            event1.Venue = venue;
            event1.StartsAt = DateTime.UtcNow.AddDays(45);
            await _database.EventRepository.AddAsync(event1);

            var event2 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(2, venue.Id, "Event in 15 days", 100, 50m);
            event2.Venue = venue;
            event2.StartsAt = DateTime.UtcNow.AddDays(15);
            await _database.EventRepository.AddAsync(event2);

            var event3 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(3, venue.Id, "Event in 30 days", 100, 50m);
            event3.Venue = venue;
            event3.StartsAt = DateTime.UtcNow.AddDays(30);
            await _database.EventRepository.AddAsync(event3);

            // Act
            var result = await _service.GetFutureEventsWithAvailabilityAsync();

            // Assert
            result.Should().HaveCount(3);
            var orderedResult = result.ToList();
            orderedResult[0].Name.Should().Be("Event in 15 days");
            orderedResult[1].Name.Should().Be("Event in 30 days");
            orderedResult[2].Name.Should().Be("Event in 45 days");
        }
    }
}
