namespace EventBookingSystem.Domain.Entities
{
    /// <summary>
    /// Represents a physical section within a venue (e.g., Orchestra, Balcony, VIP).
    /// </summary>
    public class VenueSection
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public required string Name { get; set; }
        
        // Navigation properties
        public Venue? Venue { get; set; }
        public List<VenueSeat> VenueSeats { get; set; } = new();
        
        /// <summary>
        /// Gets the actual capacity based on the number of seats in this section.
        /// </summary>
        public int Capacity => VenueSeats?.Count ?? 0;
    }
}
