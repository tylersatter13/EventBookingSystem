using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Mapping;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Tests.Mapping;

/// <summary>
/// Tests for GeneralAdmissionEventMapper.
/// </summary>
[TestClass]
public class GeneralAdmissionEventMapperTests
{
    #region ToDomain Tests

    [TestMethod]
    public void ToDomain_WithValidDto_MapsAllProperties()
    {
        // Arrange
        var dto = new GeneralAdmissionEventDto
        {
            Id = 1,
            VenueId = 10,
            Name = "Summer Festival",
            StartsAt = new DateTime(2024, 7, 15, 18, 0, 0),
            EndsAt = new DateTime(2024, 7, 15, 23, 0, 0),
            EstimatedAttendance = 500,
            Capacity = 1000,
            Attendees = 250,
            Price = 50.00m
        };

        // Act
        var gaEvent = GeneralAdmissionEventMapper.ToDomain(dto);

        // Assert
        gaEvent.Should().NotBeNull();
        gaEvent.Id.Should().Be(1);
        gaEvent.VenueId.Should().Be(10);
        gaEvent.Name.Should().Be("Summer Festival");
        gaEvent.StartsAt.Should().Be(new DateTime(2024, 7, 15, 18, 0, 0));
        gaEvent.EndsAt.Should().Be(new DateTime(2024, 7, 15, 23, 0, 0));
        gaEvent.EstimatedAttendance.Should().Be(500);
        gaEvent.Capacity.Should().Be(1000);
        gaEvent.Attendees.Should().Be(250);
        gaEvent.Price.Should().Be(50.00m);
    }

    [TestMethod]
    public void ToDomain_RestoresPrivateAttendeesField()
    {
        // Arrange
        var dto = new GeneralAdmissionEventDto
        {
            Name = "Concert",
            Capacity = 500,
            Attendees = 123
        };

        // Act
        var gaEvent = GeneralAdmissionEventMapper.ToDomain(dto);

        // Assert
        gaEvent.Attendees.Should().Be(123, because: "private _attendees field should be restored");
        gaEvent.TotalReserved.Should().Be(123);
        gaEvent.AvailableCapacity.Should().Be(377); // 500 - 123
    }

    [TestMethod]
    public void ToDomain_WithNullPrice_MapsCorrectly()
    {
        // Arrange
        var dto = new GeneralAdmissionEventDto
        {
            Name = "Free Event",
            Capacity = 100,
            Price = null
        };

        // Act
        var gaEvent = GeneralAdmissionEventMapper.ToDomain(dto);

        // Assert
        gaEvent.Price.Should().BeNull();
    }

    #endregion

    #region ToDto Tests

    [TestMethod]
    public void ToDto_WithValidDomainEntity_MapsAllProperties()
    {
        // Arrange
        var gaEvent = new GeneralAdmissionEvent
        {
            Id = 2,
            VenueId = 20,
            Name = "Rock Concert",
            StartsAt = new DateTime(2024, 8, 20, 19, 0, 0),
            EndsAt = new DateTime(2024, 8, 20, 22, 0, 0),
            EstimatedAttendance = 300,
            Capacity = 600,
            Price = 75.50m
        };
        
        // Reserve some tickets
        gaEvent.ReserveTickets(150);

        // Act
        var dto = GeneralAdmissionEventMapper.ToDto(gaEvent);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(2);
        dto.VenueId.Should().Be(20);
        dto.Name.Should().Be("Rock Concert");
        dto.StartsAt.Should().Be(new DateTime(2024, 8, 20, 19, 0, 0));
        dto.EndsAt.Should().Be(new DateTime(2024, 8, 20, 22, 0, 0));
        dto.EstimatedAttendance.Should().Be(300);
        dto.Capacity.Should().Be(600);
        dto.Attendees.Should().Be(150);
        dto.Price.Should().Be(75.50m);
    }

    [TestMethod]
    public void ToDto_PreservesAttendeesCount()
    {
        // Arrange
        var gaEvent = new GeneralAdmissionEvent
        {
            Name = "Event",
            Capacity = 1000
        };
        
        gaEvent.ReserveTickets(250);
        gaEvent.ReserveTickets(100);

        // Act
        var dto = GeneralAdmissionEventMapper.ToDto(gaEvent);

        // Assert
        dto.Attendees.Should().Be(350, because: "attendees should be preserved from domain");
        dto.AvailableCapacity.Should().Be(650); // 1000 - 350
    }

    #endregion

    #region ToEventDto Tests

    [TestMethod]
    public void ToEventDto_ConvertsToTablePerHierarchyFormat()
    {
        // Arrange
        var gaDto = new GeneralAdmissionEventDto
        {
            Id = 3,
            VenueId = 30,
            Name = "Festival",
            StartsAt = new DateTime(2024, 9, 1, 12, 0, 0),
            Capacity = 2000,
            Attendees = 500,
            Price = 35.00m
        };

        // Act
        var eventDto = GeneralAdmissionEventMapper.ToEventDto(gaDto);

        // Assert
        eventDto.Should().NotBeNull();
        eventDto.Id.Should().Be(3);
        eventDto.VenueId.Should().Be(30);
        eventDto.Name.Should().Be("Festival");
        eventDto.EventType.Should().Be("GeneralAdmission");
        eventDto.GA_Capacity.Should().Be(2000);
        eventDto.GA_Attendees.Should().Be(500);
        eventDto.GA_Price.Should().Be(35.00m);
    }

    [TestMethod]
    public void ToEventDto_SetsEventTypeDiscriminator()
    {
        // Arrange
        var gaDto = new GeneralAdmissionEventDto
        {
            Name = "Test Event",
            Capacity = 100
        };

        // Act
        var eventDto = GeneralAdmissionEventMapper.ToEventDto(gaDto);

        // Assert
        eventDto.EventType.Should().Be("GeneralAdmission");
    }

    #endregion

    #region FromEventDto Tests

    [TestMethod]
    public void FromEventDto_ConvertsFromTablePerHierarchyFormat()
    {
        // Arrange
        var eventDto = new EventDto
        {
            Id = 4,
            VenueId = 40,
            Name = "Club Night",
            StartsAt = new DateTime(2024, 10, 15, 21, 0, 0),
            EventType = "GeneralAdmission",
            GA_Capacity = 300,
            GA_Attendees = 150,
            GA_Price = 25.00m
        };

        // Act
        var gaDto = GeneralAdmissionEventMapper.FromEventDto(eventDto);

        // Assert
        gaDto.Should().NotBeNull();
        gaDto.Id.Should().Be(4);
        gaDto.VenueId.Should().Be(40);
        gaDto.Name.Should().Be("Club Night");
        gaDto.Capacity.Should().Be(300);
        gaDto.Attendees.Should().Be(150);
        gaDto.Price.Should().Be(25.00m);
    }

    [TestMethod]
    public void FromEventDto_WithWrongEventType_ThrowsException()
    {
        // Arrange
        var eventDto = new EventDto
        {
            EventType = "SectionBased",
            Name = "Not GA Event"
        };

        // Act
        Action act = () => GeneralAdmissionEventMapper.FromEventDto(eventDto);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot convert EventDto of type 'SectionBased' to GeneralAdmissionEventDto*");
    }

    [TestMethod]
    public void FromEventDto_WithNullCapacity_UsesZero()
    {
        // Arrange
        var eventDto = new EventDto
        {
            EventType = "GeneralAdmission",
            Name = "Event",
            GA_Capacity = null,
            GA_Attendees = null
        };

        // Act
        var gaDto = GeneralAdmissionEventMapper.FromEventDto(eventDto);

        // Assert
        gaDto.Capacity.Should().Be(0);
        gaDto.Attendees.Should().Be(0);
    }

    #endregion

    #region Round-Trip Tests

    [TestMethod]
    public void RoundTrip_DomainToDtoToDomain_PreservesData()
    {
        // Arrange
        var original = new GeneralAdmissionEvent
        {
            Id = 5,
            VenueId = 50,
            Name = "Original Event",
            StartsAt = new DateTime(2024, 11, 1, 18, 0, 0),
            Capacity = 500,
            Price = 40.00m
        };
        original.ReserveTickets(200);

        // Act - Round trip
        var dto = GeneralAdmissionEventMapper.ToDto(original);
        var restored = GeneralAdmissionEventMapper.ToDomain(dto);

        // Assert
        restored.Id.Should().Be(original.Id);
        restored.VenueId.Should().Be(original.VenueId);
        restored.Name.Should().Be(original.Name);
        restored.StartsAt.Should().Be(original.StartsAt);
        restored.Capacity.Should().Be(original.Capacity);
        restored.Attendees.Should().Be(original.Attendees);
        restored.Price.Should().Be(original.Price);
    }

    [TestMethod]
    public void RoundTrip_ThroughEventDto_PreservesData()
    {
        // Arrange
        var original = new GeneralAdmissionEventDto
        {
            Id = 6,
            VenueId = 60,
            Name = "Concert Series",
            Capacity = 800,
            Attendees = 400,
            Price = 55.00m
        };

        // Act - Round trip through EventDto
        var eventDto = GeneralAdmissionEventMapper.ToEventDto(original);
        var restored = GeneralAdmissionEventMapper.FromEventDto(eventDto);

        // Assert
        restored.Id.Should().Be(original.Id);
        restored.VenueId.Should().Be(original.VenueId);
        restored.Name.Should().Be(original.Name);
        restored.Capacity.Should().Be(original.Capacity);
        restored.Attendees.Should().Be(original.Attendees);
        restored.Price.Should().Be(original.Price);
    }

    #endregion

    #region Calculated Properties Tests

    [TestMethod]
    public void Dto_CalculatedProperties_WorkCorrectly()
    {
        // Arrange
        var dto = new GeneralAdmissionEventDto
        {
            Capacity = 1000,
            Attendees = 300
        };

        // Assert
        dto.AvailableCapacity.Should().Be(700); // 1000 - 300
        dto.IsSoldOut.Should().BeFalse();
    }

    [TestMethod]
    public void Dto_WhenSoldOut_IsSoldOutIsTrue()
    {
        // Arrange
        var dto = new GeneralAdmissionEventDto
        {
            Capacity = 500,
            Attendees = 500
        };

        // Assert
        dto.IsSoldOut.Should().BeTrue();
        dto.AvailableCapacity.Should().Be(0);
    }

    #endregion
}
