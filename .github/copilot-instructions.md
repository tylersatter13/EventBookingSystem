# GitHub Copilot Custom Instructions for EventBookingSystem

## Project Overview
This is an Event Booking System built using .NET 10 and C# 14.0, following Clean Architecture principles with a focus on maintainability, testability, and adherence to SOLID design principles.

## Architecture Principles

### Clean Architecture
- **Domain Layer** (`EventBookingSystem.Domain`): Contains business logic, entities, domain services, and interfaces. This layer has NO dependencies on other layers.
- **Application Layer** (`EventBookingSystem.Application`): Contains application-specific business rules, use cases, and application services. Depends only on Domain.
- **Infrastructure Layer** (`EventBookingSystem.Infrastructure`): Contains implementations for external concerns (databases, file systems, web services). Depends on Domain and Application.
- **Dependency Rule**: Dependencies should always point inward. Inner layers should never depend on outer layers.

### SOLID Principles

#### Single Responsibility Principle (SRP)
- Each class should have one, and only one, reason to change
- Separate validation logic from business logic
- Use dedicated validator classes (e.g., `CapacityValidator`, `TimeConflictValidator`)
- Keep services focused on a single responsibility

#### Open/Closed Principle (OCP)
- Classes should be open for extension but closed for modification
- Use interfaces and abstract classes to allow behavior extension
- Leverage dependency injection for extensibility
- Prefer composition over modification when adding new features

#### Liskov Substitution Principle (LSP)
- Derived classes must be substitutable for their base classes
- Ensure interface implementations maintain expected behavior contracts
- Avoid breaking preconditions or postconditions in derived types

#### Interface Segregation Principle (ISP)
- Create small, focused interfaces rather than large, monolithic ones
- Clients should not be forced to depend on interfaces they don't use
- Example: `IEventBookingValidator`, `ISeatReservationValidator` are focused interfaces

#### Dependency Inversion Principle (DIP)
- Depend on abstractions, not concretions
- Use interfaces for all dependencies between layers
- Configure dependency injection in the composition root
- Domain layer defines interfaces; Infrastructure implements them

## Code Style and Conventions

### General Guidelines
- Use **C# 14.0** features and idioms where appropriate
- Target **.NET 10** framework features
- Use `required` keyword for mandatory properties
- Prefer records for immutable data structures
- Use nullable reference types consistently
- Include XML documentation comments for public APIs

### Naming Conventions
- Use meaningful, descriptive names
- Follow standard C# naming conventions (PascalCase for types and public members, camelCase for parameters and local variables)
- Prefix interfaces with `I` (e.g., `IEventBookingValidator`)
- Use descriptive names that reveal intent (e.g., `TimeConflictValidator` not `Validator2`)

### Error Handling
- Use exceptions for exceptional conditions, not flow control
- Create custom exceptions when domain-specific errors occur
- Include meaningful error messages
- Use `ValidationResult` pattern for validation failures instead of exceptions where appropriate

## Unit Testing Standards

### Testing Framework
- **Primary Framework**: MSTest (Microsoft.VisualStudio.TestTools.UnitTesting)
- **Assertions Library**: FluentAssertions (Awesome Assertions)
- **Test Projects**: Follow naming convention `[ProjectName].Tests`

### Test Structure
```csharp
[TestClass]
public class ServiceNameTests
{
    [TestMethod]
    public void MethodName_Scenario_ExpectedBehavior()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var input = CreateTestInput();
        
        // Act
        var result = sut.MethodName(input);
        
        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }
}
```

### FluentAssertions Guidelines
- **Always use FluentAssertions** for assertions instead of MSTest's `Assert` class
- Use method chaining for readable assertions: `result.Should().NotBeNull().And.Be(expectedValue);`
- Prefer specific assertions over generic ones: `collection.Should().HaveCount(3)` over `collection.Count.Should().Be(3)`
- Include descriptive failure messages using `because`: `result.Should().BeTrue(because: "the event is within capacity");`

### Common FluentAssertions Patterns
```csharp
// Boolean assertions
result.Should().BeTrue();
result.Should().BeFalse();

// Null/Not Null
instance.Should().NotBeNull();
instance.Should().BeNull();

// Equality
actual.Should().Be(expected);
actual.Should().NotBe(unwanted);

// Collections
collection.Should().HaveCount(5);
collection.Should().Contain(item);
collection.Should().NotContain(item);
collection.Should().BeEmpty();
collection.Should().ContainSingle();

// Exceptions
Action act = () => sut.Method();
act.Should().Throw<InvalidOperationException>()
   .WithMessage("expected message");

// Strings
text.Should().Contain("substring");
text.Should().StartWith("prefix");
text.Should().Match("pattern*");

// Types
instance.Should().BeOfType<ConcreteType>();
instance.Should().BeAssignableTo<IInterface>();

// Numeric comparisons
number.Should().BeGreaterThan(5);
number.Should().BeLessThanOrEqualTo(10);
number.Should().BeInRange(1, 100);
```

### Test Naming Convention
- Use pattern: `MethodName_Scenario_ExpectedBehavior`
- Examples:
  - `Validate_WhenCapacityExceeded_ReturnsFailure`
  - `BookEvent_WithValidEvent_AddsEventToVenue`
  - `BookEvent_WithConflictingTime_ThrowsInvalidOperationException`

### Test Organization
- One test class per production class
- Group related tests with regions or nested classes if needed
- Keep tests independent and isolated
- Use test initialization (`[TestInitialize]`) for common setup
- Use test cleanup (`[TestCleanup]`) when needed

### Mocking and Test Doubles
- Use appropriate test doubles (mocks, stubs, fakes)
- Consider manual stubs for simple interfaces
- Use mocking frameworks (like Moq) for complex scenarios only when beneficial
- Verify behavior, not implementation details

## Domain-Driven Design (DDD) Patterns

### Entities
- Have identity (Id property)
- Can change state over time
- Encapsulate business logic and invariants
- Use validation in constructors or factory methods

### Value Objects
- Immutable
- Defined by their properties, not identity
- Use records when appropriate
- Example: `ValidationResult`

### Domain Services
- Contain business logic that doesn't naturally fit in entities
- Example: `EventBookingService`, `SeatReservationService`
- Should be stateless
- Use validator pattern for validation logic

### Aggregates
- Enforce consistency boundaries
- Access through aggregate root only
- Example: `Venue` is the aggregate root for venue-related operations

## Validation Patterns

### Validator Pattern
- Create focused validator classes implementing validation interfaces
- Each validator has a single responsibility
- Return `ValidationResult` with success/failure and error messages
- Allow composition of multiple validators
- Example:
```csharp
public interface IEventBookingValidator
{
    ValidationResult Validate(Venue venue, Event evnt);
}

public class CapacityValidator : IEventBookingValidator
{
    public ValidationResult Validate(Venue venue, Event evnt)
    {
        return evnt.EstimatedAttendance > venue.MaxCapacity
            ? ValidationResult.Failure("The event exceeds the venue's maximum capacity.")
            : ValidationResult.Success();
    }
}
```

## Dependency Injection

- Register services in the composition root (startup/program file)
- Use constructor injection for required dependencies
- Inject interfaces, not concrete types
- Keep constructors simple (parameter assignment only)
- Use params array when appropriate for collection of same-type dependencies

## Code Quality Standards

- **No magic numbers**: Use named constants or configuration
- **Avoid premature optimization**: Focus on clarity first
- **Write self-documenting code**: Clear names reduce need for comments
- **Keep methods short**: Aim for 10-20 lines per method
- **Minimize nesting**: Extract methods to reduce complexity
- **Use guard clauses**: Return early to reduce nesting

## When Suggesting Code

1. **Always consider SOLID principles** before suggesting a design
2. **Follow Clean Architecture** layering and dependency rules
3. **Generate MSTest tests** with FluentAssertions for new functionality
4. **Include XML documentation** for public APIs
5. **Use appropriate C# 14.0/.NET 10 features**
6. **Validate architectural decisions** against Clean Architecture principles
7. **Suggest interfaces** when abstraction is beneficial
8. **Prefer composition** over inheritance
9. **Keep validators separate** from business logic
10. **Write testable code** with clear dependencies

## Examples to Follow

### Good: Validator Pattern with DIP
```csharp
// Interface in Domain layer
public interface IEventBookingValidator
{
    ValidationResult Validate(Venue venue, Event evnt);
}

// Implementation in Domain layer
public class CapacityValidator : IEventBookingValidator
{
    public ValidationResult Validate(Venue venue, Event evnt)
    {
        return evnt.EstimatedAttendance > venue.MaxCapacity
            ? ValidationResult.Failure("The event exceeds the venue's maximum capacity.")
            : ValidationResult.Success();
    }
}

// Service using validators (OCP - open for extension)
public class EventBookingService
{
    private readonly IEventBookingValidator[] _validators;

    public EventBookingService(params IEventBookingValidator[] validators)
    {
        _validators = validators;
    }

    public void BookEvent(Venue venue, Event evnt)
    {
        foreach (var validator in _validators)
        {
            var result = validator.Validate(venue, evnt);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(result.ErrorMessage);
            }
        }
        
        venue.BookEvent(evnt);
    }
}
```

### Good: MSTest with FluentAssertions
```csharp
[TestClass]
public class CapacityValidatorTests
{
    [TestMethod]
    public void Validate_WhenAttendanceExceedsCapacity_ReturnsFailureResult()
    {
        // Arrange
        var validator = new CapacityValidator();
        var venue = new Venue { MaxCapacity = 100 };
        var evnt = new Event { EstimatedAttendance = 150 };

        // Act
        var result = validator.Validate(venue, evnt);

        // Assert
        result.IsValid.Should().BeFalse(because: "attendance exceeds venue capacity");
        result.ErrorMessage.Should().Contain("exceeds the venue's maximum capacity");
    }

    [TestMethod]
    public void Validate_WhenAttendanceWithinCapacity_ReturnsSuccessResult()
    {
        // Arrange
        var validator = new CapacityValidator();
        var venue = new Venue { MaxCapacity = 100 };
        var evnt = new Event { EstimatedAttendance = 80 };

        // Act
        var result = validator.Validate(venue, evnt);

        // Assert
        result.IsValid.Should().BeTrue(because: "attendance is within venue capacity");
    }
}
```

## Anti-Patterns to Avoid

? **Don't violate Clean Architecture dependencies**
```csharp
// BAD: Domain depending on Infrastructure
using EventBookingSystem.Infrastructure;
namespace EventBookingSystem.Domain { }
```

? **Don't use MSTest Assert class**
```csharp
// BAD: Using MSTest Assert
Assert.IsTrue(result.IsValid);
Assert.AreEqual(expected, actual);

// GOOD: Using FluentAssertions
result.IsValid.Should().BeTrue();
actual.Should().Be(expected);
```

? **Don't create god classes**
```csharp
// BAD: Class with too many responsibilities
public class EventManager
{
    public void BookEvent() { }
    public void ValidateCapacity() { }
    public void CheckTimeConflicts() { }
    public void SendNotifications() { }
    public void ProcessPayment() { }
}
```

? **Don't use concrete types for dependencies**
```csharp
// BAD: Depending on concrete implementation
public class EventService
{
    private readonly EventRepository _repository;
}

// GOOD: Depending on abstraction
public class EventService
{
    private readonly IEventRepository _repository;
}
```

## Summary

When working on this project:
- ??? **Respect Clean Architecture** boundaries and dependency rules
- ?? **Apply SOLID principles** to every design decision
- ? **Write tests using MSTest** with **FluentAssertions** for all assertions
- ?? **Document public APIs** with XML comments
- ?? **Keep classes focused** and responsibilities clear
- ?? **Use interfaces** for extensibility and testability
- ?? **Test behavior**, not implementation details
