using EventBookingSystem.Domain.Entities;
using System;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Service for handling event reservations using the strategy pattern.
    /// Automatically selects and applies the appropriate reservation strategy based on event type.
    /// </summary>
    public class EventReservationService
    {
        private readonly GeneralAdmissionReservationStrategy _generalAdmissionStrategy;
        private readonly SectionBasedReservationStrategy _sectionBasedStrategy;
        private readonly ReservedSeatingReservationStrategy _reservedSeatingStrategy;

        public EventReservationService()
        {
            _generalAdmissionStrategy = new GeneralAdmissionReservationStrategy();
            _sectionBasedStrategy = new SectionBasedReservationStrategy();
            _reservedSeatingStrategy = new ReservedSeatingReservationStrategy();
        }

        /// <summary>
        /// Reserves tickets for a general admission event.
        /// </summary>
        /// <param name="venue">The venue hosting the event.</param>
        /// <param name="evnt">The general admission event.</param>
        /// <param name="quantity">The number of tickets to reserve.</param>
        public void ReserveTickets(Venue venue, GeneralAdmissionEvent evnt, int quantity)
        {
            var request = new ReservationRequest { Quantity = quantity };
            _generalAdmissionStrategy.Reserve(venue, evnt, request);
        }

        /// <summary>
        /// Reserves tickets in a specific section for a section-based event.
        /// </summary>
        /// <param name="venue">The venue hosting the event.</param>
        /// <param name="evnt">The section-based event.</param>
        /// <param name="sectionId">The section to reserve in.</param>
        /// <param name="quantity">The number of tickets to reserve.</param>
        public void ReserveInSection(Venue venue, SectionBasedEvent evnt, int sectionId, int quantity)
        {
            var request = new ReservationRequest 
            { 
                SectionId = sectionId, 
                Quantity = quantity 
            };
            _sectionBasedStrategy.Reserve(venue, evnt, request);
        }

        /// <summary>
        /// Reserves a specific seat for a reserved seating event.
        /// </summary>
        /// <param name="venue">The venue hosting the event.</param>
        /// <param name="evnt">The reserved seating event.</param>
        /// <param name="seatId">The seat to reserve.</param>
        public void ReserveSeat(Venue venue, ReservedSeatingEvent evnt, int seatId)
        {
            var request = new ReservationRequest { SeatId = seatId };
            _reservedSeatingStrategy.Reserve(venue, evnt, request);
        }

        /// <summary>
        /// Validates if a reservation can be made for a general admission event.
        /// </summary>
        public ValidationResult ValidateGeneralAdmission(Venue venue, GeneralAdmissionEvent evnt, int quantity)
        {
            var request = new ReservationRequest { Quantity = quantity };
            return _generalAdmissionStrategy.ValidateReservation(venue, evnt, request);
        }

        /// <summary>
        /// Validates if a reservation can be made in a specific section.
        /// </summary>
        public ValidationResult ValidateSectionBased(Venue venue, SectionBasedEvent evnt, int sectionId, int quantity)
        {
            var request = new ReservationRequest 
            { 
                SectionId = sectionId, 
                Quantity = quantity 
            };
            return _sectionBasedStrategy.ValidateReservation(venue, evnt, request);
        }

        /// <summary>
        /// Validates if a specific seat can be reserved.
        /// </summary>
        public ValidationResult ValidateReservedSeating(Venue venue, ReservedSeatingEvent evnt, int seatId)
        {
            var request = new ReservationRequest { SeatId = seatId };
            return _reservedSeatingStrategy.ValidateReservation(venue, evnt, request);
        }
    }
}
