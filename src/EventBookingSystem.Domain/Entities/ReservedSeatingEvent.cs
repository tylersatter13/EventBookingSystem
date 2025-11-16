using System;
using System.Collections.Generic;
using System.Linq;

namespace EventBookingSystem.Domain.Entities
{
    /// <summary>
    /// Represents an event with specific seat assignments.
    /// Each seat is individually tracked and reserved.
    /// Examples: Theatre shows, sports games, assigned seating concerts.
    /// </summary>
    public class ReservedSeatingEvent : EventBase
    {
        public List<EventSeat> Seats { get; set; } = new();
        
        public override int TotalCapacity => Seats?.Count ?? 0;
        
        public override int TotalReserved => 
            Seats?.Count(s => s.Status == SeatStatus.Reserved) ?? 0;
        
        public override bool IsSoldOut => !GetAvailableSeats().Any();
        
        /// <summary>
        /// Gets all available seats.
        /// </summary>
        /// <returns>A list of available seats.</returns>
        public List<EventSeat> GetAvailableSeats()
        {
            return Seats?.Where(s => s.Status == SeatStatus.Available).ToList() ?? new();
        }
        
        /// <summary>
        /// Gets all reserved seats.
        /// </summary>
        /// <returns>A list of reserved seats.</returns>
        public List<EventSeat> GetReservedSeats()
        {
            return Seats?.Where(s => s.Status == SeatStatus.Reserved).ToList() ?? new();
        }
        
        /// <summary>
        /// Gets all locked seats (temporarily held during checkout).
        /// </summary>
        /// <returns>A list of locked seats.</returns>
        public List<EventSeat> GetLockedSeats()
        {
            return Seats?.Where(s => s.Status == SeatStatus.Locked).ToList() ?? new();
        }
        
        /// <summary>
        /// Gets a specific seat by venue seat ID.
        /// </summary>
        /// <param name="venueSeatId">The venue seat ID.</param>
        /// <returns>The EventSeat if found, null otherwise.</returns>
        public EventSeat? GetSeat(int venueSeatId)
        {
            return Seats?.FirstOrDefault(s => s.VenueSeatId == venueSeatId);
        }
        
        /// <summary>
        /// Validates if a specific seat can be reserved.
        /// </summary>
        /// <param name="venueSeatId">The venue seat ID to validate.</param>
        /// <returns>A ValidationResult indicating success or failure.</returns>
        public ValidationResult ValidateSeatReservation(int venueSeatId)
        {
            var seat = GetSeat(venueSeatId);
            if (seat == null)
                return ValidationResult.Failure($"Seat with ID {venueSeatId} not found.");
            
            if (!seat.IsAvailable())
                return ValidationResult.Failure(
                    $"Seat is not available. Current status: {seat.Status}");
            
            return ValidationResult.Success();
        }
        
        /// <summary>
        /// Reserves a specific seat.
        /// </summary>
        /// <param name="venueSeatId">The venue seat ID to reserve.</param>
        /// <exception cref="InvalidOperationException">Thrown when seat not found or not available.</exception>
        public void ReserveSeat(int venueSeatId)
        {
            var validation = ValidateSeatReservation(venueSeatId);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.ErrorMessage);
            
            var seat = GetSeat(venueSeatId)!;
            seat.Reserve();
        }
        
        /// <summary>
        /// Locks a seat temporarily (e.g., during checkout process).
        /// </summary>
        /// <param name="venueSeatId">The venue seat ID to lock.</param>
        /// <exception cref="InvalidOperationException">Thrown when seat not found or cannot be locked.</exception>
        public void LockSeat(int venueSeatId)
        {
            var seat = GetSeat(venueSeatId);
            if (seat == null)
                throw new InvalidOperationException($"Seat with ID {venueSeatId} not found.");
            
            seat.Lock();
        }
        
        /// <summary>
        /// Releases a locked seat back to available status.
        /// </summary>
        /// <param name="venueSeatId">The venue seat ID to release.</param>
        /// <exception cref="InvalidOperationException">Thrown when seat not found or cannot be released.</exception>
        public void ReleaseSeat(int venueSeatId)
        {
            var seat = GetSeat(venueSeatId);
            if (seat == null)
                throw new InvalidOperationException($"Seat with ID {venueSeatId} not found.");
            
            seat.Release();
        }
        
        /// <summary>
        /// Gets seats in a specific venue section.
        /// </summary>
        /// <param name="venueSectionId">The venue section ID.</param>
        /// <returns>A list of seats in the specified section.</returns>
        public List<EventSeat> GetSeatsInSection(int venueSectionId)
        {
            return Seats?
                .Where(s => s.VenueSeat?.VenueSectionId == venueSectionId)
                .ToList() ?? new();
        }
        
        /// <summary>
        /// Gets available seats in a specific section.
        /// </summary>
        /// <param name="venueSectionId">The venue section ID.</param>
        /// <returns>A list of available seats in the specified section.</returns>
        public List<EventSeat> GetAvailableSeatsInSection(int venueSectionId)
        {
            return GetSeatsInSection(venueSectionId)
                .Where(s => s.IsAvailable())
                .ToList();
        }
    }
}
