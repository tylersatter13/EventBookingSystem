# Event Booking System

A comprehensive event booking system built with **Clean Architecture** principles using **.NET 10** and **C# 14.0**. This system demonstrates enterprise-level software design patterns including SOLID principles, Domain-Driven Design (DDD), and Test-Driven Development (TDD).

## 🏗️ Architecture Overview

This project follows **Clean Architecture** with clear separation of concerns across four distinct layers:

```
┌─────────────────────────────────────────────────────────────┐
│                        Presentation                          │
│                     (Future: API/UI)                         │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                       Application Layer                      │
│  • Application Services (Orchestration)                      │
│  • DTOs & Commands                                          │
│  • Repository Interfaces (DIP)                              │
│  • Query Services                                           │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                        Domain Layer                          │
│  • Entities & Aggregates                                    │
│  • Domain Services                                          │
│  • Business Logic & Validation                              │
│  • Domain Events                                            │
└─────────────────────────────────────────────────────────────┘
                              ↑
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                      │
│  • Repository Implementations (Dapper)                       │
│  • Database Access (SQLite)                                 │
│  • External Service Integrations                            │
└─────────────────────────────────────────────────────────────┘
```

### Key Architectural Principles

- ✅ **Dependency Inversion Principle (DIP)**: Application defines interfaces; Infrastructure implements them
- ✅ **Single Responsibility Principle (SRP)**: Each class has one reason to change
- ✅ **Open/Closed Principle (OCP)**: Open for extension, closed for modification
- ✅ **Liskov Substitution Principle (LSP)**: Derived classes are substitutable
- ✅ **Interface Segregation Principle (ISP)**: Focused, client-specific interfaces

## 🎯 Features

### Event Management
- **Three Event Types**: General Admission, Section-Based, and Reserved Seating
- **Polymorphic Event Hierarchy**: Type-safe event handling with inheritance
- **Dynamic Capacity Management**: Flexible capacity override and real-time availability tracking
- **Time Conflict Detection**: Prevent double-booking of venues

### Booking System
- **Multi-Strategy Reservation**: Different booking strategies for each event type
- **Payment Integration**: Simulated payment processing with validation
- **Payment Rollback**: Automatic capacity release on payment failure
- **Comprehensive Validation**: Command-level and domain-level validation

### Query Services
- **Event Availability Queries**: Real-time availability data with detailed breakdowns
- **Booking Queries**: User bookings, venue bookings, and complex analytical queries
- **Customer Analytics**: Identify paid users, find potential customers for marketing

## 📁 Project Structure

```
EventBookingSystem/
├── src/
│   ├── EventBookingSystem.Domain/              # Domain Layer (Core Business Logic)
│   │   ├── Entities/                           # Domain entities
│   │   │   ├── EventBase.cs                    # Base event class
│   │   │   ├── GeneralAdmissionEvent.cs        # GA event type
│   │   │   ├── SectionBasedEvent.cs            # Section-based event type
│   │   │   ├── ReservedSeatingEvent.cs         # Reserved seating event type
│   │   │   ├── Booking.cs                      # Booking aggregate
│   │   │   ├── Venue.cs                        # Venue entity
│   │   │   └── User.cs                         # User entity
│   │   └── Services/                           # Domain services
│   │       ├── IBookingService.cs              # Booking service interface
│   │       ├── BookingService.cs               # Booking orchestration
│   │       ├── EventReservationService.cs      # Reservation logic
│   │       ├── IReservationStrategy.cs         # Strategy pattern interface
│   │       └── BookingValidators.cs            # Domain validators
│   │
│   ├── EventBookingSystem.Application/          # Application Layer (Use Cases)
│   │   ├── Services/                           # Application services
│   │   │   ├── BookingApplicationService.cs    # Booking orchestration
│   │   │   ├── BookingQueryService.cs          # Booking queries
│   │   │   ├── EventQueryService.cs            # Event queries
│   │   │   └── IPaymentService.cs              # Payment abstraction
│   │   ├── DTOs/                               # Data Transfer Objects
│   │   │   ├── BookingDto.cs
│   │   │   ├── EventAvailabilityDto.cs
│   │   │   └── BookingResultDto.cs
│   │   ├── Models/                             # Command models
│   │   │   └── CreateBookingCommand.cs
│   │   ├── Validators/                         # Application validators
│   │   │   └── BookingCommandValidators.cs
│   │   └── Interfaces/                         # Repository interfaces (DIP)
│   │       ├── IBookingRepository.cs
│   │       ├── IEventRepository.cs
│   │       ├── IUserRepository.cs
│   │       └── IVenueRepository.cs
│   │
│   └── EventBookingSystem.Infrastructure/       # Infrastructure Layer (External Concerns)
│       ├── Repositories/                       # Repository implementations
│       │   ├── DapperBookingRepository.cs      # Dapper-based booking repo
│       │   ├── DapperEventRepository.cs        # Polymorphic event repo
│       │   ├── DapperUserRepository.cs
│       │   └── DapperVenueRepository.cs
│       ├── Data/                               # Database infrastructure
│       │   ├── IDBConnectionFactory.cs
│       │   └── SqlScriptExecutor.cs
│       ├── Mapping/                            # DTO-Entity mappers
│       └── Models/                             # Database DTOs
│
├── tests/
│   ├── EventBookingSystem.Domain.Tests/         # Domain unit tests
│   ├── EventBookingSystem.Application.Tests/    # Application unit tests
│   ├── EventBookingSystem.Application.IntegrationTests/  # Integration tests
│   └── EventBookingSystem.Infrastructure.Tests/ # Infrastructure tests
│
└── docs/                                        # Documentation
    ├── CodeArchitecture-Diagrams.md
    ├── DatabaseSchema-Diagram.md
    ├── SOLID-Principles-Overview.md
    └── ...
```

## 🚀 Getting Started

### Prerequisites

- **.NET 10 SDK** or later
- **Visual Studio 2022** (v17.13+) or **Visual Studio Code**
- **SQLite** (included with .NET)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/tylersatter13/EventBookingSystem.git
   cd EventBookingSystem
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

## 🧪 Testing Strategy

The project includes comprehensive test coverage across all layers:

### Unit Tests (MSTest + FluentAssertions)
- **Domain Tests**: Business logic, entities, and domain services
- **Application Tests**: Service orchestration and validation logic
- **Infrastructure Tests**: Repository implementations and data access

### Integration Tests
- **Database Integration**: SQLite in-memory database for fast, isolated tests
- **End-to-End Workflows**: Complete booking flows from command to persistence
- **Query Service Tests**: Complex analytical queries

### Test Coverage
```
Domain Layer:        95%+
Application Layer:   90%+
Infrastructure:      85%+
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/EventBookingSystem.Domain.Tests

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## 💾 Database Schema

The system uses **SQLite** with a comprehensive schema supporting:

- **Table Per Hierarchy (TPH)**: Polymorphic event storage with discriminator
- **Referential Integrity**: Foreign keys with cascade delete
- **Optimized Indexes**: Performance-tuned for common queries
- **Check Constraints**: Database-level validation

### Key Tables
- `Events` (with discriminator for event types)
- `EventSectionInventories` (section-based event capacity)
- `EventSeats` (reserved seating status)
- `Bookings` (customer bookings)
- `BookingItems` (booking line items)
- `Venues` / `VenueSections` / `VenueSeats`
- `Users`

See [DatabaseSchema-Diagram.md](docs/DatabaseSchema-Diagram.md) for detailed schema documentation.

## 🎨 Design Patterns Used

### Creational Patterns
- **Factory Pattern**: Connection factories for database access
- **Builder Pattern**: Test data builders for complex object creation

### Structural Patterns
- **Repository Pattern**: Data access abstraction
- **Adapter Pattern**: Dapper adapters for domain entities
- **Composite Pattern**: Validator composition

### Behavioral Patterns
- **Strategy Pattern**: Different reservation strategies for event types
- **Template Method**: Base validation template in domain services
- **Chain of Responsibility**: Validator chains

## 📊 Domain Model

### Event Hierarchy

```
EventBase (Abstract)
├── GeneralAdmissionEvent
│   ├── Capacity: int
│   ├── TotalReserved: int
│   └── Price: decimal
├── SectionBasedEvent
│   └── SectionInventories: List<EventSectionInventory>
│       ├── VenueSectionId
│       ├── Capacity
│       ├── Booked
│       └── Price
└── ReservedSeatingEvent
    └── Seats: List<EventSeat>
        ├── VenueSeatId
        ├── Status (Available/Reserved/Locked)
        └── VenueSeat navigation
```

### Booking Aggregate

```
Booking (Aggregate Root)
├── User
├── Event
├── BookingType (GA/Section/Seat)
├── PaymentStatus (Pending/Paid/Refunded/Failed)
├── TotalAmount
└── BookingItems: List<BookingItem>
    ├── EventSeat (for reserved seating)
    ├── EventSectionInventory (for section-based)
    └── Quantity
```

## 🔧 Key Services

### BookingApplicationService
Orchestrates the complete booking workflow:
1. ✅ Command validation
2. 🔍 Entity loading
3. ✔️ Domain validation
4. 📝 Booking creation
5. 💳 Payment processing
6. 💾 Persistence
7. 🔄 Rollback on failure

### EventQueryService
Provides event availability information:
- Future events with availability
- Events by venue with detailed breakdowns
- Section-level availability
- Seat-level availability

### BookingQueryService
Complex booking queries:
- User bookings
- Venue bookings
- Bookings for paid users (analytics)
- Users without bookings (marketing)

## 📚 Documentation

Comprehensive documentation is available in the `docs/` directory:

- **[SOLID-Principles-Overview.md](docs/SOLID-Principles-Overview.md)**: SOLID implementation guide
- **[CodeArchitecture-Diagrams.md](docs/CodeArchitecture-Diagrams.md)**: Architecture diagrams
- **[DatabaseSchema-Diagram.md](docs/DatabaseSchema-Diagram.md)**: Database design
- **[IntegrationTestingStrategy.md](docs/IntegrationTestingStrategy.md)**: Testing approach
- **[PaymentIntegrationSummary.md](docs/PaymentIntegrationSummary.md)**: Payment workflow
- **[MigrationGuide.md](docs/MigrationGuide.md)**: Evolution of the codebase


### Development Guidelines

1. **Follow SOLID principles**
2. **Write tests first** (TDD approach)
3. **Use FluentAssertions** for all test assertions
4. **Document public APIs** with XML comments
5. **Keep classes focused** on single responsibilities
6. **Use interfaces** for extensibility

### Code Style

- **C# 14.0 features**: Use latest language features (required properties, pattern matching, etc.)
- **Nullable reference types**: Enabled throughout
- **Async/await**: Use for all I/O operations
- **Naming conventions**: Follow standard C# conventions

## 📝 License

This project is created for educational purposes to demonstrate Clean Architecture, SOLID principles, and modern .NET development practices.

## 🙏 Acknowledgments

Built following:
- **Clean Architecture** by Robert C. Martin
- **Domain-Driven Design** by Eric Evans
- **SOLID Principles** by Robert C. Martin
- **.NET Best Practices** by Microsoft

## 📧 Contact

Tyler Satter - [GitHub](https://github.com/tylersatter13)

Project Link: [https://github.com/tylersatter13/EventBookingSystem](https://github.com/tylersatter13/EventBookingSystem)

---

## 🎓 Learning Outcomes

This project demonstrates:

✅ **Clean Architecture** implementation in .NET  
✅ **SOLID principles** in real-world scenarios  
✅ **Domain-Driven Design** patterns and practices  
✅ **Test-Driven Development** with comprehensive coverage  
✅ **Repository Pattern** with Dependency Inversion  
✅ **Strategy Pattern** for polymorphic behavior  
✅ **Dapper micro-ORM** for performance and control  
✅ **SQLite** for lightweight data persistence  
✅ **MSTest + FluentAssertions** for expressive testing  
✅ **Integration testing** with in-memory databases  

---
