using EventBookingSystem.Application.DTOs;
using EventBookingSystem.Application.Interfaces;
using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Application.Services
{
    /// <summary>
    /// Service for querying event information with availability details.
    /// Orchestrates data retrieval from repositories and maps to DTOs.
    /// Follows SRP - single responsibility of reading event availability data.
    /// Follows DIP - depends on repository abstractions.
    /// </summary>
    public class EventQueryService : IEventQueryService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IVenueRepository _venueRepository;

        public EventQueryService(
            IEventRepository eventRepository,
            IVenueRepository venueRepository)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _venueRepository = venueRepository ?? throw new ArgumentNullException(nameof(venueRepository));
        }

        /// <summary>
        /// Retrieves all future events with available seat/capacity information.
        /// Events are filtered to only include those starting after the current date.
        /// </summary>
        public async Task<IEnumerable<EventAvailabilityDto>> GetFutureEventsWithAvailabilityAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var futureDate = now.AddYears(1); // Look ahead 1 year
            
            var events = await _eventRepository.GetByDateRangeAsync(now, futureDate, cancellationToken);
            
            var availabilityTasks = events.Select(async evnt => await MapToAvailabilityDto(evnt, cancellationToken));
            var availabilityDtos = await Task.WhenAll(availabilityTasks);
            
            return availabilityDtos
                .Where(dto => dto != null)
                .OrderBy(dto => dto.StartsAt)
                .ToList()!;
        }

        /// <summary>
        /// Retrieves future events at a specific venue with availability information.
        /// </summary>
        public async Task<IEnumerable<EventAvailabilityDto>> GetFutureEventsByVenueAsync(int venueId, CancellationToken cancellationToken = default)
        {
            if (venueId <= 0)
            {
                throw new ArgumentException("Venue ID must be greater than zero", nameof(venueId));
            }

            var events = await _eventRepository.GetByVenueIdAsync(venueId, cancellationToken);
            
            var now = DateTime.UtcNow;
            var futureEvents = events.Where(e => e.StartsAt > now);
            
            var availabilityTasks = futureEvents.Select(async evnt => await MapToAvailabilityDto(evnt, cancellationToken));
            var availabilityDtos = await Task.WhenAll(availabilityTasks);
            
            return availabilityDtos
                .Where(dto => dto != null)
                .OrderBy(dto => dto.StartsAt)
                .ToList()!;
        }

        /// <summary>
        /// Retrieves availability details for a specific event.
        /// </summary>
        public async Task<EventAvailabilityDto?> GetEventAvailabilityAsync(int eventId, CancellationToken cancellationToken = default)
        {
            if (eventId <= 0)
            {
                throw new ArgumentException("Event ID must be greater than zero", nameof(eventId));
            }

            var evnt = await _eventRepository.GetByIdWithDetailsAsync(eventId, cancellationToken);
            
            if (evnt == null)
            {
                return null;
            }

            return await MapToAvailabilityDto(evnt, cancellationToken);
        }

        /// <summary>
        /// Maps a domain Event entity to an EventAvailabilityDto.
        /// Handles all three event types: GeneralAdmission, SectionBased, and ReservedSeating.
        /// </summary>
        private async Task<EventAvailabilityDto> MapToAvailabilityDto(EventBase evnt, CancellationToken cancellationToken)
        {
            // Load venue if not already loaded
            string venueName = evnt.Venue?.Name ?? "Unknown Venue";
            string venueAddress = evnt.Venue?.Address ?? string.Empty;
            
            if (evnt.Venue == null)
            {
                var venue = await _venueRepository.GetByIdAsync(evnt.VenueId, cancellationToken);
                if (venue != null)
                {
                    venueName = venue.Name;
                    venueAddress = venue.Address;
                }
            }

            var dto = new EventAvailabilityDto
            {
                Id = evnt.Id,
                Name = evnt.Name,
                StartsAt = evnt.StartsAt,
                EndsAt = evnt.EndsAt,
                VenueId = evnt.VenueId,
                VenueName = venueName,
                VenueAddress = venueAddress,
                EstimatedAttendance = evnt.EstimatedAttendance
            };

            // Map type-specific details
            switch (evnt)
            {
                case GeneralAdmissionEvent ga:
                    MapGeneralAdmissionDetails(ga, dto);
                    break;

                case SectionBasedEvent sb:
                    MapSectionBasedDetails(sb, dto);
                    break;

                case ReservedSeatingEvent rs:
                    MapReservedSeatingDetails(rs, dto);
                    break;
            }

            return dto;
        }

        /// <summary>
        /// Maps General Admission event details to DTO.
        /// </summary>
        private static void MapGeneralAdmissionDetails(GeneralAdmissionEvent ga, EventAvailabilityDto dto)
        {
            dto.EventType = "GeneralAdmission";
            dto.TotalCapacity = ga.Capacity;
            dto.ReservedCount = ga.TotalReserved;
            dto.AvailableCapacity = ga.AvailableCapacity;
            dto.Price = ga.Price;
            dto.IsAvailable = ga.AvailableCapacity > 0;
            dto.AvailabilityPercentage = dto.TotalCapacity > 0 
                ? Math.Round((double)dto.AvailableCapacity / dto.TotalCapacity * 100, 2)
                : 0;
        }

        /// <summary>
        /// Maps Section-Based event details to DTO.
        /// </summary>
        private static void MapSectionBasedDetails(SectionBasedEvent sb, EventAvailabilityDto dto)
        {
            dto.EventType = "SectionBased";
            
            // Map section details
            dto.Sections = sb.SectionInventories.Select(section => new SectionAvailabilityDto
            {
                SectionId = section.VenueSectionId,
                SectionName = section.VenueSection?.Name ?? $"Section {section.VenueSectionId}",
                Capacity = section.Capacity,
                Booked = section.Booked,
                Available = section.Remaining,
                Price = section.Price ?? 0m,
                IsAvailable = section.Remaining > 0,
                AvailabilityPercentage = section.Capacity > 0
                    ? Math.Round((double)section.Remaining / section.Capacity * 100, 2)
                    : 0
            }).ToList();

            // Calculate totals
            dto.TotalCapacity = dto.Sections.Sum(s => s.Capacity);
            dto.ReservedCount = dto.Sections.Sum(s => s.Booked);
            dto.AvailableCapacity = dto.Sections.Sum(s => s.Available);
            dto.IsAvailable = dto.AvailableCapacity > 0;
            dto.AvailabilityPercentage = dto.TotalCapacity > 0
                ? Math.Round((double)dto.AvailableCapacity / dto.TotalCapacity * 100, 2)
                : 0;
            
            // Set price range if sections have different prices
            var prices = dto.Sections.Select(s => s.Price).Distinct().ToList();
            dto.Price = prices.Count == 1 ? prices.First() : prices.Min();
        }

        /// <summary>
        /// Maps Reserved Seating event details to DTO.
        /// </summary>
        private static void MapReservedSeatingDetails(ReservedSeatingEvent rs, EventAvailabilityDto dto)
        {
            dto.EventType = "ReservedSeating";
            
            // Map seat details
            dto.Seats = rs.Seats.Select(seat => new SeatAvailabilityDto
            {
                VenueSeatId = seat.VenueSeatId,
                SectionName = seat.VenueSeat?.Section?.Name ?? "Unknown Section",
                Row = seat.VenueSeat?.Row ?? "?",
                SeatNumber = seat.VenueSeat?.SeatNumber ?? "?",
                SeatLabel = seat.VenueSeat?.SeatLabel ?? $"{seat.VenueSeat?.Row}{seat.VenueSeat?.SeatNumber}",
                Status = seat.Status.ToString(),
                IsAvailable = seat.Status == SeatStatus.Available
            }).ToList();

            // Calculate totals
            dto.TotalCapacity = dto.Seats.Count;
            dto.AvailableCapacity = dto.Seats.Count(s => s.IsAvailable);
            dto.ReservedCount = dto.Seats.Count(s => s.Status == "Reserved");
            dto.IsAvailable = dto.AvailableCapacity > 0;
            dto.AvailabilityPercentage = dto.TotalCapacity > 0
                ? Math.Round((double)dto.AvailableCapacity / dto.TotalCapacity * 100, 2)
                : 0;
            
            // Reserved seating pricing would come from a pricing service
            dto.Price = null;
        }
    }
}
