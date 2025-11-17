using AwesomeAssertions;
using EventBookingSystem.Application.Services;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Interfaces;
using Moq;

namespace EventBookingSystem.Application.Tests.Services
{
    [TestClass]
    public class EventQueryServiceTests
    {
        private Mock<IEventRepository> _mockEventRepository = null!;
        private Mock<IVenueRepository> _mockVenueRepository = null!;
        private EventQueryService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockEventRepository = new Mock<IEventRepository>();
            _mockVenueRepository = new Mock<IVenueRepository>();
            _service = new EventQueryService(_mockEventRepository.Object, _mockVenueRepository.Object);
        }

        #region GetFutureEventsWithAvailabilityAsync Tests

        [TestMethod]
        public async Task GetFutureEventsWithAvailabilityAsync_WithFutureEvents_ReturnsAvailabilityDetails()
        {
            // Arrange
            var venue = new Venue { Id = 1, Name = "Test Venue", Address = "123 Test St" };
            var futureEvent = new GeneralAdmissionEvent
            {
                Id = 1,
                VenueId = 1,
                Name = "Future Concert",
                StartsAt = DateTime.UtcNow.AddDays(30),
                Capacity = 100,
                Price = 50m,
                Venue = venue
            };
            futureEvent.ReserveTickets(25); // 25 tickets booked

            _mockEventRepository
                .Setup(x => x.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EventBase> { futureEvent });

            // Act
            var result = await _service.GetFutureEventsWithAvailabilityAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            
            var dto = result.First();
            dto.Id.Should().Be(1);
            dto.Name.Should().Be("Future Concert");
            dto.EventType.Should().Be("GeneralAdmission");
            dto.TotalCapacity.Should().Be(100);
            dto.AvailableCapacity.Should().Be(75);
            dto.ReservedCount.Should().Be(25);
            dto.Price.Should().Be(50m);
            dto.IsAvailable.Should().BeTrue();
            dto.AvailabilityPercentage.Should().Be(75.0);
        }

        [TestMethod]
        public async Task GetFutureEventsWithAvailabilityAsync_NoEvents_ReturnsEmptyCollection()
        {
            // Arrange
            _mockEventRepository
                .Setup(x => x.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EventBase>());

            // Act
            var result = await _service.GetFutureEventsWithAvailabilityAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetFutureEventsWithAvailabilityAsync_SoldOutEvent_ShowsZeroAvailability()
        {
            // Arrange
            var venue = new Venue { Id = 1, Name = "Small Venue", Address = "456 Small St" };
            var soldOutEvent = new GeneralAdmissionEvent
            {
                Id = 1,
                VenueId = 1,
                Name = "Sold Out Show",
                StartsAt = DateTime.UtcNow.AddDays(7),
                Capacity = 50,
                Price = 75m,
                Venue = venue
            };
            soldOutEvent.ReserveTickets(50); // Fully booked

            _mockEventRepository
                .Setup(x => x.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EventBase> { soldOutEvent });

            // Act
            var result = await _service.GetFutureEventsWithAvailabilityAsync();

            // Assert
            var dto = result.First();
            dto.AvailableCapacity.Should().Be(0);
            dto.IsAvailable.Should().BeFalse();
            dto.AvailabilityPercentage.Should().Be(0.0);
        }

        #endregion

        #region GetFutureEventsByVenueAsync Tests

        [TestMethod]
        public async Task GetFutureEventsByVenueAsync_ValidVenueId_ReturnsOnlyFutureEvents()
        {
            // Arrange
            var venueId = 1;
            var venue = new Venue { Id = venueId, Name = "Test Venue", Address = "789 Venue St" };
            
            var pastEvent = new GeneralAdmissionEvent
            {
                Id = 1,
                VenueId = venueId,
                Name = "Past Event",
                StartsAt = DateTime.UtcNow.AddDays(-7),
                Capacity = 100,
                Venue = venue
            };

            var futureEvent = new GeneralAdmissionEvent
            {
                Id = 2,
                VenueId = venueId,
                Name = "Future Event",
                StartsAt = DateTime.UtcNow.AddDays(7),
                Capacity = 150,
                Venue = venue
            };

            _mockEventRepository
                .Setup(x => x.GetByVenueIdAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EventBase> { pastEvent, futureEvent });

            // Act
            var result = await _service.GetFutureEventsByVenueAsync(venueId);

            // Assert
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Future Event");
        }

        [TestMethod]
        public async Task GetFutureEventsByVenueAsync_InvalidVenueId_ThrowsArgumentException()
        {
            // Arrange
            var venueId = 0;

            // Act
            Func<Task> act = async () => await _service.GetFutureEventsByVenueAsync(venueId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Venue ID must be greater than zero*");
        }

        #endregion

        #region GetEventAvailabilityAsync Tests

        [TestMethod]
        public async Task GetEventAvailabilityAsync_SectionBasedEvent_ReturnsSectionDetails()
        {
            // Arrange
            var eventId = 1;
            var venue = new Venue { Id = 1, Name = "Stadium", Address = "Sports Complex" };
            
            var vipSection = new VenueSection { Id = 1, Name = "VIP" };
            var generalSection = new VenueSection { Id = 2, Name = "General" };

            var sectionEvent = new SectionBasedEvent
            {
                Id = eventId,
                VenueId = 1,
                Name = "Big Game",
                StartsAt = DateTime.UtcNow.AddDays(14),
                Venue = venue,
                SectionInventories = new List<EventSectionInventory>
                {
                    new EventSectionInventory
                    {
                        VenueSectionId = 1,
                        VenueSection = vipSection,
                        Capacity = 100,
                        Price = 200m
                    },
                    new EventSectionInventory
                    {
                        VenueSectionId = 2,
                        VenueSection = generalSection,
                        Capacity = 500,
                        Price = 50m
                    }
                }
            };

            // Reserve some tickets
            sectionEvent.ReserveInSection(1, 25); // 25 VIP tickets
            sectionEvent.ReserveInSection(2, 150); // 150 general tickets

            _mockEventRepository
                .Setup(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sectionEvent);

            // Act
            var result = await _service.GetEventAvailabilityAsync(eventId);

            // Assert
            result.Should().NotBeNull();
            result!.EventType.Should().Be("SectionBased");
            result.Sections.Should().HaveCount(2);
            
            var vipDto = result.Sections.First(s => s.SectionName == "VIP");
            vipDto.Capacity.Should().Be(100);
            vipDto.Booked.Should().Be(25);
            vipDto.Available.Should().Be(75);
            vipDto.Price.Should().Be(200m);
            vipDto.IsAvailable.Should().BeTrue();
            vipDto.AvailabilityPercentage.Should().Be(75.0);

            var generalDto = result.Sections.First(s => s.SectionName == "General");
            generalDto.Capacity.Should().Be(500);
            generalDto.Booked.Should().Be(150);
            generalDto.Available.Should().Be(350);

            result.TotalCapacity.Should().Be(600);
            result.AvailableCapacity.Should().Be(425);
            result.ReservedCount.Should().Be(175);
        }

        [TestMethod]
        public async Task GetEventAvailabilityAsync_ReservedSeatingEvent_ReturnsSeatDetails()
        {
            // Arrange
            var eventId = 1;
            var venue = new Venue { Id = 1, Name = "Theatre", Address = "Downtown" };
            var venueSection = new VenueSection { Id = 1, Name = "Orchestra" };
            
            var venueSeat1 = new VenueSeat { Id = 1, Row = "A", SeatNumber = "1", SeatLabel = "A1", Section = venueSection };
            var venueSeat2 = new VenueSeat { Id = 2, Row = "A", SeatNumber = "2", SeatLabel = "A2", Section = venueSection };
            var venueSeat3 = new VenueSeat { Id = 3, Row = "B", SeatNumber = "1", SeatLabel = "B1", Section = venueSection };

            var reservedEvent = new ReservedSeatingEvent
            {
                Id = eventId,
                VenueId = 1,
                Name = "Theatre Show",
                StartsAt = DateTime.UtcNow.AddDays(21),
                Venue = venue,
                Seats = new List<EventSeat>
                {
                    new EventSeat { VenueSeatId = 1, Status = SeatStatus.Available, VenueSeat = venueSeat1 },
                    new EventSeat { VenueSeatId = 2, Status = SeatStatus.Reserved, VenueSeat = venueSeat2 },
                    new EventSeat { VenueSeatId = 3, Status = SeatStatus.Available, VenueSeat = venueSeat3 }
                }
            };

            _mockEventRepository
                .Setup(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservedEvent);

            // Act
            var result = await _service.GetEventAvailabilityAsync(eventId);

            // Assert
            result.Should().NotBeNull();
            result!.EventType.Should().Be("ReservedSeating");
            result.Seats.Should().HaveCount(3);
            result.TotalCapacity.Should().Be(3);
            result.AvailableCapacity.Should().Be(2);
            result.ReservedCount.Should().Be(1);
            result.IsAvailable.Should().BeTrue();
            
            var availableSeats = result.Seats.Where(s => s.IsAvailable).ToList();
            availableSeats.Should().HaveCount(2);
            availableSeats.Should().Contain(s => s.SeatLabel == "A1");
            availableSeats.Should().Contain(s => s.SeatLabel == "B1");
        }

        [TestMethod]
        public async Task GetEventAvailabilityAsync_InvalidEventId_ThrowsArgumentException()
        {
            // Arrange
            var eventId = 0;

            // Act
            Func<Task> act = async () => await _service.GetEventAvailabilityAsync(eventId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Event ID must be greater than zero*");
        }

        [TestMethod]
        public async Task GetEventAvailabilityAsync_EventNotFound_ReturnsNull()
        {
            // Arrange
            var eventId = 999;
            _mockEventRepository
                .Setup(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EventBase?)null);

            // Act
            var result = await _service.GetEventAvailabilityAsync(eventId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Multiple Event Types Tests

        [TestMethod]
        public async Task GetFutureEventsWithAvailabilityAsync_MixedEventTypes_ReturnsAllWithCorrectDetails()
        {
            // Arrange
            var venue = new Venue { Id = 1, Name = "Multi-Purpose Venue", Address = "Central Location" };
            
            var gaEvent = new GeneralAdmissionEvent
            {
                Id = 1,
                VenueId = 1,
                Name = "GA Concert",
                StartsAt = DateTime.UtcNow.AddDays(10),
                Capacity = 200,
                Price = 40m,
                Venue = venue
            };

            var sbEvent = new SectionBasedEvent
            {
                Id = 2,
                VenueId = 1,
                Name = "Sport Event",
                StartsAt = DateTime.UtcNow.AddDays(15),
                Venue = venue,
                SectionInventories = new List<EventSectionInventory>
                {
                    new EventSectionInventory
                    {
                        VenueSectionId = 1,
                        Capacity = 300,
                        Price = 75m,
                        VenueSection = new VenueSection { Id = 1, Name = "Main" }
                    }
                }
            };

            _mockEventRepository
                .Setup(x => x.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EventBase> { gaEvent, sbEvent });

            // Act
            var result = await _service.GetFutureEventsWithAvailabilityAsync();

            // Assert
            result.Should().HaveCount(2);
            
            var gaDto = result.First(e => e.EventType == "GeneralAdmission");
            gaDto.TotalCapacity.Should().Be(200);
            gaDto.Sections.Should().BeEmpty();
            gaDto.Seats.Should().BeEmpty();

            var sbDto = result.First(e => e.EventType == "SectionBased");
            sbDto.Sections.Should().HaveCount(1);
            sbDto.Seats.Should().BeEmpty();
        }

        #endregion
    }
}
