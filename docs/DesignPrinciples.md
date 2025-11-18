# Design Principles in the Event Booking System

## Table of Contents
1. [Overview](#overview)
2. [Architectural Patterns](#architectural-patterns)
3. [SOLID Principles](#solid-principles)
4. [Domain-Driven Design (DDD)](#domain-driven-design-ddd)
5. [Design Patterns](#design-patterns)
6. [Additional Principles](#additional-principles)
7. [Code Quality Practices](#code-quality-practices)
8. [Testing Philosophy](#testing-philosophy)
9. [Examples and Case Studies](#examples-and-case-studies)
10. [Conclusion](#conclusion)

---

## Overview

The Event Booking System is a sophisticated application built with .NET 10 and C# 14.0 that exemplifies modern software engineering principles. The system manages event bookings, venue capacity, and reservations using Clean Architecture, SOLID principles, Domain-Driven Design, and established design patterns.

**Core Philosophy:**
> "Build systems that are easy to understand, extend, and maintain by following proven architectural principles and design patterns."

---

## Architectural Patterns

### Clean Architecture

Clean Architecture is the foundational principle organizing the codebase into distinct layers with well-defined dependencies.

#### Layer Structure

```
???????????????????????????????????????????
?         Presentation Layer              ?
?         (Future API/UI Layer)           ?
???????????????????????????????????????????
               ?
???????????????????????????????????????????
?      Application Layer                   ?
?  - Application Services                  ?
?  - DTOs & Commands                       ?
?  - Application Validators                ?
?  - Query Services                        ?
???????????????????????????????????????????
               ?
???????????????????????????????????????????
?         Domain Layer                     ?
?  - Entities & Value Objects              ?
?  - Domain Services                       ?
?  - Domain Interfaces                     ?
?  - Business Logic                        ?
???????????????????????????????????????????
               ?
???????????????????????????????????????????
?      Infrastructure Layer                ?
?  - Repository Implementations            ?
?  - Database Access (Dapper)              ?
?  - External Service Integrations         ?
????????????????????????????????????????????
```

#### The Dependency Rule

**Core Principle:** Dependencies always point inward. Inner layers have no knowledge of outer layers.

**Example:**
```csharp
// ? CORRECT: Domain defines interface
namespace EventBookingSystem.Domain.Services
{
    public interface IBookingValidator
    {
        ValidationResult Validate(User user, EventBase evnt, ReservationRequest request);
    }
}

// ? CORRECT: Infrastructure implements interface
namespace EventBookingSystem.Infrastructure.Interfaces
{
    public interface IEventRepository  // Domain interface
    {
        Task<EventBase> AddAsync(EventBase entity, CancellationToken cancellationToken = default);
    }
}

// ? WRONG: Domain depending on Infrastructure
namespace EventBookingSystem.Domain
{
    using EventBookingSystem.Infrastructure;  // NEVER DO THIS!
}
```

#### Benefits Demonstrated

1. **Testability:** Domain logic can be tested without databases or external dependencies
2. **Independence:** Business logic is decoupled from frameworks and UI
3. **Flexibility:** Easy to swap infrastructure components (e.g., switch from SQLite to PostgreSQL)
4. **Maintainability:** Changes in outer layers don't affect inner layers

**Real Example from the Project:**
```csharp
// Domain Layer - Pure business logic
public class BookingService : IBookingService
{
    // Depends only on domain abstractions
    private readonly EventReservationService _reservationService;
    private readonly IEnumerable<IBookingValidator> _bookingValidators;

    public Booking CreateBooking(User user, EventBase evnt, ReservationRequest request)
    {
        // Business logic with no infrastructure dependencies
        var validationResult = ValidateBooking(user, evnt, request);
        // ...
    }
}

// Application Layer - Orchestrates workflow
public class BookingApplicationService
{
    // Depends on both domain and infrastructure abstractions
    private readonly IBookingRepository _bookingRepository;  // Infrastructure
    private readonly IBookingService _bookingService;        // Domain
    private readonly IPaymentService _paymentService;        // Application

    public async Task<BookingResultDto> CreateBookingAsync(CreateBookingCommand command)
    {
        // 1. Validate command (application concern)
        // 2. Load entities (infrastructure concern)
        // 3. Execute domain logic (domain concern)
        // 4. Process payment (application concern)
        // 5. Persist changes (infrastructure concern)
        // 6. Map to DTO (application concern)
    }
}
```

---

## SOLID Principles

SOLID is deeply embedded throughout the project, guiding every design decision.

### Single Responsibility Principle (SRP)

**Definition:** A class should have one, and only one, reason to change.

#### Example 1: Separated Validators

Instead of one monolithic validator, responsibilities are split:

```csharp
// Each validator has ONE responsibility

/// <summary>
/// ONLY validates user booking limits
/// </summary>
public class UserBookingLimitValidator : IBookingValidator
{
    public ValidationResult Validate(User user, EventBase evnt, ReservationRequest request)
    {
        // Only checks: Has user exceeded booking limit?
        var existingBookings = user.Bookings?.Count ?? 0;
        // ...
    }
}

/// <summary>
/// ONLY validates event availability
/// </summary>
public class EventAvailabilityValidator : IBookingValidator
{
    public ValidationResult Validate(User user, EventBase evnt, ReservationRequest request)
    {
        // Only checks: Is event still accepting bookings?
        if (evnt.StartsAt <= DateTime.UtcNow) { /* ... */ }
        if (evnt.IsSoldOut) { /* ... */ }
        // ...
    }
}

/// <summary>
/// ONLY validates user information completeness
/// </summary>
public class UserInformationValidator : IBookingValidator
{
    public ValidationResult Validate(User user, EventBase evnt, ReservationRequest request)
    {
        // Only checks: Does user have required information?
        if (string.IsNullOrWhiteSpace(user.Email)) { /* ... */ }
        // ...
    }
}
```

**Benefits:**
- Easy to test each validator independently
- Easy to add new validation rules without modifying existing code
- Clear, focused classes with obvious purpose
- Changes to one validation rule don't affect others

#### Example 2: Separated Concerns in Application Service

```csharp
public class BookingApplicationService
{
    // Private methods each have ONE clear responsibility

    private async Task<ValidationResult> ValidateCommandAsync(CreateBookingCommand command)
    {
        // ONLY validates commands
    }

    private async Task<EntityLoadResult> LoadRequiredEntitiesAsync(CreateBookingCommand command)
    {
        // ONLY loads entities from repositories
    }

    private static ReservationRequest BuildReservationRequest(CreateBookingCommand command)
    {
        // ONLY maps command to domain request
    }

    private async Task<PaymentResult> ProcessPaymentAsync(Booking booking, EventBase evnt)
    {
        // ONLY processes payment
    }

    private async Task<PersistenceResult> PersistBookingAsync(Booking booking, EventBase evnt)
    {
        // ONLY persists data
    }

    private static BookingResultDto MapToResultDto(Booking booking)
    {
        // ONLY maps domain entity to DTO
    }
}
```

---

### Open/Closed Principle (OCP)

**Definition:** Software entities should be open for extension but closed for modification.

#### Example 1: Validator Extensibility

Adding new validation rules requires NO modification of existing code:

```csharp
// Existing service - NEVER needs to change
public class BookingService : IBookingService
{
    private readonly IEnumerable<IBookingValidator> _bookingValidators;

    public ValidationResult ValidateBooking(User user, EventBase evnt, ReservationRequest request)
    {
        // Iterates over ALL validators without knowing what they are
        foreach (var validator in _bookingValidators)
        {
            var result = validator.Validate(user, evnt, request);
            if (!result.IsValid) return result;
        }
        return ValidationResult.Success();
    }
}

// Adding a NEW validator - NO modification needed
public class CreditCardValidator : IBookingValidator  // New class!
{
    public ValidationResult Validate(User user, EventBase evnt, ReservationRequest request)
    {
        // New validation logic
        if (user.CreditCard == null || user.CreditCard.IsExpired)
            return ValidationResult.Failure("Valid credit card required.");
        return ValidationResult.Success();
    }
}

// Registration (composition root) - ONLY place that changes
services.AddTransient<IBookingValidator, UserBookingLimitValidator>();
services.AddTransient<IBookingValidator, EventAvailabilityValidator>();
services.AddTransient<IBookingValidator, CreditCardValidator>();  // Just add this line
```

#### Example 2: Strategy Pattern for Event Types

The system handles different event types without modifying existing strategies:

```csharp
// Base abstraction
public interface IReservationStrategy<TEvent> where TEvent : EventBase
{
    ValidationResult ValidateReservation(Venue venue, TEvent evnt, ReservationRequest request);
    void Reserve(Venue venue, TEvent evnt, ReservationRequest request);
}

// Existing strategies - CLOSED for modification
public class GeneralAdmissionReservationStrategy : IReservationStrategy<GeneralAdmissionEvent> { }
public class SectionBasedReservationStrategy : IReservationStrategy<SectionBasedEvent> { }
public class ReservedSeatingReservationStrategy : IReservationStrategy<ReservedSeatingEvent> { }

// Want to add VIP seating? EXTEND, don't modify!
public class VipSeatingReservationStrategy : IReservationStrategy<VipSeatingEvent>
{
    public ValidationResult ValidateReservation(Venue venue, VipSeatingEvent evnt, ReservationRequest request)
    {
        // VIP-specific validation (e.g., membership check)
    }
    
    public void Reserve(Venue venue, VipSeatingEvent evnt, ReservationRequest request)
    {
        // VIP-specific reservation logic
    }
}
```

---

### Liskov Substitution Principle (LSP)

**Definition:** Derived classes must be substitutable for their base classes without altering program correctness.

#### Example 1: Event Type Hierarchy

All event types can be used interchangeably through the `EventBase` abstraction:

```csharp
public abstract class EventBase
{
    // Contract: All events have capacity
    public abstract int TotalCapacity { get; }
    public abstract int TotalReserved { get; }
    public abstract bool IsSoldOut { get; }
    
    // Contract: All events can validate capacity
    public virtual ValidationResult ValidateCapacity(int quantity)
    {
        if (quantity <= 0)
            return ValidationResult.Failure("Quantity must be positive.");
        
        if (IsSoldOut)
            return ValidationResult.Failure($"Event '{Name}' is sold out.");
        
        if (AvailableCapacity < quantity)
            return ValidationResult.Failure($"Insufficient capacity.");
        
        return ValidationResult.Success();
    }
}

// All subtypes maintain the contract
public class GeneralAdmissionEvent : EventBase
{
    // ? Maintains contract - capacity is always valid
    public override int TotalCapacity => CapacityOverride ?? (Venue?.MaxCapacity ?? 0);
    public override int TotalReserved => Attendees;
    public override bool IsSoldOut => TotalReserved >= TotalCapacity;
}

public class ReservedSeatingEvent : EventBase
{
    // ? Maintains contract - capacity is always valid
    public override int TotalCapacity => Seats.Count;
    public override int TotalReserved => Seats.Count(s => s.Status == SeatStatus.Reserved);
    public override bool IsSoldOut => TotalReserved >= TotalCapacity;
}

// Usage: Any EventBase subtype works correctly
public class BookingService
{
    public Booking CreateBooking(User user, EventBase evnt, ReservationRequest request)
    {
        // Works correctly for ANY event type!
        var validation = evnt.ValidateCapacity(request.Quantity);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);
        // ...
    }
}
```

#### Example 2: Strategy Implementations

All strategies are substitutable:

```csharp
public class EventReservationService
{
    private readonly GeneralAdmissionReservationStrategy _generalAdmissionStrategy;
    private readonly SectionBasedReservationStrategy _sectionBasedStrategy;
    private readonly ReservedSeatingReservationStrategy _reservedSeatingStrategy;

    // ? All strategies have the same interface and maintain contracts
    public ValidationResult ValidateGeneralAdmission(Venue venue, GeneralAdmissionEvent evnt, int quantity)
    {
        var request = new ReservationRequest { Quantity = quantity };
        return _generalAdmissionStrategy.ValidateReservation(venue, evnt, request);
    }

    public ValidationResult ValidateReservedSeating(Venue venue, ReservedSeatingEvent evnt, int seatId)
    {
        var request = new ReservationRequest { SeatId = seatId };
        return _reservedSeatingStrategy.ValidateReservation(venue, evnt, request);
    }
    
    // Both return ValidationResult with same behavior contract
}
```

---

### Interface Segregation Principle (ISP)

**Definition:** Clients should not be forced to depend on interfaces they don't use.

#### Example 1: Focused Validator Interfaces

Instead of one large interface, multiple focused interfaces:

```csharp
// ? BAD: Large interface with many methods
public interface IBookingManager
{
    ValidationResult ValidateBooking(...);
    ValidationResult ValidateUser(...);
    ValidationResult ValidateEvent(...);
    ValidationResult ValidatePayment(...);
    Booking CreateBooking(...);
    bool CancelBooking(...);
    bool RefundBooking(...);
    void SendConfirmationEmail(...);
    void SendReminderEmail(...);
    // Clients must implement ALL of these!
}

// ? GOOD: Focused interfaces
public interface IBookingValidator
{
    ValidationResult Validate(User user, EventBase evnt, ReservationRequest request);
}

public interface IBookingService
{
    Booking CreateBooking(User user, EventBase evnt, ReservationRequest request);
    ValidationResult ValidateBooking(User user, EventBase evnt, ReservationRequest request);
}

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken);
}

// Each interface is small and focused!
```

#### Example 2: Repository Pattern Interfaces

```csharp
// Base repository - minimal core operations
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

// Specialized repository - adds only what IT needs
public interface IEventRepository : IRepository<EventBase>
{
    Task<EventBase?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventBase>> GetByVenueIdAsync(int venueId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventBase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

// Another specialized repository - different needs
public interface IBookingRepository : IRepository<Booking>
{
    Task<IEnumerable<Booking>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
}

// Clients only depend on what they need!
```

---

### Dependency Inversion Principle (DIP)

**Definition:** High-level modules should not depend on low-level modules. Both should depend on abstractions.

#### Example 1: Domain Depending on Abstractions

```csharp
// ? Domain Layer: Defines the abstraction (interface)
namespace EventBookingSystem.Domain.Services
{
    public interface IBookingService  // Abstraction
    {
        Booking CreateBooking(User user, EventBase evnt, ReservationRequest request);
    }
    
    public class BookingService : IBookingService  // High-level module
    {
        private readonly EventReservationService _reservationService;
        private readonly IEnumerable<IBookingValidator> _bookingValidators;  // Depends on abstraction
        
        // Implementation
    }
}

// ? Infrastructure Layer: Defines interface for data access
namespace EventBookingSystem.Infrastructure.Interfaces
{
    public interface IEventRepository  // Abstraction
    {
        Task<EventBase> AddAsync(EventBase entity, CancellationToken cancellationToken);
    }
}

// ? Infrastructure Layer: Implements the abstraction
namespace EventBookingSystem.Infrastructure.Repositories
{
    public class DapperEventRepository : IEventRepository  // Low-level module
    {
        // Dapper-specific implementation
    }
}

// ? Application Layer: Depends on both abstractions
namespace EventBookingSystem.Application.Services
{
    public class BookingApplicationService
    {
        private readonly IBookingRepository _bookingRepository;  // Abstraction
        private readonly IEventRepository _eventRepository;      // Abstraction
        private readonly IBookingService _bookingService;        // Abstraction
        private readonly IPaymentService _paymentService;        // Abstraction
        
        // All dependencies are abstractions!
    }
}

// ? Never see this pattern in the codebase:
// public class BookingService
// {
//     private readonly DapperEventRepository _repository;  // Concrete type - BAD!
// }
```

#### Example 2: Testing Benefits

Because of DIP, unit testing is straightforward:

```csharp
[TestClass]
public class BookingServiceTests
{
    [TestMethod]
    public void CreateBooking_ValidRequest_CreatesBooking()
    {
        // Arrange: Use test doubles, no database needed!
        var mockReservationService = new EventReservationService();
        var mockValidators = new List<IBookingValidator>();  // Empty or with test validators
        
        var bookingService = new BookingService(mockReservationService, mockValidators);
        
        var user = new User { Id = 1, Name = "John", Email = "john@test.com" };
        var evnt = new GeneralAdmissionEvent { Id = 1, Name = "Concert", /* ... */ };
        var request = new ReservationRequest { Quantity = 2 };
        
        // Act
        var booking = bookingService.CreateBooking(user, evnt, request);
        
        // Assert
        booking.Should().NotBeNull();
        booking.User.Should().Be(user);
        booking.Event.Should().Be(evnt);
    }
}
```

---

## Domain-Driven Design (DDD)

DDD principles guide the domain layer's structure and behavior.

### Ubiquitous Language

The code uses domain terminology consistently throughout:

**Domain Terms:**
- `Event`, `Venue`, `Booking`, `User`
- `GeneralAdmission`, `ReservedSeating`, `SectionBased`
- `Capacity`, `Reservation`, `Inventory`
- `Attendees`, `Seats`, `Sections`

**Example:**
```csharp
// Code reads like the business language
public void ReserveTickets(int quantity)
{
    if (IsSoldOut)
        throw new InvalidOperationException($"Event '{Name}' is sold out.");
    
    Attendees += quantity;
}
```

### Entities

Entities have identity and lifecycle:

```csharp
/// <summary>
/// Entity: Has identity (Id), mutable state, and lifecycle
/// </summary>
public class Booking
{
    public int Id { get; set; }  // Identity
    public User User { get; set; }
    public EventBase Event { get; set; }
    public BookingType BookingType { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; }  // Mutable state
    public DateTime CreatedAt { get; set; }
    
    public List<BookingItem> BookingItems { get; set; } = new();
}
```

### Value Objects

Value objects are immutable and compared by value:

```csharp
/// <summary>
/// Value Object: Immutable, compared by value, no identity
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; }
    public string ErrorMessage { get; }

    private ValidationResult(bool isValid, string errorMessage = "")
    {
        IsValid = isValid;
        ErrorMessage = errorMessage ?? "";
    }

    public static ValidationResult Success() => new ValidationResult(true);
    public static ValidationResult Failure(string errorMessage) => new ValidationResult(false, errorMessage);
    
    // Compared by VALUE, not identity
}

/// <summary>
/// Value Object for reservation details
/// </summary>
public class ReservationRequest
{
    public int Quantity { get; set; }
    public int CustomerId { get; set; }
    public int? SectionId { get; set; }
    public int? SeatId { get; set; }
}
```

### Aggregates and Aggregate Roots

Aggregates enforce consistency boundaries:

```csharp
/// <summary>
/// Aggregate Root: Event controls access to EventSeats and EventSectionInventories
/// </summary>
public abstract class EventBase  // Aggregate Root
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // Encapsulated logic - enforces invariants
    public abstract int TotalCapacity { get; }
    public abstract int TotalReserved { get; }
    
    // Only through aggregate root can you validate
    public virtual ValidationResult ValidateCapacity(int quantity)
    {
        if (IsSoldOut)
            return ValidationResult.Failure($"Event '{Name}' is sold out.");
        
        if (AvailableCapacity < quantity)
            return ValidationResult.Failure("Insufficient capacity.");
        
        return ValidationResult.Success();
    }
}

/// <summary>
/// Child entity: EventSeat can only be accessed through ReservedSeatingEvent
/// </summary>
public class ReservedSeatingEvent : EventBase
{
    public List<EventSeat> Seats { get; set; } = new();  // Part of aggregate
    
    // Aggregate root controls modifications
    public void ReserveSeat(int seatId)
    {
        var seat = GetSeat(seatId);
        if (seat == null)
            throw new InvalidOperationException("Seat not found.");
        
        seat.Reserve();  // Controlled through aggregate
    }
    
    public EventSeat? GetSeat(int seatId)
    {
        return Seats.FirstOrDefault(s => s.VenueSeatId == seatId);
    }
}
```

**Benefits:**
- Consistency: All changes go through aggregate root
- Encapsulation: Business rules are enforced
- Clarity: Clear boundaries for transactions

### Domain Services

Domain services contain business logic that doesn't naturally fit in entities:

```csharp
/// <summary>
/// Domain Service: Coordinates reservation logic across multiple entities
/// </summary>
public class EventReservationService
{
    private readonly GeneralAdmissionReservationStrategy _generalAdmissionStrategy;
    private readonly SectionBasedReservationStrategy _sectionBasedStrategy;
    private readonly ReservedSeatingReservationStrategy _reservedSeatingStrategy;

    // Stateless service - no internal state
    public void ReserveTickets(Venue venue, GeneralAdmissionEvent evnt, int quantity)
    {
        var request = new ReservationRequest { Quantity = quantity };
        _generalAdmissionStrategy.Reserve(venue, evnt, request);
    }
    
    // Business logic that spans multiple entities
}

/// <summary>
/// Domain Service: Manages booking creation workflow
/// </summary>
public class BookingService : IBookingService
{
    // Orchestrates validators and reservation service
    // Contains logic that doesn't belong to a single entity
}
```

**When to Use Domain Services:**
- Logic spans multiple aggregates
- Operation doesn't naturally belong to any entity
- Stateless operations
- Complex algorithms or calculations

---

## Design Patterns

The project implements several classic design patterns.

### Strategy Pattern

**Purpose:** Define a family of algorithms, encapsulate each one, and make them interchangeable.

**Implementation:**
```csharp
// Strategy interface
public interface IReservationStrategy<TEvent> where TEvent : EventBase
{
    ValidationResult ValidateReservation(Venue venue, TEvent evnt, ReservationRequest request);
    void Reserve(Venue venue, TEvent evnt, ReservationRequest request);
}

// Concrete Strategy 1
public class GeneralAdmissionReservationStrategy : IReservationStrategy<GeneralAdmissionEvent>
{
    public ValidationResult ValidateReservation(Venue venue, GeneralAdmissionEvent evnt, ReservationRequest request)
    {
        return evnt.ValidateCapacity(request.Quantity);
    }
    
    public void Reserve(Venue venue, GeneralAdmissionEvent evnt, ReservationRequest request)
    {
        evnt.ReserveTickets(request.Quantity);
    }
}

// Concrete Strategy 2
public class ReservedSeatingReservationStrategy : IReservationStrategy<ReservedSeatingEvent>
{
    public ValidationResult ValidateReservation(Venue venue, ReservedSeatingEvent evnt, ReservationRequest request)
    {
        return evnt.ValidateSeatReservation(request.SeatId!.Value);
    }
    
    public void Reserve(Venue venue, ReservedSeatingEvent evnt, ReservationRequest request)
    {
        evnt.ReserveSeat(request.SeatId!.Value);
    }
}

// Context - uses strategies
public class EventReservationService
{
    private readonly GeneralAdmissionReservationStrategy _generalAdmissionStrategy;
    private readonly ReservedSeatingReservationStrategy _reservedSeatingStrategy;
    
    // Selects appropriate strategy based on event type
    public void ReserveTickets(Venue venue, GeneralAdmissionEvent evnt, int quantity)
    {
        _generalAdmissionStrategy.Reserve(venue, evnt, new ReservationRequest { Quantity = quantity });
    }
}
```

**Benefits:**
- New event types can be added without modifying existing strategies
- Each strategy is independently testable
- Clear separation of algorithms
- Runtime algorithm selection

---

### Repository Pattern

**Purpose:** Mediate between the domain and data mapping layers using a collection-like interface.

**Implementation:**
```csharp
// Generic repository interface
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

// Specialized repository
public interface IEventRepository : IRepository<EventBase>
{
    Task<EventBase?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventBase>> GetByVenueIdAsync(int venueId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventBase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

// Concrete implementation with Dapper
public class DapperEventRepository : IEventRepository
{
    private readonly IDBConnectionFactory _connectionFactory;

    public async Task<EventBase> AddAsync(EventBase entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var dto = EventMapper.ToDto(entity);
            var sql = BuildInsertSql(dto.EventType);
            dto.Id = await connection.ExecuteScalarAsync<int>(sql, dto, transaction);
            entity.Id = dto.Id;

            await SaveRelatedDataAsync(connection, transaction, entity);
            transaction.Commit();
            return entity;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
    
    // Other implementations...
}
```

**Benefits:**
- Domain layer doesn't know about database
- Easy to swap data access technology
- Testable with in-memory implementations
- Consistent interface for data operations

---

### Composite Pattern

**Purpose:** Compose objects into tree structures and treat individual objects and compositions uniformly.

**Implementation:**
```csharp
// Component interface
public interface IBookingValidator
{
    ValidationResult Validate(User user, EventBase evnt, ReservationRequest request);
}

// Leaf validators
public class UserBookingLimitValidator : IBookingValidator { }
public class EventAvailabilityValidator : IBookingValidator { }
public class UserInformationValidator : IBookingValidator { }

// Composite - treats collection of validators uniformly
public class BookingService : IBookingService
{
    private readonly IEnumerable<IBookingValidator> _bookingValidators;

    public ValidationResult ValidateBooking(User user, EventBase evnt, ReservationRequest request)
    {
        // Treats single validator and collection of validators the same way
        foreach (var validator in _bookingValidators)
        {
            var result = validator.Validate(user, evnt, request);
            if (!result.IsValid)
                return result;
        }
        return ValidationResult.Success();
    }
}
```

**Benefits:**
- Add validators without changing validation logic
- Treat single and multiple validators uniformly
- Easy to create validator chains or trees

---

### Factory Pattern

**Purpose:** Create objects without specifying exact class.

**Implementation:**
```csharp
// Factory for creating test data
public static class TestDataBuilder
{
    /// <summary>
    /// Factory method for creating venue with capacity
    /// </summary>
    public static Venue CreateVenueWithCapacity(string name, string address, int capacity)
    {
        var venue = new Venue
        {
            Id = 1,
            Name = name,
            Address = address,
            VenueSections = new List<VenueSection>
            {
                new VenueSection
                {
                    Id = 1,
                    VenueId = 1,
                    Name = "General Section",
                    VenueSeats = CreateSeats(capacity)
                }
            }
        };
        
        return venue;
    }

    /// <summary>
    /// Factory method for creating event with section inventories
    /// </summary>
    public static SectionBasedEvent CreateEventWithSectionInventories(
        Venue venue, 
        string eventName, 
        DateTime startsAt)
    {
        var evnt = new SectionBasedEvent
        {
            Id = 1,
            VenueId = venue.Id,
            Venue = venue,
            Name = eventName,
            StartsAt = startsAt,
            SectionInventories = new List<EventSectionInventory>()
        };

        foreach (var section in venue.VenueSections)
        {
            evnt.SectionInventories.Add(new EventSectionInventory
            {
                EventId = evnt.Id,
                VenueSectionId = section.Id,
                Capacity = section.Capacity
            });
        }

        return evnt;
    }
}
```

**Benefits:**
- Consistent object creation
- Encapsulates complex creation logic
- Simplifies test setup

---

### Mapper Pattern

**Purpose:** Transform between different object representations (Domain ? DTO).

**Implementation:**
```csharp
/// <summary>
/// Mapper for Event entities and DTOs
/// </summary>
public static class EventMapper
{
    /// <summary>
    /// Maps from domain entity to DTO
    /// </summary>
    public static EventDto ToDto(EventBase eventBase)
    {
        var dto = new EventDto
        {
            Id = eventBase.Id,
            VenueId = eventBase.VenueId,
            Name = eventBase.Name,
            StartsAt = eventBase.StartsAt,
            EndsAt = eventBase.EndsAt,
            EstimatedAttendance = eventBase.EstimatedAttendance
        };

        // Type-specific mapping
        switch (eventBase)
        {
            case GeneralAdmissionEvent ga:
                dto.EventType = "GeneralAdmission";
                dto.GA_Capacity = ga.Capacity;
                dto.GA_Attendees = ga.Attendees;
                dto.GA_Price = ga.Price;
                break;
            
            case SectionBasedEvent sb:
                dto.EventType = "SectionBased";
                dto.SB_CapacityOverride = sb.CapacityOverride;
                break;
            
            case ReservedSeatingEvent:
                dto.EventType = "ReservedSeating";
                break;
        }

        return dto;
    }

    /// <summary>
    /// Maps from DTO to domain entity
    /// </summary>
    public static EventBase ToDomain(EventDto dto)
    {
        return dto.EventType switch
        {
            "GeneralAdmission" => new GeneralAdmissionEvent
            {
                Id = dto.Id,
                VenueId = dto.VenueId,
                Name = dto.Name,
                Capacity = dto.GA_Capacity ?? 0,
                Attendees = dto.GA_Attendees ?? 0,
                Price = dto.GA_Price
            },
            "SectionBased" => new SectionBasedEvent
            {
                Id = dto.Id,
                VenueId = dto.VenueId,
                Name = dto.Name,
                CapacityOverride = dto.SB_CapacityOverride
            },
            "ReservedSeating" => new ReservedSeatingEvent
            {
                Id = dto.Id,
                VenueId = dto.VenueId,
                Name = dto.Name
            },
            _ => throw new InvalidOperationException($"Unknown event type: {dto.EventType}")
        };
    }
}
```

**Benefits:**
- Separates domain models from database models
- Enables different representations for different contexts
- Centralizes conversion logic

---

## Additional Principles

### Tell, Don't Ask

**Principle:** Tell objects what to do, don't ask for their state and make decisions.

```csharp
// ? BAD: Asking for state and making decisions
if (eventSeat.Status == SeatStatus.Available)
{
    eventSeat.Status = SeatStatus.Reserved;
}

// ? GOOD: Telling object what to do
eventSeat.Reserve();

// Implementation encapsulates logic
public class EventSeat
{
    public void Reserve()
    {
        if (Status != SeatStatus.Available)
            throw new InvalidOperationException("Seat is not available.");
        
        Status = SeatStatus.Reserved;
    }
}
```

---

### Law of Demeter (Principle of Least Knowledge)

**Principle:** An object should only talk to its immediate friends.

```csharp
// ? BAD: Reaching through multiple objects
var seatPrice = booking.Event.Venue.VenueSections[0].VenueSeats[0].Price;

// ? GOOD: Using methods at appropriate level
public class EventSectionInventory
{
    public decimal? Price { get; set; }  // Price at appropriate level
}

// ? GOOD: Encapsulated query
public class SectionBasedEvent
{
    public EventSectionInventory? GetSection(int sectionId)
    {
        return SectionInventories.FirstOrDefault(s => s.VenueSectionId == sectionId);
    }
}

// Usage
var section = sectionBasedEvent.GetSection(sectionId);
var price = section?.Price;
```

---

### Calculated Properties over Stored State

**Principle:** Derive state from authoritative sources rather than maintaining redundant copies.

```csharp
// ? GOOD: Calculated from actual seats
public class VenueSection
{
    public List<VenueSeat> VenueSeats { get; set; } = new();
    
    // Calculated - always accurate
    public int Capacity => VenueSeats?.Count ?? 0;
}

// ? GOOD: Calculated from event type
public abstract class EventBase
{
    // Different calculation for each type
    public abstract int TotalCapacity { get; }
    public abstract int TotalReserved { get; }
    
    // Derived property
    public int AvailableCapacity => TotalCapacity - TotalReserved;
    public bool IsSoldOut => AvailableCapacity <= 0;
}

// ? BAD: Would be redundant and error-prone
// public class Venue
// {
//     public int MaxCapacity { get; set; }  // Can get out of sync!
// }
```

**Benefits:**
- Single source of truth
- No synchronization issues
- Always accurate
- Easier to maintain

---

## Code Quality Practices

### Comprehensive XML Documentation

Every public API is documented:

```csharp
/// <summary>
/// Domain service responsible for creating and managing bookings.
/// Orchestrates validation and booking creation following SOLID principles.
/// </summary>
public class BookingService : IBookingService
{
    /// <summary>
    /// Creates a new booking after validating all business rules.
    /// </summary>
    /// <param name="user">The user making the booking.</param>
    /// <param name="evnt">The event being booked.</param>
    /// <param name="reservationRequest">The reservation details.</param>
    /// <returns>The created booking.</returns>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    public Booking CreateBooking(User user, EventBase evnt, ReservationRequest request)
    {
        // Implementation
    }
}
```

---

### Guard Clauses

Return early to reduce nesting:

```csharp
// ? GOOD: Guard clauses
public ValidationResult ValidateCapacity(int quantity)
{
    if (quantity <= 0)
        return ValidationResult.Failure("Quantity must be positive.");
    
    if (IsSoldOut)
        return ValidationResult.Failure($"Event '{Name}' is sold out.");
    
    if (AvailableCapacity < quantity)
        return ValidationResult.Failure("Insufficient capacity.");
    
    return ValidationResult.Success();
}

// ? BAD: Nested conditions
public ValidationResult ValidateCapacity(int quantity)
{
    if (quantity > 0)
    {
        if (!IsSoldOut)
        {
            if (AvailableCapacity >= quantity)
            {
                return ValidationResult.Success();
            }
            else
            {
                return ValidationResult.Failure("Insufficient capacity.");
            }
        }
        else
        {
            return ValidationResult.Failure($"Event '{Name}' is sold out.");
        }
    }
    else
    {
        return ValidationResult.Failure("Quantity must be positive.");
    }
}
```

---

### Meaningful Names

Names reveal intent:

```csharp
// ? GOOD: Clear, descriptive names
public class UserBookingLimitValidator
public void ReserveInSection(int sectionId, int quantity)
public ValidationResult ValidateReservation(...)

// ? BAD: Unclear names
public class Validator2
public void DoStuff(int x, int y)
public bool Check(...)
```

---

### Consistent Error Handling

```csharp
// Validation returns result objects (not exceptions)
public ValidationResult ValidateCapacity(int quantity)
{
    if (IsSoldOut)
        return ValidationResult.Failure($"Event '{Name}' is sold out.");
    
    return ValidationResult.Success();
}

// Business rule violations throw exceptions
public void Reserve(Venue venue, ReservedSeatingEvent evnt, ReservationRequest request)
{
    var validation = ValidateReservation(venue, evnt, request);
    if (!validation.IsValid)
    {
        throw new InvalidOperationException(validation.ErrorMessage);
    }
    
    evnt.ReserveSeat(request.SeatId!.Value);
}
```

---

## Testing Philosophy

### Test Structure: AAA Pattern

All tests follow Arrange-Act-Assert:

```csharp
[TestMethod]
public void Validate_WhenAttendanceExceedsCapacity_ReturnsFailureResult()
{
    // Arrange
    var validator = new CapacityValidator();
    var venue = new Venue { MaxCapacity = 100 };
    var evnt = new GeneralAdmissionEvent { Capacity = 100, Attendees = 50 };

    // Act
    var result = validator.Validate(venue, evnt);

    // Assert
    result.IsValid.Should().BeFalse(because: "attendance exceeds venue capacity");
    result.ErrorMessage.Should().Contain("exceeds the venue's maximum capacity");
}
```

### FluentAssertions for Readability

Use FluentAssertions over MSTest Assert:

```csharp
// ? GOOD: FluentAssertions - readable and expressive
result.IsValid.Should().BeTrue();
collection.Should().HaveCount(5);
collection.Should().Contain(item);
actual.Should().BeGreaterThan(expected);

// ? BAD: MSTest Assert - less readable
Assert.IsTrue(result.IsValid);
Assert.AreEqual(5, collection.Count);
Assert.IsTrue(collection.Contains(item));
Assert.IsTrue(actual > expected);
```

### Test Naming Convention

Pattern: `MethodName_Scenario_ExpectedBehavior`

```csharp
[TestMethod]
public void Reserve_WhenSeatAvailable_ReservesSeat() { }

[TestMethod]
public void Reserve_WhenSeatAlreadyReserved_ThrowsException() { }

[TestMethod]
public void ValidateCapacity_WhenEventSoldOut_ReturnsFailure() { }
```

### Test Independence

Each test is independent:

```csharp
[TestClass]
public class BookingServiceTests
{
    [TestMethod]
    public void Test1()
    {
        // Creates its own data
        var venue = TestDataBuilder.CreateVenueWithCapacity("Venue1", "Address1", 100);
        // Test logic
    }

    [TestMethod]
    public void Test2()
    {
        // Creates its own data (not dependent on Test1)
        var venue = TestDataBuilder.CreateVenueWithCapacity("Venue2", "Address2", 200);
        // Test logic
    }
}
```

---

## Examples and Case Studies

### Case Study 1: Adding a New Validator

**Requirement:** Add validation to ensure users have a verified email before booking.

**Solution (following OCP):**

```csharp
// Step 1: Create new validator (NEW code)
public class EmailVerificationValidator : IBookingValidator
{
    public ValidationResult Validate(User user, EventBase evnt, ReservationRequest request)
    {
        if (!user.EmailVerified)
        {
            return ValidationResult.Failure("Email must be verified before booking.");
        }
        return ValidationResult.Success();
    }
}

// Step 2: Register in DI container (ONE line change)
services.AddTransient<IBookingValidator, EmailVerificationValidator>();

// Step 3: Write tests
[TestClass]
public class EmailVerificationValidatorTests
{
    [TestMethod]
    public void Validate_WhenEmailNotVerified_ReturnsFailure()
    {
        // Test implementation
    }
}

// NO changes needed to:
// - BookingService
// - Other validators
// - Application service
```

---

### Case Study 2: Adding a New Event Type

**Requirement:** Support VIP events with special access rules.

**Solution (following OCP and Strategy Pattern):**

```csharp
// Step 1: Create domain entity (NEW code)
public class VipEvent : EventBase
{
    public string MembershipLevel { get; set; }
    public override int TotalCapacity => /* logic */;
    public override int TotalReserved => /* logic */;
    public override bool IsSoldOut => /* logic */;
}

// Step 2: Create strategy (NEW code)
public class VipReservationStrategy : IReservationStrategy<VipEvent>
{
    public ValidationResult ValidateReservation(Venue venue, VipEvent evnt, ReservationRequest request)
    {
        // VIP-specific validation
        if (request.User.MembershipLevel != evnt.MembershipLevel)
            return ValidationResult.Failure("Insufficient membership level.");
        
        return ValidationResult.Success();
    }
    
    public void Reserve(Venue venue, VipEvent evnt, ReservationRequest request)
    {
        // VIP-specific reservation logic
    }
}

// Step 3: Update EventReservationService (SMALL change)
public class EventReservationService
{
    private readonly VipReservationStrategy _vipStrategy;  // Add field
    
    public void ReserveVipTickets(Venue venue, VipEvent evnt, int quantity)  // Add method
    {
        _vipStrategy.Reserve(venue, evnt, new ReservationRequest { Quantity = quantity });
    }
}

// Existing code continues to work unchanged!
```

---

## Conclusion

The Event Booking System demonstrates how applying proven design principles creates maintainable, testable, and extensible software.

### Key Takeaways

1. **Clean Architecture** separates concerns into layers with clear dependencies
2. **SOLID Principles** guide every design decision:
   - **SRP**: Each class has one responsibility
   - **OCP**: Extend behavior without modifying existing code
   - **LSP**: Subtypes are substitutable for base types
   - **ISP**: Focused, minimal interfaces
   - **DIP**: Depend on abstractions, not concretions

3. **Domain-Driven Design** creates a rich domain model:
   - Entities with identity and behavior
   - Value objects for immutable concepts
   - Aggregates enforcing consistency
   - Domain services for cross-entity logic

4. **Design Patterns** solve common problems:
   - Strategy Pattern for algorithm families
   - Repository Pattern for data access
   - Composite Pattern for validator chains
   - Factory Pattern for object creation

5. **Code Quality** practices ensure maintainability:
   - Comprehensive XML documentation
   - Meaningful names
   - Guard clauses
   - Calculated properties
   - Consistent error handling

6. **Testing** is baked into the design:
   - FluentAssertions for readable tests
   - AAA pattern for structure
   - Test independence
   - Clear naming conventions

### The Result

A codebase that is:
- ? Easy to understand
- ? Easy to test
- ? Easy to extend
- ? Easy to maintain
- ? Resistant to bugs
- ? Aligned with business domain
- ? Professional and well-documented

### Moving Forward

As the system evolves, these principles continue to guide development:
- New features extend existing abstractions
- Tests validate behavior at all layers
- Documentation keeps the codebase accessible
- Clean Architecture maintains separation of concerns

**The principles are not just rules to follow—they're the foundation for building quality software that lasts.**

---

**Document Version:** 1.0  
**Last Updated:** 2025-01-XX  
**Target Framework:** .NET 10, C# 14.0
