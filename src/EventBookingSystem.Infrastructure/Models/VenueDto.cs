namespace EventBookingSystem.Infrastructure.Models
{
    /// <summary>
    /// Data Transfer Object for Venue entity.
    /// </summary>
    public class VenueDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the venue.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the venue.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the address of the venue.
        /// </summary>
        public string Address { get; set; } = string.Empty;
    }
}
