using System;

namespace EventBookingSystem.Domain.Entities
{
    /// <summary>
    /// Represents the status of a specific seat for a specific event.
    /// Used for Reserved EventType.
    /// </summary>
    public class EventSeat
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int VenueSeatId { get; set; }
        public SeatStatus Status { get; set; } = SeatStatus.Available;

        // Navigation properties
        public EventBase? Event { get; set; }
        public VenueSeat? VenueSeat { get; set; }
        
        /// <summary>
        /// Checks if this seat can be reserved.
        /// </summary>
        public bool IsAvailable() => Status == SeatStatus.Available;
        
        /// <summary>
        /// Marks the seat as reserved.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the seat is not available for reservation.</exception>
        public void Reserve()
        {
            if (!IsAvailable())
            {
                throw new InvalidOperationException($"Seat with VenueSeatId {VenueSeatId} is not available. Current status: {Status}");
            }
            Status = SeatStatus.Reserved;
        }
        
        /// <summary>
        /// Locks the seat temporarily (e.g., during checkout process).
        /// </summary>
        public void Lock()
        {
            if (Status != SeatStatus.Available)
            {
                throw new InvalidOperationException($"Cannot lock seat with VenueSeatId {VenueSeatId}. Current status: {Status}");
            }
            Status = SeatStatus.Locked;
        }
        
        /// <summary>
        /// Releases a locked seat back to available status.
        /// </summary>
        public void Release()
        {
            if (Status != SeatStatus.Locked)
            {
                throw new InvalidOperationException($"Cannot release seat with VenueSeatId {VenueSeatId}. Only locked seats can be released. Current status: {Status}");
            }
            Status = SeatStatus.Available;
        }
    }
}
