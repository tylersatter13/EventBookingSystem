namespace EventBookingSystem.Infrastructure.Models
{
    /// <summary>
    /// Data Transfer Object for User entity.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user's name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's phone number (optional).
        /// </summary>
        public string? PhoneNumber { get; set; }
    }
}
