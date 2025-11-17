namespace EventBookingSystem.Application.DTOs
{
    /// <summary>
    /// DTO for event information with availability details.
    /// Provides comprehensive information for displaying events to users.
    /// </summary>
    public class EventAvailabilityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string VenueAddress { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public int EstimatedAttendance { get; set; }
        
        // Availability information
        public int TotalCapacity { get; set; }
        public int AvailableCapacity { get; set; }
        public int ReservedCount { get; set; }
        public decimal? Price { get; set; }
        public bool IsAvailable { get; set; }
        public double AvailabilityPercentage { get; set; }
        
        // Section-based event details (if applicable)
        public List<SectionAvailabilityDto> Sections { get; set; } = new();
        
        // Reserved seating event details (if applicable)
        public List<SeatAvailabilityDto> Seats { get; set; } = new();
    }

    /// <summary>
    /// DTO for section availability in section-based events.
    /// </summary>
    public class SectionAvailabilityDto
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int Available { get; set; }
        public int Booked { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public double AvailabilityPercentage { get; set; }
    }

    /// <summary>
    /// DTO for seat availability in reserved seating events.
    /// </summary>
    public class SeatAvailabilityDto
    {
        public int VenueSeatId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public string Row { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public string SeatLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
}
