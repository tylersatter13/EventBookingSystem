# Event Booking System - Code Architecture Diagrams

## Comprehensive Visual Guide to the Codebase

This document provides Mermaid diagrams illustrating the code structure, relationships, and design patterns in the Event Booking System.

---

## Table of Contents

1. [Clean Architecture Layers](#clean-architecture-layers)
2. [Domain Model Class Diagram](#domain-model-class-diagram)
3. [Event Type Hierarchy](#event-type-hierarchy)
4. [Strategy Pattern Implementation](#strategy-pattern-implementation)
5. [Validator Pattern Implementation](#validator-pattern-implementation)
6. [Repository Pattern Architecture](#repository-pattern-architecture)
7. [Application Service Flow](#application-service-flow)
8. [Mapper Pattern](#mapper-pattern)
9. [Dependency Injection Container](#dependency-injection-container)
10. [Request Flow Sequence Diagrams](#request-flow-sequence-diagrams)

---

## Clean Architecture Layers

### Layer Dependencies

```mermaid
graph TB
    subgraph "Presentation Layer (Future)"
        API[Web API / UI]
    end
    
    subgraph "Application Layer"
        AppServices[Application Services]
        DTOs[DTOs]
        Commands[Commands]
        Validators[Command Validators]
        QueryServices[Query Services]
    end
    
    subgraph "Domain Layer"
        Entities[Domain Entities]
        DomainServices[Domain Services]
        DomainInterfaces[Domain Interfaces]
        ValueObjects[Value Objects]
        Strategies[Strategy Implementations]
    end
    
    subgraph "Infrastructure Layer"
        Repositories[Repository Implementations]
        DataAccess[Data Access (Dapper)]
        Mappers[Entity-DTO Mappers]
        DbModels[Database DTOs]
    end
    
    subgraph "External"
        Database[(SQLite Database)]
    end
    
    API --> AppServices
    API --> QueryServices
    
    AppServices --> DomainServices
    AppServices --> Repositories
    QueryServices --> Repositories
    
    Repositories --> DataAccess
    Repositories --> Mappers
    DataAccess --> Database
    
    DomainServices --> Entities
    DomainServices --> DomainInterfaces
    DomainServices --> Strategies
    
    Mappers --> Entities
    Mappers --> DbModels
    
    style Domain Layer fill:#c8e6c9
    style Application Layer fill:#fff9c4
    style Infrastructure Layer fill:#b3e5fc
    style External fill:#ffccbc
```

### Project Structure

```mermaid
graph LR
    subgraph "Solution: EventBookingSystem"
        Domain[EventBookingSystem.Domain]
        App[EventBookingSystem.Application]
        Infra[EventBookingSystem.Infrastructure]
        
        DomainTests[EventBookingSystem.Domain.Tests]
        AppTests[EventBookingSystem.Application.Tests]
        InfraTests[EventBookingSystem.Infrastructure.Tests]
        IntegrationTests[EventBookingSystem.Application.IntegrationTests]
    end
    
    App --> Domain
    Infra --> Domain
    Infra --> App
    
    DomainTests -.tests.-> Domain
    AppTests -.tests.-> App
    InfraTests -.tests.-> Infra
    IntegrationTests -.tests.-> App
    IntegrationTests -.tests.-> Infra
    
    style Domain fill:#c8e6c9
    style App fill:#fff9c4
    style Infra fill:#b3e5fc
    style DomainTests fill:#e8f5e9
    style AppTests fill:#fffde7
    style InfraTests fill:#e1f5fe
    style IntegrationTests fill:#fce4ec
```

---

## Domain Model Class Diagram

### Core Entities and Relationships

```mermaid
classDiagram
    %% Core Entities
    class User {
        +int Id
        +string Name
        +string Email
        +string PhoneNumber
        +List~Booking~ Bookings
    }
    
    class Venue {
        +int Id
        +string Name
        +string Address
        +List~VenueSection~ VenueSections
        +int MaxCapacity
        +VenueSection GetSection(int sectionId)
    }
    
    class VenueSection {
        +int Id
        +int VenueId
        +string Name
        +List~VenueSeat~ VenueSeats
        +int Capacity
    }
    
    class VenueSeat {
        +int Id
        +int VenueSectionId
        +string Row
        +string SeatNumber
        +string SeatLabel
    }
    
    class Booking {
        +int Id
        +int UserId
        +User User
        +int EventId
        +EventBase Event
        +BookingType BookingType
        +PaymentStatus PaymentStatus
        +decimal TotalAmount
        +DateTime CreatedAt
        +List~BookingItem~ BookingItems
    }
    
    class BookingItem {
        +int Id
        +int BookingId
        +int? EventSeatId
        +EventSeat EventSeat
        +int? EventSectionInventoryId
        +EventSectionInventory EventSectionInventory
        +int Quantity
    }
    
    %% Event Hierarchy
    class EventBase {
        <<abstract>>
        +int Id
        +int VenueId
        +Venue Venue
        +string Name
        +DateTime StartsAt
        +DateTime? EndsAt
        +int EstimatedAttendance
        +int TotalCapacity*
        +int TotalReserved*
        +bool IsSoldOut*
        +int AvailableCapacity
        +ValidationResult ValidateCapacity(int quantity)*
    }
    
    class GeneralAdmissionEvent {
        +int Capacity
        +int Attendees
        +decimal? Price
        +int? CapacityOverride
        +void ReserveTickets(int quantity)
        +override int TotalCapacity
        +override int TotalReserved
        +override bool IsSoldOut
    }
    
    class SectionBasedEvent {
        +int? CapacityOverride
        +List~EventSectionInventory~ SectionInventories
        +EventSectionInventory GetSection(int sectionId)
        +void ReserveInSection(int sectionId, int quantity)
        +override int TotalCapacity
        +override int TotalReserved
        +override bool IsSoldOut
    }
    
    class ReservedSeatingEvent {
        +List~EventSeat~ Seats
        +EventSeat GetSeat(int seatId)
        +void ReserveSeat(int seatId)
        +ValidationResult ValidateSeatReservation(int seatId)
        +override int TotalCapacity
        +override int TotalReserved
        +override bool IsSoldOut
    }
    
    class EventSectionInventory {
        +int Id
        +int EventId
        +int VenueSectionId
        +VenueSection VenueSection
        +int Capacity
        +int ReservedCount
        +decimal? Price
        +SeatAllocationMode AllocationMode
        +int AvailableCapacity
    }
    
    class EventSeat {
        +int Id
        +int EventId
        +int VenueSeatId
        +VenueSeat VenueSeat
        +SeatStatus Status
        +void Reserve()
        +void Lock()
        +void Release()
    }
    
    %% Enums
    class BookingType {
        <<enumeration>>
        GA
        Section
        Seat
    }
    
    class PaymentStatus {
        <<enumeration>>
        Pending
        Paid
        Refunded
        Failed
    }
    
    class SeatStatus {
        <<enumeration>>
        Available
        Reserved
        Locked
    }
    
    class SeatAllocationMode {
        <<enumeration>>
        GeneralAdmission
        Reserved
        BestAvailable
    }
    
    %% Relationships
    User "1" --> "*" Booking : makes
    Venue "1" --> "*" VenueSection : has
    Venue "1" --> "*" EventBase : hosts
    VenueSection "1" --> "*" VenueSeat : contains
    VenueSection "1" --> "*" EventSectionInventory : referenced by
    VenueSeat "1" --> "*" EventSeat : used in
    
    EventBase <|-- GeneralAdmissionEvent : extends
    EventBase <|-- SectionBasedEvent : extends
    EventBase <|-- ReservedSeatingEvent : extends
    
    EventBase "1" --> "*" Booking : receives
    Booking "1" --> "*" BookingItem : contains
    BookingItem "*" --> "0..1" EventSeat : references
    BookingItem "*" --> "0..1" EventSectionInventory : references
    
    SectionBasedEvent "1" --> "*" EventSectionInventory : manages
    ReservedSeatingEvent "1" --> "*" EventSeat : manages
```

---

## Event Type Hierarchy

### Inheritance and Polymorphism

```mermaid
classDiagram
    class EventBase {
        <<abstract>>
        +int Id
        +int VenueId
        +Venue? Venue
        +string Name
        +DateTime StartsAt
        +DateTime? EndsAt
        +int EstimatedAttendance
        
        <<abstract>> +int TotalCapacity
        <<abstract>> +int TotalReserved
        <<abstract>> +bool IsSoldOut
        
        +int AvailableCapacity
        +ValidationResult ValidateCapacity(int quantity)
    }
    
    class GeneralAdmissionEvent {
        +int Capacity
        +int Attendees
        +decimal? Price
        +int? CapacityOverride
        
        +void ReserveTickets(int quantity)
        
        <<override>> +int TotalCapacity
        <<override>> +int TotalReserved
        <<override>> +bool IsSoldOut
    }
    
    class SectionBasedEvent {
        +int? CapacityOverride
        +List~EventSectionInventory~ SectionInventories
        
        +EventSectionInventory? GetSection(int sectionId)
        +void ReserveInSection(int sectionId, int quantity)
        
        <<override>> +int TotalCapacity
        <<override>> +int TotalReserved
        <<override>> +bool IsSoldOut
    }
    
    class ReservedSeatingEvent {
        +List~EventSeat~ Seats
        
        +EventSeat? GetSeat(int seatId)
        +void ReserveSeat(int seatId)
        +ValidationResult ValidateSeatReservation(int seatId)
        
        <<override>> +int TotalCapacity
        <<override>> +int TotalReserved
        <<override>> +bool IsSoldOut
    }
    
    EventBase <|-- GeneralAdmissionEvent
    EventBase <|-- SectionBasedEvent
    EventBase <|-- ReservedSeatingEvent
    
    note for EventBase "Abstract base class defines\ncontract for all event types.\nLSP: All subtypes are\nsubstitutable."
    
    note for GeneralAdmissionEvent "Simple capacity counter.\nUse case: Festivals,\nstanding-room events."
    
    note for SectionBasedEvent "Section-level inventory.\nUse case: Theaters with\ntiered pricing."
    
    note for ReservedSeatingEvent "Individual seat management.\nUse case: Concerts,\nBroadway shows."
```

### Event Type Strategy Mapping

```mermaid
graph LR
    subgraph "Domain Entities"
        GA[GeneralAdmissionEvent]
        SB[SectionBasedEvent]
        RS[ReservedSeatingEvent]
    end
    
    subgraph "Strategy Implementations"
        GAStrat[GeneralAdmissionReservationStrategy]
        SBStrat[SectionBasedReservationStrategy]
        RSStrat[ReservedSeatingReservationStrategy]
    end
    
    subgraph "Service Context"
        ERS[EventReservationService]
    end
    
    GA -.handled by.-> GAStrat
    SB -.handled by.-> SBStrat
    RS -.handled by.-> RSStrat
    
    GAStrat --> ERS
    SBStrat --> ERS
    RSStrat --> ERS
    
    style GA fill:#c8e6c9
    style SB fill:#fff9c4
    style RS fill:#ffccbc
    style GAStrat fill:#a5d6a7
    style SBStrat fill:#fff59d
    style RSStrat fill:#ffab91
```

---

## Strategy Pattern Implementation

### Reservation Strategy Pattern

```mermaid
classDiagram
    class IReservationStrategy~TEvent~ {
        <<interface>>
        +ValidationResult ValidateReservation(Venue, TEvent, ReservationRequest)
        +void Reserve(Venue, TEvent, ReservationRequest)
    }
    
    class GeneralAdmissionReservationStrategy {
        +ValidationResult ValidateReservation(Venue, GeneralAdmissionEvent, ReservationRequest)
        +void Reserve(Venue, GeneralAdmissionEvent, ReservationRequest)
    }
    
    class SectionBasedReservationStrategy {
        +ValidationResult ValidateReservation(Venue, SectionBasedEvent, ReservationRequest)
        +void Reserve(Venue, SectionBasedEvent, ReservationRequest)
    }
    
    class ReservedSeatingReservationStrategy {
        +ValidationResult ValidateReservation(Venue, ReservedSeatingEvent, ReservationRequest)
        +void Reserve(Venue, ReservedSeatingEvent, ReservationRequest)
    }
    
    class EventReservationService {
        -GeneralAdmissionReservationStrategy _generalAdmissionStrategy
        -SectionBasedReservationStrategy _sectionBasedStrategy
        -ReservedSeatingReservationStrategy _reservedSeatingStrategy
        
        +ValidationResult ValidateGeneralAdmission(Venue, GeneralAdmissionEvent, int)
        +void ReserveGeneralAdmission(Venue, GeneralAdmissionEvent, int)
        +ValidationResult ValidateSectionBased(Venue, SectionBasedEvent, int, int)
        +void ReserveSectionBased(Venue, SectionBasedEvent, int, int)
        +ValidationResult ValidateReservedSeating(Venue, ReservedSeatingEvent, int)
        +void ReserveReservedSeating(Venue, ReservedSeatingEvent, int)
    }
    
    class ReservationRequest {
        +int Quantity
        +int CustomerId
        +int? SectionId
        +int? SeatId
    }
    
    IReservationStrategy~TEvent~ <|.. GeneralAdmissionReservationStrategy : implements
    IReservationStrategy~TEvent~ <|.. SectionBasedReservationStrategy : implements
    IReservationStrategy~TEvent~ <|.. ReservedSeatingReservationStrategy : implements
    
    EventReservationService --> GeneralAdmissionReservationStrategy : uses
    EventReservationService --> SectionBasedReservationStrategy : uses
    EventReservationService --> ReservedSeatingReservationStrategy : uses
    
    GeneralAdmissionReservationStrategy ..> ReservationRequest : uses
    SectionBasedReservationStrategy ..> ReservationRequest : uses
    ReservedSeatingReservationStrategy ..> ReservationRequest : uses
    
    note for IReservationStrategy~TEvent~ "Generic strategy interface.\nOCP: Add new strategies\nwithout modifying existing."
    
    note for EventReservationService "Context: Delegates to\nappropriate strategy\nbased on event type."
```

---

## Validator Pattern Implementation

### Booking Validator Composition

```mermaid
classDiagram
    class IBookingValidator {
        <<interface>>
        +ValidationResult Validate(User, EventBase, ReservationRequest)
    }
    
    class UserBookingLimitValidator {
        +ValidationResult Validate(User, EventBase, ReservationRequest)
    }
    
    class EventAvailabilityValidator {
        +ValidationResult Validate(User, EventBase, ReservationRequest)
    }
    
    class UserInformationValidator {
        +ValidationResult Validate(User, EventBase, ReservationRequest)
    }
    
    class BookingService {
        -EventReservationService _reservationService
        -IEnumerable~IBookingValidator~ _bookingValidators
        
        +Booking CreateBooking(User, EventBase, ReservationRequest)
        +ValidationResult ValidateBooking(User, EventBase, ReservationRequest)
    }
    
    class ValidationResult {
        <<value object>>
        +bool IsValid
        +string ErrorMessage
        +static ValidationResult Success()
        +static ValidationResult Failure(string)
    }
    
    IBookingValidator <|.. UserBookingLimitValidator : implements
    IBookingValidator <|.. EventAvailabilityValidator : implements
    IBookingValidator <|.. UserInformationValidator : implements
    
    BookingService --> IBookingValidator : uses collection of
    UserBookingLimitValidator ..> ValidationResult : returns
    EventAvailabilityValidator ..> ValidationResult : returns
    UserInformationValidator ..> ValidationResult : returns
    
    note for IBookingValidator "SRP: Each validator has\none responsibility.\nOCP: Add validators without\nmodifying existing code."
    
    note for BookingService "Composite pattern:\nIterates through validators\nand fails fast."
```

### Validator Workflow

```mermaid
flowchart TD
    Start([CreateBooking Called]) --> V1{UserBookingLimitValidator}
    
    V1 -->|Invalid| Fail1[Return Failure]
    V1 -->|Valid| V2{EventAvailabilityValidator}
    
    V2 -->|Invalid| Fail2[Return Failure]
    V2 -->|Valid| V3{UserInformationValidator}
    
    V3 -->|Invalid| Fail3[Return Failure]
    V3 -->|Valid| Reserve[Reserve via Strategy]
    
    Reserve --> Create[Create Booking Entity]
    Create --> Success([Return Booking])
    
    style Start fill:#e1f5ff
    style Success fill:#c8e6c9
    style Fail1 fill:#ffcdd2
    style Fail2 fill:#ffcdd2
    style Fail3 fill:#ffcdd2
    style V1 fill:#fff9c4
    style V2 fill:#fff9c4
    style V3 fill:#fff9c4
```

---

## Repository Pattern Architecture

### Repository Interfaces and Implementations

```mermaid
classDiagram
    class IRepository~TEntity~ {
        <<interface>>
        +Task~TEntity~ AddAsync(TEntity, CancellationToken)
        +Task~TEntity?~ GetByIdAsync(int, CancellationToken)
        +Task~TEntity~ UpdateAsync(TEntity, CancellationToken)
        +Task~bool~ DeleteAsync(int, CancellationToken)
    }
    
    class IEventRepository {
        <<interface>>
        +Task~EventBase?~ GetByIdWithDetailsAsync(int, CancellationToken)
        +Task~IEnumerable~EventBase~~ GetByVenueIdAsync(int, CancellationToken)
        +Task~IEnumerable~EventBase~~ GetByDateRangeAsync(DateTime, DateTime, CancellationToken)
    }
    
    class IBookingRepository {
        <<interface>>
        +Task~IEnumerable~Booking~~ GetByUserIdAsync(int, CancellationToken)
        +Task~IEnumerable~Booking~~ GetByEventIdAsync(int, CancellationToken)
    }
    
    class IUserRepository {
        <<interface>>
        +Task~User?~ GetByEmailAsync(string, CancellationToken)
    }
    
    class IVenueRepository {
        <<interface>>
        +Task~Venue?~ GetByIdWithDetailsAsync(int, CancellationToken)
    }
    
    class DapperEventRepository {
        -IDBConnectionFactory _connectionFactory
        +Task~EventBase~ AddAsync(EventBase, CancellationToken)
        +Task~EventBase?~ GetByIdAsync(int, CancellationToken)
        +Task~EventBase?~ GetByIdWithDetailsAsync(int, CancellationToken)
        +Task~IEnumerable~EventBase~~ GetByVenueIdAsync(int, CancellationToken)
        +Task~IEnumerable~EventBase~~ GetByDateRangeAsync(DateTime, DateTime, CancellationToken)
        -Task SaveRelatedDataAsync(connection, transaction, EventBase)
    }
    
    class DapperBookingRepository {
        -IDBConnectionFactory _connectionFactory
        +Task~Booking~ AddAsync(Booking, CancellationToken)
        +Task~Booking?~ GetByIdAsync(int, CancellationToken)
        +Task~IEnumerable~Booking~~ GetByUserIdAsync(int, CancellationToken)
        +Task~IEnumerable~Booking~~ GetByEventIdAsync(int, CancellationToken)
    }
    
    class DapperUserRepository {
        -IDBConnectionFactory _connectionFactory
        +Task~User~ AddAsync(User, CancellationToken)
        +Task~User?~ GetByIdAsync(int, CancellationToken)
        +Task~User?~ GetByEmailAsync(string, CancellationToken)
    }
    
    class DapperVenueRepository {
        -IDBConnectionFactory _connectionFactory
        +Task~Venue~ AddAsync(Venue, CancellationToken)
        +Task~Venue?~ GetByIdAsync(int, CancellationToken)
        +Task~Venue?~ GetByIdWithDetailsAsync(int, CancellationToken)
    }
    
    class IDBConnectionFactory {
        <<interface>>
        +Task~IDbConnection~ CreateConnectionAsync(CancellationToken)
    }
    
    IRepository~TEntity~ <|-- IEventRepository : extends
    IRepository~TEntity~ <|-- IBookingRepository : extends
    IRepository~TEntity~ <|-- IUserRepository : extends
    IRepository~TEntity~ <|-- IVenueRepository : extends
    
    IEventRepository <|.. DapperEventRepository : implements
    IBookingRepository <|.. DapperBookingRepository : implements
    IUserRepository <|.. DapperUserRepository : implements
    IVenueRepository <|.. DapperVenueRepository : implements
    
    DapperEventRepository --> IDBConnectionFactory : uses
    DapperBookingRepository --> IDBConnectionFactory : uses
    DapperUserRepository --> IDBConnectionFactory : uses
    DapperVenueRepository --> IDBConnectionFactory : uses
    
    note for IRepository~TEntity~ "Base repository interface.\nISP: Minimal core operations."
    
    note for DapperEventRepository "DIP: Depends on abstraction\n(IDBConnectionFactory)\nnot concrete implementation."
```

### Repository Data Flow

```mermaid
sequenceDiagram
    participant App as Application Service
    participant Repo as Repository Interface
    participant Impl as Dapper Repository
    participant Factory as Connection Factory
    participant Mapper as Entity Mapper
    participant DB as SQLite Database
    
    App->>Repo: GetByIdAsync(id)
    Repo->>Impl: GetByIdAsync(id)
    Impl->>Factory: CreateConnectionAsync()
    Factory-->>Impl: IDbConnection
    
    Impl->>DB: Execute SQL Query
    DB-->>Impl: DTO Result
    
    Impl->>Mapper: ToDomain(dto)
    Mapper-->>Impl: Domain Entity
    
    Impl-->>Repo: Domain Entity
    Repo-->>App: Domain Entity
    
    note over Mapper: Mapper translates between<br/>database DTOs and<br/>domain entities
```

---

## Application Service Flow

### Booking Application Service Architecture

```mermaid
classDiagram
    class BookingApplicationService {
        -IBookingRepository _bookingRepository
        -IEventRepository _eventRepository
        -IUserRepository _userRepository
        -IVenueRepository _venueRepository
        -IBookingService _bookingService
        -IPaymentService _paymentService
        
        +Task~BookingResultDto~ CreateBookingAsync(CreateBookingCommand, CancellationToken)
        -Task~ValidationResult~ ValidateCommandAsync(CreateBookingCommand)
        -Task~EntityLoadResult~ LoadRequiredEntitiesAsync(CreateBookingCommand)
        -ReservationRequest BuildReservationRequest(CreateBookingCommand)
        -Task~PaymentResult~ ProcessPaymentAsync(Booking, EventBase)
        -Task~PersistenceResult~ PersistBookingAsync(Booking, EventBase)
        -BookingResultDto MapToResultDto(Booking)
    }
    
    class CreateBookingCommand {
        +int UserId
        +int EventId
        +int Quantity
        +int? SectionId
        +int? SeatId
    }
    
    class BookingResultDto {
        +int BookingId
        +string UserName
        +string EventName
        +string BookingType
        +decimal TotalAmount
        +string PaymentStatus
        +DateTime CreatedAt
    }
    
    class IBookingService {
        <<interface>>
        +Booking CreateBooking(User, EventBase, ReservationRequest)
        +ValidationResult ValidateBooking(User, EventBase, ReservationRequest)
    }
    
    class IPaymentService {
        <<interface>>
        +Task~PaymentResult~ ProcessPaymentAsync(PaymentRequest, CancellationToken)
    }
    
    class IBookingRepository {
        <<interface>>
        +Task~Booking~ AddAsync(Booking, CancellationToken)
    }
    
    class IEventRepository {
        <<interface>>
        +Task~EventBase?~ GetByIdWithDetailsAsync(int, CancellationToken)
        +Task~EventBase~ UpdateAsync(EventBase, CancellationToken)
    }
    
    class IUserRepository {
        <<interface>>
        +Task~User?~ GetByIdAsync(int, CancellationToken)
    }
    
    class IVenueRepository {
        <<interface>>
        +Task~Venue?~ GetByIdWithDetailsAsync(int, CancellationToken)
    }
    
    BookingApplicationService --> CreateBookingCommand : receives
    BookingApplicationService --> BookingResultDto : returns
    BookingApplicationService --> IBookingService : uses
    BookingApplicationService --> IPaymentService : uses
    BookingApplicationService --> IBookingRepository : uses
    BookingApplicationService --> IEventRepository : uses
    BookingApplicationService --> IUserRepository : uses
    BookingApplicationService --> IVenueRepository : uses
    
    note for BookingApplicationService "SRP: Each private method\nhas single responsibility.\nDIP: All dependencies\nare abstractions."
```

### Booking Creation Workflow

```mermaid
flowchart TD
    Start([CreateBookingAsync]) --> ValidateCmd{Validate Command}
    
    ValidateCmd -->|Invalid| ReturnError1[Return Error]
    ValidateCmd -->|Valid| LoadEntities[Load User, Event, Venue]
    
    LoadEntities --> CheckEntities{All Entities Found?}
    CheckEntities -->|No| ReturnError2[Return Error]
    CheckEntities -->|Yes| ValidateDomain{Domain Validation}
    
    ValidateDomain -->|Invalid| ReturnError3[Return Error]
    ValidateDomain -->|Valid| CreateBooking[BookingService.CreateBooking]
    
    CreateBooking --> ProcessPayment{Process Payment}
    ProcessPayment -->|Failed| ReturnError4[Return Error]
    ProcessPayment -->|Success| SaveBooking[Persist Booking]
    
    SaveBooking --> UpdateEvent[Update Event State]
    UpdateEvent --> MapResult[Map to DTO]
    MapResult --> Success([Return BookingResultDto])
    
    style Start fill:#e1f5ff
    style Success fill:#c8e6c9
    style ReturnError1 fill:#ffcdd2
    style ReturnError2 fill:#ffcdd2
    style ReturnError3 fill:#ffcdd2
    style ReturnError4 fill:#ffcdd2
    style ValidateCmd fill:#fff9c4
    style CheckEntities fill:#fff9c4
    style ValidateDomain fill:#fff9c4
    style ProcessPayment fill:#fff9c4
```

---

## Mapper Pattern

### Entity-DTO Mapping Architecture

```mermaid
classDiagram
    class EventMapper {
        <<static>>
        +EventDto ToDto(EventBase)
        +EventBase ToDomain(EventDto)
    }
    
    class BookingMapper {
        <<static>>
        +BookingDto ToDto(Booking)
        +Booking ToDomain(BookingDto)
    }
    
    class VenueMapper {
        <<static>>
        +VenueDto ToDto(Venue)
        +Venue ToDomain(VenueDto)
    }
    
    class UserMapper {
        <<static>>
        +UserDto ToDto(User)
        +User ToDomain(UserDto)
    }
    
    class EventSeatMapper {
        <<static>>
        +EventSeatDto ToDto(EventSeat)
        +EventSeat ToDomain(EventSeatDto)
    }
    
    class EventSectionInventoryMapper {
        <<static>>
        +EventSectionInventoryDto ToDto(EventSectionInventory)
        +EventSectionInventory ToDomain(EventSectionInventoryDto)
    }
    
    class VenueSectionMapper {
        <<static>>
        +VenueSectionDto ToDto(VenueSection)
        +VenueSection ToDomain(VenueSectionDto)
    }
    
    class EventBase {
        <<domain>>
    }
    
    class EventDto {
        <<database>>
        +string EventType
        +int? GA_Capacity
        +int? GA_Attendees
        +decimal? GA_Price
        +int? SB_CapacityOverride
    }
    
    class Booking {
        <<domain>>
    }
    
    class BookingDto {
        <<database>>
    }
    
    EventMapper ..> EventBase : maps
    EventMapper ..> EventDto : maps
    BookingMapper ..> Booking : maps
    BookingMapper ..> BookingDto : maps
    
    note for EventMapper "Handles TPH pattern.\nMaps discriminator to\nappropriate subtype."
    
    note for EventDto "Contains all fields for\nall event types.\nUsed for database I/O."
```

### Mapper Usage in Repository

```mermaid
sequenceDiagram
    participant Repo as Repository
    participant Mapper as EventMapper
    participant Domain as Domain Entity
    participant DTO as Database DTO
    participant DB as Database
    
    Note over Repo,DB: Save Flow
    Repo->>Mapper: ToDto(domainEntity)
    Mapper->>DTO: Create DTO
    DTO-->>Mapper: EventDto
    Mapper-->>Repo: EventDto
    Repo->>DB: Insert DTO
    
    Note over Repo,DB: Load Flow
    Repo->>DB: Query
    DB-->>Repo: EventDto
    Repo->>Mapper: ToDomain(dto)
    Mapper->>Domain: Create Domain Entity
    Domain-->>Mapper: EventBase
    Mapper-->>Repo: EventBase
```

---

## Dependency Injection Container

### Service Registration

```mermaid
graph TB
    subgraph "Composition Root (Startup/Program.cs)"
        DI[Dependency Injection Container]
    end
    
    subgraph "Domain Services"
        BS[IBookingService ? BookingService]
        ERS[EventReservationService]
        VAL1[IBookingValidator ? UserBookingLimitValidator]
        VAL2[IBookingValidator ? EventAvailabilityValidator]
        VAL3[IBookingValidator ? UserInformationValidator]
        STRAT1[GeneralAdmissionReservationStrategy]
        STRAT2[SectionBasedReservationStrategy]
        STRAT3[ReservedSeatingReservationStrategy]
    end
    
    subgraph "Application Services"
        BAS[BookingApplicationService]
        EQS[IEventQueryService ? EventQueryService]
        BQS[IBookingQueryService ? BookingQueryService]
        PAY[IPaymentService ? SimulatedPaymentService]
    end
    
    subgraph "Infrastructure Services"
        ER[IEventRepository ? DapperEventRepository]
        BR[IBookingRepository ? DapperBookingRepository]
        UR[IUserRepository ? DapperUserRepository]
        VR[IVenueRepository ? DapperVenueRepository]
        CF[IDBConnectionFactory ? SqliteConnectionFactory]
    end
    
    DI --> BS
    DI --> ERS
    DI --> VAL1
    DI --> VAL2
    DI --> VAL3
    DI --> STRAT1
    DI --> STRAT2
    DI --> STRAT3
    DI --> BAS
    DI --> EQS
    DI --> BQS
    DI --> PAY
    DI --> ER
    DI --> BR
    DI --> UR
    DI --> VR
    DI --> CF
    
    style DI fill:#e1f5ff
    style BS fill:#c8e6c9
    style ERS fill:#c8e6c9
    style VAL1 fill:#a5d6a7
    style VAL2 fill:#a5d6a7
    style VAL3 fill:#a5d6a7
    style BAS fill:#fff9c4
    style EQS fill:#fff59d
    style BQS fill:#fff59d
    style ER fill:#b3e5fc
    style BR fill:#b3e5fc
    style UR fill:#b3e5fc
    style VR fill:#b3e5fc
```

### Dependency Resolution Example

```mermaid
graph TD
    Request[HTTP Request] --> BAS[BookingApplicationService]
    
    BAS --> BR[IBookingRepository]
    BAS --> ER[IEventRepository]
    BAS --> UR[IUserRepository]
    BAS --> VR[IVenueRepository]
    BAS --> BS[IBookingService]
    BAS --> PAY[IPaymentService]
    
    BS --> ERS[EventReservationService]
    BS --> VAL[IEnumerable&lt;IBookingValidator&gt;]
    
    VAL --> VAL1[UserBookingLimitValidator]
    VAL --> VAL2[EventAvailabilityValidator]
    VAL --> VAL3[UserInformationValidator]
    
    ERS --> STRAT1[GeneralAdmissionReservationStrategy]
    ERS --> STRAT2[SectionBasedReservationStrategy]
    ERS --> STRAT3[ReservedSeatingReservationStrategy]
    
    BR --> CF[IDBConnectionFactory]
    ER --> CF
    UR --> CF
    VR --> CF
    
    style Request fill:#e1f5ff
    style BAS fill:#fff9c4
    style BS fill:#c8e6c9
    style CF fill:#b3e5fc
```

---

## Request Flow Sequence Diagrams

### Complete Booking Flow

```mermaid
sequenceDiagram
    actor User
    participant API as API Controller
    participant AppSvc as BookingApplicationService
    participant DomainSvc as BookingService
    participant Validators as Validators (Collection)
    participant StratSvc as EventReservationService
    participant Strategy as Reservation Strategy
    participant PaySvc as Payment Service
    participant Repo as Repositories
    participant DB as Database
    
    User->>API: POST /bookings
    API->>AppSvc: CreateBookingAsync(command)
    
    Note over AppSvc: 1. Validate Command
    AppSvc->>AppSvc: ValidateCommandAsync()
    
    Note over AppSvc: 2. Load Entities
    AppSvc->>Repo: GetUserAsync(userId)
    Repo->>DB: Query Users
    DB-->>Repo: User DTO
    Repo-->>AppSvc: User Entity
    
    AppSvc->>Repo: GetEventAsync(eventId)
    Repo->>DB: Query Events + Details
    DB-->>Repo: Event DTO
    Repo-->>AppSvc: Event Entity
    
    AppSvc->>Repo: GetVenueAsync(venueId)
    Repo->>DB: Query Venues + Sections
    DB-->>Repo: Venue DTO
    Repo-->>AppSvc: Venue Entity
    
    Note over AppSvc: 3. Domain Validation & Booking
    AppSvc->>DomainSvc: CreateBooking(user, event, request)
    
    DomainSvc->>Validators: Validate(user, event, request)
    loop Each Validator
        Validators->>Validators: Check Rule
        alt Validation Fails
            Validators-->>DomainSvc: ValidationResult.Failure
            DomainSvc-->>AppSvc: throws InvalidOperationException
        end
    end
    Validators-->>DomainSvc: ValidationResult.Success
    
    DomainSvc->>StratSvc: Reserve(venue, event, request)
    StratSvc->>Strategy: ValidateReservation()
    Strategy-->>StratSvc: ValidationResult
    StratSvc->>Strategy: Reserve()
    Strategy->>Strategy: Update Event State
    Strategy-->>StratSvc: void
    StratSvc-->>DomainSvc: void
    
    DomainSvc->>DomainSvc: Create Booking Entity
    DomainSvc-->>AppSvc: Booking
    
    Note over AppSvc: 4. Process Payment
    AppSvc->>PaySvc: ProcessPaymentAsync(paymentRequest)
    PaySvc->>PaySvc: Simulate Payment
    PaySvc-->>AppSvc: PaymentResult
    
    alt Payment Failed
        AppSvc-->>API: Error Response
        API-->>User: 400 Bad Request
    end
    
    Note over AppSvc: 5. Persist Data
    AppSvc->>Repo: AddBookingAsync(booking)
    Repo->>DB: Insert Booking + Items
    DB-->>Repo: Booking ID
    Repo-->>AppSvc: Booking
    
    AppSvc->>Repo: UpdateEventAsync(event)
    Repo->>DB: Update Event State
    DB-->>Repo: Success
    Repo-->>AppSvc: Event
    
    Note over AppSvc: 6. Map to DTO
    AppSvc->>AppSvc: MapToResultDto(booking)
    AppSvc-->>API: BookingResultDto
    
    API-->>User: 201 Created + Booking Details
```

### Query Service Flow

```mermaid
sequenceDiagram
    actor User
    participant API as API Controller
    participant QuerySvc as EventQueryService
    participant Repo as EventRepository
    participant Mapper as EventMapper
    participant DB as Database
    
    User->>API: GET /events/future
    API->>QuerySvc: GetFutureEventsWithAvailabilityAsync()
    
    QuerySvc->>Repo: GetByDateRangeAsync(DateTime.UtcNow, ...)
    Repo->>DB: SELECT * FROM Events WHERE StartsAt > ?
    DB-->>Repo: List<EventDto>
    
    loop Each Event DTO
        Repo->>Mapper: ToDomain(eventDto)
        Mapper->>Mapper: Determine Event Type
        Mapper->>Mapper: Create Appropriate Subclass
        Mapper-->>Repo: EventBase (GA/SB/RS)
        
        alt Has EventSeats
            Repo->>DB: SELECT * FROM EventSeats WHERE EventId = ?
            DB-->>Repo: List<EventSeatDto>
        end
        
        alt Has SectionInventories
            Repo->>DB: SELECT * FROM EventSectionInventories WHERE EventId = ?
            DB-->>Repo: List<EventSectionInventoryDto>
        end
    end
    
    Repo-->>QuerySvc: List<EventBase>
    
    QuerySvc->>QuerySvc: Calculate Availability
    QuerySvc->>QuerySvc: Map to EventAvailabilityDto
    
    QuerySvc-->>API: List<EventAvailabilityDto>
    API-->>User: 200 OK + Event List with Availability
```

---

## Design Pattern Summary

### Patterns Used in the System

```mermaid
mindmap
  root((Design Patterns))
    Architectural
      Clean Architecture
      Layered Architecture
      Dependency Inversion
    Creational
      Factory Method
      Builder::TestDataBuilder
    Structural
      Repository
      Mapper
      Composite::Validators
    Behavioral
      Strategy::Reservation
      Template Method
      Chain of Responsibility::Validators
    Domain
      Entity
      Value Object::ValidationResult
      Aggregate Root::EventBase
      Domain Service
```

---

## Key Architectural Benefits

### SOLID Principles Visualization

```mermaid
graph TB
    subgraph "Single Responsibility"
        S1[Each validator has one job]
        S2[Services focus on one concern]
        S3[Mappers only handle translation]
    end
    
    subgraph "Open/Closed"
        O1[Add validators without changes]
        O2[Extend event types via inheritance]
        O3[New strategies without modification]
    end
    
    subgraph "Liskov Substitution"
        L1[All EventBase subtypes substitutable]
        L2[All validators interchangeable]
        L3[All repositories swappable]
    end
    
    subgraph "Interface Segregation"
        I1[Small focused interfaces]
        I2[IBookingValidator: 1 method]
        I3[Repository specialization]
    end
    
    subgraph "Dependency Inversion"
        D1[Depend on abstractions]
        D2[Constructor injection]
        D3[No new in business logic]
    end
    
    style S1 fill:#c8e6c9
    style S2 fill:#c8e6c9
    style S3 fill:#c8e6c9
    style O1 fill:#fff9c4
    style O2 fill:#fff9c4
    style O3 fill:#fff9c4
    style L1 fill:#b3e5fc
    style L2 fill:#b3e5fc
    style L3 fill:#b3e5fc
    style I1 fill:#ffccbc
    style I2 fill:#ffccbc
    style I3 fill:#ffccbc
    style D1 fill:#d1c4e9
    style D2 fill:#d1c4e9
    style D3 fill:#d1c4e9
```

---

## Related Documentation

- ?? **[SOLID-Principles-Overview.md](./SOLID-Principles-Overview.md)** - SOLID principles explained
- ?? **[DesignPrinciples.md](./DesignPrinciples.md)** - Comprehensive design guide
- ?? **[DatabaseSchema-Diagram.md](./DatabaseSchema-Diagram.md)** - Database structure
- ?? **[EventQueryServiceDocumentation.md](./EventQueryServiceDocumentation.md)** - Query service details
- ?? **[BookingQueryServiceDocumentation.md](./BookingQueryServiceDocumentation.md)** - Booking queries

---

**Document Version:** 1.0  
**Last Updated:** 2025-01-XX  
**Target Framework:** .NET 10, C# 14.0  
**Architecture:** Clean Architecture with SOLID Principles
