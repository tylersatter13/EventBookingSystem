# SOLID Principles Overview - Event Booking System

## Quick Reference Guide

This document provides a high-level overview of how SOLID principles are implemented throughout the Event Booking System. For detailed examples and case studies, see [DesignPrinciples.md](./DesignPrinciples.md).

---

## Table of Contents

1. [What is SOLID?](#what-is-solid)
2. [Single Responsibility Principle (SRP)](#single-responsibility-principle-srp)
3. [Open/Closed Principle (OCP)](#openclosed-principle-ocp)
4. [Liskov Substitution Principle (LSP)](#liskov-substitution-principle-lsp)
5. [Interface Segregation Principle (ISP)](#interface-segregation-principle-isp)
6. [Dependency Inversion Principle (DIP)](#dependency-inversion-principle-dip)
7. [Benefits in Our System](#benefits-in-our-system)
8. [Quick Decision Guide](#quick-decision-guide)

---

## What is SOLID?

SOLID is an acronym for five design principles that make software designs more understandable, flexible, and maintainable:

| Principle | Focus | Key Question |
|-----------|-------|--------------|
| **S**ingle Responsibility | One class, one job | Does this class have only one reason to change? |
| **O**pen/Closed | Extend, don't modify | Can I add features without changing existing code? |
| **L**iskov Substitution | Interchangeable types | Can I swap derived classes without breaking things? |
| **I**nterface Segregation | Focused interfaces | Are my interfaces small and focused? |
| **D**ependency Inversion | Depend on abstractions | Am I depending on interfaces, not concrete classes? |

---

## Single Responsibility Principle (SRP)

### Definition
> A class should have one, and only one, reason to change.

### In Our System

Each class has a single, well-defined responsibility:

#### ? Examples in Our Code

```csharp
// ? Each validator has ONE responsibility

/// <summary>
/// ONLY validates user booking limits
/// </summary>
public class UserBookingLimitValidator : IBookingValidator
{
    // Only checks: Has user exceeded booking limit?
}

/// <summary>
/// ONLY validates event availability
/// </summary>
public class EventAvailabilityValidator : IBookingValidator
{
    // Only checks: Is event still accepting bookings?
}

/// <summary>
/// ONLY validates user information completeness
/// </summary>
public class UserInformationValidator : IBookingValidator
{
    // Only checks: Does user have required information?
}

// ? Application service methods each have ONE responsibility

public class BookingApplicationService
{
    private async Task<ValidationResult> ValidateCommandAsync(...) { }     // ONLY validates
    private async Task<EntityLoadResult> LoadRequiredEntitiesAsync(...) { } // ONLY loads
    private async Task<PaymentResult> ProcessPaymentAsync(...) { }          // ONLY processes payment
    private async Task<PersistenceResult> PersistBookingAsync(...) { }      // ONLY persists
}
```

#### ? Anti-Pattern (What We Avoid)

```csharp
// BAD: God class with too many responsibilities
public class EventManager
{
    public void BookEvent() { }           // Responsibility 1
    public void ValidateCapacity() { }    // Responsibility 2
    public void CheckTimeConflicts() { }  // Responsibility 3
    public void SendNotifications() { }   // Responsibility 4
    public void ProcessPayment() { }      // Responsibility 5
    public void GenerateReports() { }     // Responsibility 6
}
```

### Benefits
- **Easier to understand**: Clear purpose for each class
- **Easier to test**: Focused unit tests
- **Easier to maintain**: Changes affect only related code
- **Easier to reuse**: Single-purpose classes are more reusable

### Where to Find Examples
- `src/EventBookingSystem.Domain/Services/BookingValidators.cs` - Focused validators
- `src/EventBookingSystem.Application/Services/BookingApplicationService.cs` - Separated concerns
- `src/EventBookingSystem.Application/Services/EventQueryService.cs` - Query-only service
- `src/EventBookingSystem.Application/Services/BookingQueryService.cs` - Query-only service

---

## Open/Closed Principle (OCP)

### Definition
> Software entities should be open for extension but closed for modification.

### In Our System

We can add new behavior without modifying existing code:

#### ? Examples in Our Code

```csharp
// ? Validator extensibility - NO modification needed to add new validators

// Existing service (NEVER changes)
public class BookingService : IBookingService
{
    private readonly IEnumerable<IBookingValidator> _bookingValidators;

    public ValidationResult ValidateBooking(...)
    {
        foreach (var validator in _bookingValidators)
        {
            var result = validator.Validate(...);
            if (!result.IsValid) return result;
        }
        return ValidationResult.Success();
    }
}

// Adding NEW validator - existing code unchanged!
public class CreditCardValidator : IBookingValidator  // New class
{
    public ValidationResult Validate(...)
    {
        // New validation logic
        if (user.CreditCard == null || user.CreditCard.IsExpired)
            return ValidationResult.Failure("Valid credit card required.");
        
        return ValidationResult.Success();
    }
}

// Only registration changes (composition root)
services.AddTransient<IBookingValidator, CreditCardValidator>();  // One line added
```

```csharp
// ? Strategy pattern - extend event types without modification

// Base strategy interface (stable)
public interface IReservationStrategy<TEvent> where TEvent : EventBase
{
    ValidationResult ValidateReservation(Venue venue, TEvent evnt, ReservationRequest request);
    void Reserve(Venue venue, TEvent evnt, ReservationRequest request);
}

// Existing strategies (closed for modification)
public class GeneralAdmissionReservationStrategy : IReservationStrategy<GeneralAdmissionEvent> { }
public class SectionBasedReservationStrategy : IReservationStrategy<SectionBasedEvent> { }
public class ReservedSeatingReservationStrategy : IReservationStrategy<ReservedSeatingEvent> { }

// Want VIP events? EXTEND, don't modify!
public class VipReservationStrategy : IReservationStrategy<VipEvent>  // New class
{
    public ValidationResult ValidateReservation(...) { /* VIP logic */ }
    public void Reserve(...) { /* VIP logic */ }
}
```

### Benefits
- **Risk reduction**: Existing code isn't touched
- **Regression prevention**: Working code stays working
- **Parallel development**: Multiple developers can add features simultaneously
- **Easy rollback**: New features can be disabled without affecting old code

### Where to Find Examples
- `src/EventBookingSystem.Domain/Services/BookingValidators.cs` - Validator collection
- `src/EventBookingSystem.Domain/Services/IReservationStrategy.cs` - Strategy interfaces
- `src/EventBookingSystem.Domain/Services/GeneralAdmissionStrategy.cs` - Strategy implementations
- `src/EventBookingSystem.Domain/Services/EventReservationService.cs` - Strategy context

---

## Liskov Substitution Principle (LSP)

### Definition
> Derived classes must be substitutable for their base classes without altering correctness.

### In Our System

All subtypes maintain the contracts of their base types:

#### ? Examples in Our Code

```csharp
// ? Event type hierarchy - all subtypes are substitutable

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
            return ValidationResult.Failure("Insufficient capacity.");
        
        return ValidationResult.Success();
    }
}

// ? All subtypes maintain the contract

public class GeneralAdmissionEvent : EventBase
{
    // ? Contract maintained - capacity is always valid
    public override int TotalCapacity => CapacityOverride ?? (Venue?.MaxCapacity ?? 0);
    public override int TotalReserved => Attendees;
    public override bool IsSoldOut => TotalReserved >= TotalCapacity;
}

public class ReservedSeatingEvent : EventBase
{
    // ? Contract maintained - capacity is always valid
    public override int TotalCapacity => Seats.Count;
    public override int TotalReserved => Seats.Count(s => s.Status == SeatStatus.Reserved);
    public override bool IsSoldOut => TotalReserved >= TotalCapacity;
}

public class SectionBasedEvent : EventBase
{
    // ? Contract maintained - capacity is always valid
    public override int TotalCapacity => CapacityOverride ?? SectionInventories.Sum(s => s.Capacity);
    public override int TotalReserved => SectionInventories.Sum(s => s.ReservedCount);
    public override bool IsSoldOut => TotalReserved >= TotalCapacity;
}

// Usage: ANY EventBase subtype works correctly
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

#### ? Anti-Pattern (What We Avoid)

```csharp
// BAD: Violates LSP - subtype weakens preconditions
public class SpecialEvent : EventBase
{
    public override ValidationResult ValidateCapacity(int quantity)
    {
        // BAD: Accepts negative quantities, violating base contract!
        if (IsSoldOut)
            return ValidationResult.Failure("Sold out");
        
        return ValidationResult.Success();
    }
}
```

### Benefits
- **Polymorphism**: Use base types without knowing concrete types
- **Reliability**: Behavior is predictable and consistent
- **Maintainability**: No special cases needed for subtypes
- **Testability**: Test base behavior applies to all subtypes

### Where to Find Examples
- `src/EventBookingSystem.Domain/Entities/EventBase.cs` - Base class with contract
- `src/EventBookingSystem.Domain/Entities/GeneralAdmissionEvent.cs` - Maintains contract
- `src/EventBookingSystem.Domain/Entities/ReservedSeatingEvent.cs` - Maintains contract
- `src/EventBookingSystem.Domain/Entities/SectionBasedEvent.cs` - Maintains contract

---

## Interface Segregation Principle (ISP)

### Definition
> Clients should not be forced to depend on interfaces they don't use.

### In Our System

Interfaces are small, focused, and role-specific:

#### ? Examples in Our Code

```csharp
// ? Small, focused interfaces

/// <summary>
/// ONLY validation operations
/// </summary>
public interface IBookingValidator
{
    ValidationResult Validate(User user, EventBase evnt, ReservationRequest request);
}

/// <summary>
/// ONLY booking business logic
/// </summary>
public interface IBookingService
{
    Booking CreateBooking(User user, EventBase evnt, ReservationRequest request);
    ValidationResult ValidateBooking(User user, EventBase evnt, ReservationRequest request);
}

/// <summary>
/// ONLY payment processing
/// </summary>
public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// ONLY event queries (read operations)
/// </summary>
public interface IEventQueryService
{
    Task<IEnumerable<EventAvailabilityDto>> GetFutureEventsWithAvailabilityAsync(CancellationToken cancellationToken);
    Task<IEnumerable<EventAvailabilityDto>> GetFutureEventsByVenueAsync(int venueId, CancellationToken cancellationToken);
    Task<EventAvailabilityDto?> GetEventAvailabilityAsync(int eventId, CancellationToken cancellationToken);
}

/// <summary>
/// ONLY booking queries (read operations)
/// </summary>
public interface IBookingQueryService
{
    Task<IEnumerable<BookingDto>> GetAllBookingsAsync(CancellationToken cancellationToken);
    Task<IEnumerable<BookingDto>> GetBookingsByUserAsync(int userId, CancellationToken cancellationToken);
    Task<BookingDto?> GetBookingByIdAsync(int bookingId, CancellationToken cancellationToken);
}
```

```csharp
// ? Repository pattern - base + specialized

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

// Different specialized repository - different needs
public interface IBookingRepository : IRepository<Booking>
{
    Task<IEnumerable<Booking>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
}
```

#### ? Anti-Pattern (What We Avoid)

```csharp
// BAD: Large interface forcing unnecessary dependencies
public interface IBookingManager
{
    // Validation methods
    ValidationResult ValidateBooking(...);
    ValidationResult ValidateUser(...);
    ValidationResult ValidateEvent(...);
    ValidationResult ValidatePayment(...);
    
    // Booking operations
    Booking CreateBooking(...);
    bool CancelBooking(...);
    bool RefundBooking(...);
    
    // Email operations
    void SendConfirmationEmail(...);
    void SendReminderEmail(...);
    void SendCancellationEmail(...);
    
    // Reporting
    Report GenerateBookingReport(...);
    Report GenerateRevenueReport(...);
    
    // Clients must implement ALL of these, even if they only need validation!
}
```

### Benefits
- **Reduced coupling**: Classes depend only on what they use
- **Easier implementation**: Smaller interfaces are easier to implement
- **Better maintainability**: Changes to one interface don't affect others
- **Clear contracts**: Interface purpose is obvious

### Where to Find Examples
- `src/EventBookingSystem.Application/Services/IEventQueryService.cs` - Query-only interface
- `src/EventBookingSystem.Application/Services/IBookingQueryService.cs` - Query-only interface
- `src/EventBookingSystem.Domain/Services/IBookingValidator.cs` - Validation-only interface
- `src/EventBookingSystem.Infrastructure/Interfaces/IEventRepository.cs` - Focused repository

---

## Dependency Inversion Principle (DIP)

### Definition
> High-level modules should not depend on low-level modules. Both should depend on abstractions.

### In Our System

All dependencies point to abstractions (interfaces), not concrete implementations:

#### ? Examples in Our Code

```csharp
// ? Domain Layer defines abstractions

namespace EventBookingSystem.Domain.Services
{
    // High-level module depends on abstraction
    public class BookingService : IBookingService
    {
        // Depends on abstraction, not concrete validator
        private readonly IEnumerable<IBookingValidator> _bookingValidators;
        
        // Depends on abstraction, not concrete service
        private readonly EventReservationService _reservationService;
        
        public BookingService(
            EventReservationService reservationService,
            params IBookingValidator[] bookingValidators)  // Abstraction injection
        {
            _reservationService = reservationService;
            _bookingValidators = bookingValidators;
        }
    }
}

// ? Infrastructure Layer defines interface for data access

namespace EventBookingSystem.Infrastructure.Interfaces
{
    // Abstraction defined in infrastructure
    public interface IEventRepository
    {
        Task<EventBase> AddAsync(EventBase entity, CancellationToken cancellationToken);
        Task<EventBase?> GetByIdAsync(int id, CancellationToken cancellationToken);
    }
}

// ? Infrastructure Layer implements abstraction

namespace EventBookingSystem.Infrastructure.Repositories
{
    // Low-level module implements abstraction
    public class DapperEventRepository : IEventRepository
    {
        private readonly IDBConnectionFactory _connectionFactory;  // Also depends on abstraction!
        
        public async Task<EventBase> AddAsync(EventBase entity, CancellationToken cancellationToken)
        {
            // Dapper-specific implementation details
        }
    }
}

// ? Application Layer depends on abstractions

namespace EventBookingSystem.Application.Services
{
    public class BookingApplicationService
    {
        // ALL dependencies are abstractions
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IUserRepository _userRepository;
        private readonly IVenueRepository _venueRepository;
        private readonly IBookingService _bookingService;
        private readonly IPaymentService _paymentService;
        
        public BookingApplicationService(
            IBookingRepository bookingRepository,
            IEventRepository eventRepository,
            IUserRepository userRepository,
            IVenueRepository venueRepository,
            IBookingService bookingService,
            IPaymentService paymentService)
        {
            // Dependency injection of abstractions
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _userRepository = userRepository;
            _venueRepository = venueRepository;
            _bookingService = bookingService;
            _paymentService = paymentService;
        }
    }
}
```

#### ? Anti-Pattern (What We Avoid)

```csharp
// BAD: Depending on concrete implementations
public class BookingService
{
    // BAD: Concrete dependency
    private readonly DapperEventRepository _eventRepository;
    
    // BAD: Concrete dependency
    private readonly SqlServerConnection _connection;
    
    public BookingService()
    {
        // BAD: Creating dependencies inside the class
        _connection = new SqlServerConnection("connection-string");
        _eventRepository = new DapperEventRepository(_connection);
    }
}
```

### Benefits
- **Testability**: Easy to inject test doubles/mocks
- **Flexibility**: Swap implementations without changing code
- **Decoupling**: Changes to implementations don't affect consumers
- **Parallel development**: Multiple teams can work on interfaces and implementations independently

### Where to Find Examples
- `src/EventBookingSystem.Domain/Services/BookingService.cs` - Depends on abstractions
- `src/EventBookingSystem.Application/Services/BookingApplicationService.cs` - Constructor injection
- `src/EventBookingSystem.Infrastructure/Repositories/DapperEventRepository.cs` - Implements abstraction
- All service and repository interfaces throughout the codebase

---

## Benefits in Our System

### Maintainability
- **Clear responsibilities**: Each class has one job
- **Minimal ripple effects**: Changes are localized
- **Easy to navigate**: Consistent patterns throughout

### Extensibility
- **Add features without modification**: Open/Closed Principle
- **Plug-in architecture**: Strategy patterns and validators
- **New event types**: Just add new strategies

### Testability
- **Unit testing**: Mock abstractions easily
- **Integration testing**: Test interactions between components
- **Test isolation**: Single responsibilities make tests focused

### Reliability
- **Substitutable types**: LSP ensures predictable behavior
- **Contract enforcement**: Interfaces define clear expectations
- **Reduced coupling**: Changes have limited scope

### Team Collaboration
- **Parallel development**: Work on different abstractions
- **Clear interfaces**: Well-defined contracts between components
- **Code reviews**: Focused, understandable code

---

## Quick Decision Guide

### When writing new code, ask:

#### Single Responsibility
- ? "Does this class have more than one reason to change?"
- ? **If yes**: Split it into multiple classes

#### Open/Closed
- ? "Will adding new features require modifying this class?"
- ? **If yes**: Use abstraction (interface/abstract class) and strategy/composition

#### Liskov Substitution
- ? "Can I replace a base class with any derived class without issues?"
- ? **If no**: Review the inheritance hierarchy and contracts

#### Interface Segregation
- ? "Does this interface have methods that some clients don't need?"
- ? **If yes**: Split into multiple focused interfaces

#### Dependency Inversion
- ? "Am I depending on a concrete class instead of an interface?"
- ? **If yes**: Create/use an interface and inject the dependency

### Red Flags to Watch For

| Red Flag | SOLID Violation | Solution |
|----------|-----------------|----------|
| Class with many methods | SRP | Split by responsibility |
| Modifying class for new features | OCP | Use abstraction + extension |
| Type checking (`if (x is Type)`) | LSP | Review inheritance hierarchy |
| Large interface | ISP | Split into focused interfaces |
| `new` keyword in constructor | DIP | Use dependency injection |
| Many imports from outer layers | Clean Architecture | Review dependencies |

---

## Learning Path

### For New Team Members

1. **Start with SRP**: Understand single responsibilities
   - Read: `BookingValidators.cs`
   - Exercise: Identify the single responsibility of each validator

2. **Learn OCP**: See extensibility in action
   - Read: `IBookingValidator` interface and implementations
   - Exercise: Add a new validator without modifying existing code

3. **Understand LSP**: See substitutability
   - Read: `EventBase` and its derived classes
   - Exercise: Use `EventBase` reference with different event types

4. **Practice ISP**: Work with focused interfaces
   - Read: Service interfaces in Application layer
   - Exercise: Compare focused interfaces vs. monolithic interface

5. **Master DIP**: See inversion in action
   - Read: `BookingApplicationService` constructor
   - Exercise: Write a unit test using interface mocks

### Recommended Reading Order

1. This document (high-level overview)
2. `docs/DesignPrinciples.md` (detailed examples)
3. Code files in this order:
   - Domain validators (`BookingValidators.cs`)
   - Domain services (`BookingService.cs`)
   - Application services (`BookingApplicationService.cs`)
   - Strategy implementations
   - Repository interfaces and implementations

---

## Summary

SOLID principles are not optional extras—they are the foundation of our architecture:

| Principle | Core Idea | Our Implementation |
|-----------|-----------|-------------------|
| **SRP** | One responsibility per class | Focused validators, services, and repositories |
| **OCP** | Extend without modifying | Validator collections, strategy patterns |
| **LSP** | Substitutable subtypes | Event hierarchy, strategy interfaces |
| **ISP** | Small, focused interfaces | Separate query/command interfaces |
| **DIP** | Depend on abstractions | Constructor injection throughout |

### The Result

? **Testable**: Easy to mock dependencies  
? **Maintainable**: Changes are localized  
? **Extensible**: Add features without breaking existing code  
? **Understandable**: Clear responsibilities and dependencies  
? **Reliable**: Predictable behavior and strong contracts  

---

## Related Documentation

- ?? **[DesignPrinciples.md](./DesignPrinciples.md)** - Comprehensive guide with detailed examples
- ?? **[Clean Architecture](./DesignPrinciples.md#clean-architecture)** - Architectural layering
- ?? **[Design Patterns](./DesignPrinciples.md#design-patterns)** - Strategy, Repository, and more
- ?? **[Testing Philosophy](./DesignPrinciples.md#testing-philosophy)** - How SOLID enables testing

---

**Document Version:** 1.0  
**Last Updated:** 2025-01-XX  
**Target Framework:** .NET 10, C# 14.0  
**For Questions:** See detailed examples in [DesignPrinciples.md](./DesignPrinciples.md)
