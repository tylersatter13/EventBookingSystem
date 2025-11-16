using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Tests.Entities
{
    [TestClass]
    public class SectionBasedEventTests
    {
        [TestMethod]
        public void TotalCapacity_SumsAllSections()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Concert",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { Capacity = 500 },
                    new() { Capacity = 300 },
                    new() { Capacity = 200 }
                }
            };

            // Act
            var capacity = evnt.TotalCapacity;

            // Assert
            capacity.Should().Be(1000);
        }

        [TestMethod]
        public void TotalCapacity_WithOverride_ReturnsOverride()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event with Override",
                CapacityOverride = 800,
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { Capacity = 500 },
                    new() { Capacity = 300 }
                }
            };

            // Act
            var capacity = evnt.TotalCapacity;

            // Assert
            capacity.Should().Be(800, because: "override should take precedence");
        }

        [TestMethod]
        public void TotalReserved_SumsBookedAcrossAllSections()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Concert",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 500 },
                    new() { VenueSectionId = 2, Capacity = 300 },
                    new() { VenueSectionId = 3, Capacity = 200 }
                }
            };
            evnt.ReserveInSection(1, 200);
            evnt.ReserveInSection(2, 150);
            evnt.ReserveInSection(3, 100);

            // Act
            var reserved = evnt.TotalReserved;

            // Assert
            reserved.Should().Be(450);
        }

        [TestMethod]
        public void AvailableCapacity_CalculatesCorrectly()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 500 },
                    new() { VenueSectionId = 2, Capacity = 300 }
                }
            };
            evnt.ReserveInSection(1, 200);

            // Act
            var available = evnt.AvailableCapacity;

            // Assert
            available.Should().Be(600);  // 800 total - 200 reserved
        }

        [TestMethod]
        public void IsSoldOut_WithAvailableCapacity_ReturnsFalse()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100 }
                }
            };
            evnt.ReserveInSection(1, 50);

            // Act
            var isSoldOut = evnt.IsSoldOut;

            // Assert
            isSoldOut.Should().BeFalse();
        }

        [TestMethod]
        public void IsSoldOut_AtCapacity_ReturnsTrue()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Sold Out Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100 },
                    new() { VenueSectionId = 2, Capacity = 50 }
                }
            };
            evnt.ReserveInSection(1, 100);
            evnt.ReserveInSection(2, 50);

            // Act
            var isSoldOut = evnt.IsSoldOut;

            // Assert
            isSoldOut.Should().BeTrue();
        }

        [TestMethod]
        public void GetSection_WithValidId_ReturnsSection()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100, Price = 75m },
                    new() { VenueSectionId = 2, Capacity = 50, Price = 50m }
                }
            };

            // Act
            var section = evnt.GetSection(2);

            // Assert
            section.Should().NotBeNull();
            section!.VenueSectionId.Should().Be(2);
            section.Capacity.Should().Be(50);
            section.Price.Should().Be(50m);
        }

        [TestMethod]
        public void GetSection_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100 }
                }
            };

            // Act
            var section = evnt.GetSection(99);

            // Assert
            section.Should().BeNull();
        }

        [TestMethod]
        public void ReserveInSection_ValidSection_ReservesCorrectly()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Concert",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 500, Price = 100m },
                    new() { VenueSectionId = 2, Capacity = 300, Price = 60m }
                }
            };

            // Act
            evnt.ReserveInSection(1, 50);

            // Assert
            var section1 = evnt.GetSection(1);
            section1!.Booked.Should().Be(50);
            section1.Remaining.Should().Be(450);

            var section2 = evnt.GetSection(2);
            section2!.Booked.Should().Be(0);
        }

        [TestMethod]
        public void ReserveInSection_InvalidSection_ThrowsException()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100 }
                }
            };

            // Act
            Action act = () => evnt.ReserveInSection(99, 10);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Section with ID 99 not found*");
        }

        [TestMethod]
        public void ReserveInSection_ExceedingSectionCapacity_ThrowsException()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() 
                    { 
                        VenueSectionId = 1, 
                        Capacity = 50,
                        VenueSection = new VenueSection { Name = "VIP" }
                    }
                }
            };

            // Act
            Action act = () => evnt.ReserveInSection(1, 60);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Insufficient capacity*");
        }

        [TestMethod]
        public void ReleaseFromSection_ValidSection_ReleasesCorrectly()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 500 }
                }
            };
            evnt.ReserveInSection(1, 200);

            // Act
            evnt.ReleaseFromSection(1, 50);

            // Assert
            var section = evnt.GetSection(1);
            section!.Booked.Should().Be(150);
            section.Remaining.Should().Be(350);
        }

        [TestMethod]
        public void ReleaseFromSection_InvalidSection_ThrowsException()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100 }
                }
            };

            // Act
            Action act = () => evnt.ReleaseFromSection(99, 10);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Section with ID 99 not found*");
        }

        [TestMethod]
        public void GetAvailableSections_ReturnsOnlyNonSoldOutSections()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100, VenueSection = new VenueSection { Name = "Floor" } },
                    new() { VenueSectionId = 2, Capacity = 50, VenueSection = new VenueSection { Name = "Balcony" } },
                    new() { VenueSectionId = 3, Capacity = 30, VenueSection = new VenueSection { Name = "VIP" } }
                }
            };
            evnt.ReserveInSection(2, 50);  // Sell out Balcony

            // Act
            var availableSections = evnt.GetAvailableSections().ToList();

            // Assert
            availableSections.Should().HaveCount(2);
            availableSections.Should().Contain(s => s.VenueSectionId == 1);
            availableSections.Should().Contain(s => s.VenueSectionId == 3);
            availableSections.Should().NotContain(s => s.VenueSectionId == 2);
        }

        [TestMethod]
        public void GetSoldOutSections_ReturnsOnlySoldOutSections()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100 },
                    new() { VenueSectionId = 2, Capacity = 50 },
                    new() { VenueSectionId = 3, Capacity = 30 }
                }
            };
            evnt.ReserveInSection(2, 50);  // Sell out section 2
            evnt.ReserveInSection(3, 30);  // Sell out section 3

            // Act
            var soldOutSections = evnt.GetSoldOutSections().ToList();

            // Assert
            soldOutSections.Should().HaveCount(2);
            soldOutSections.Should().Contain(s => s.VenueSectionId == 2);
            soldOutSections.Should().Contain(s => s.VenueSectionId == 3);
        }

        [TestMethod]
        public void ValidateSectionReservation_ValidSection_ReturnsSuccess()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100 }
                }
            };

            // Act
            var result = evnt.ValidateSectionReservation(1, 50);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ValidateSectionReservation_InvalidSection_ReturnsFailure()
        {
            // Arrange
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100 }
                }
            };

            // Act
            var result = evnt.ValidateSectionReservation(99, 10);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Section with ID 99 not found");
        }
    }
}
