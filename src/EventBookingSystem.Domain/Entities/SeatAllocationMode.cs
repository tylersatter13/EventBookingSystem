namespace EventBookingSystem.Domain.Entities
{
    /// <summary>
    /// Defines how seats are allocated within a section for an event.
    /// </summary>
    public enum SeatAllocationMode
    {
        /// <summary>
        /// First-come, first-served. No specific seat assignments.
        /// </summary>
        GeneralAdmission,
        
        /// <summary>
        /// Specific seat assignments required (e.g., Row A, Seat 5).
        /// </summary>
        Reserved,
        
        /// <summary>
        /// System automatically picks the best available seats.
        /// </summary>
        BestAvailable
    }
}
