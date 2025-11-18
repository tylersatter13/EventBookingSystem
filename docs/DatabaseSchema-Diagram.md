# Event Booking System - Database Schema Diagram

## Entity Relationship Diagram

```mermaid
erDiagram
    %% =============================================
    %% VENUE STRUCTURE
    %% =============================================
    
    Venues ||--o{ VenueSections : "has sections"
    Venues ||--o{ Events : "hosts events"
    
    VenueSections ||--o{ VenueSeats : "contains seats"
    VenueSections ||--o{ EventSectionInventories : "referenced by"
    
    VenueSeats ||--o{ EventSeats : "used in events"
    
    %% =============================================
    %% EVENT STRUCTURE
    %% =============================================
    
    Events ||--o{ EventSectionInventories : "has section inventories"
    Events ||--o{ EventSeats : "has seat reservations"
    Events ||--o{ Bookings : "receives bookings"
    
    %% =============================================
    %% BOOKING STRUCTURE
    %% =============================================
    
    Users ||--o{ Bookings : "makes bookings"
    
    Bookings ||--o{ BookingItems : "contains items"
    
    BookingItems }o--|| EventSeats : "references (reserved seating)"
    BookingItems }o--|| EventSectionInventories : "references (section-based)"
    
    %% =============================================
    %% ENTITY DEFINITIONS
    %% =============================================
    
    Venues {
        INTEGER Id PK "Auto-increment"
        TEXT Name "Venue name"
        TEXT Address "Physical address"
    }
    
    VenueSections {
        INTEGER Id PK "Auto-increment"
        INTEGER VenueId FK "References Venues"
        TEXT Name "Section name (Orchestra, Balcony, etc.)"
    }
    
    VenueSeats {
        INTEGER Id PK "Auto-increment"
        INTEGER VenueSectionId FK "References VenueSections"
        TEXT Row "Seat row (A, B, C, etc.)"
        TEXT SeatNumber "Seat number within row"
        TEXT SeatLabel "Display label (A1, B12, etc.)"
    }
    
    Users {
        INTEGER Id PK "Auto-increment"
        TEXT Name "User full name"
        TEXT Email UK "Unique email address"
        TEXT PhoneNumber "Contact number"
    }
    
    Events {
        INTEGER Id PK "Auto-increment"
        INTEGER VenueId FK "References Venues"
        TEXT Name "Event name"
        TEXT StartsAt "ISO 8601 datetime"
        TEXT EndsAt "ISO 8601 datetime (optional)"
        INTEGER EstimatedAttendance "Expected attendance"
        TEXT EventType "Discriminator: GeneralAdmission | SectionBased | ReservedSeating"
        INTEGER GA_Capacity "General Admission: total capacity"
        INTEGER GA_Attendees "General Admission: current bookings"
        REAL GA_Price "General Admission: ticket price"
        INTEGER GA_CapacityOverride "General Admission: override venue capacity"
        INTEGER SB_CapacityOverride "Section-Based: override total capacity"
    }
    
    EventSectionInventories {
        INTEGER Id PK "Auto-increment"
        INTEGER EventId FK "References Events"
        INTEGER VenueSectionId FK "References VenueSections"
        INTEGER Capacity "Section capacity for this event"
        INTEGER Booked "Currently reserved count"
        REAL Price "Ticket price for this section"
        TEXT AllocationMode "GeneralAdmission | Reserved | BestAvailable"
    }
    
    EventSeats {
        INTEGER Id PK "Auto-increment"
        INTEGER EventId FK "References Events"
        INTEGER VenueSeatId FK "References VenueSeats"
        TEXT Status "Available | Reserved | Locked"
    }
    
    Bookings {
        INTEGER Id PK "Auto-increment"
        INTEGER UserId FK "References Users"
        INTEGER EventId FK "References Events"
        TEXT BookingType "Seat | Section | GA"
        TEXT PaymentStatus "Pending | Paid | Refunded | Failed"
        REAL TotalAmount "Total booking cost"
        TEXT CreatedAt "ISO 8601 datetime"
    }
    
    BookingItems {
        INTEGER Id PK "Auto-increment"
        INTEGER BookingId FK "References Bookings"
        INTEGER EventSeatId FK "References EventSeats (nullable)"
        INTEGER EventSectionInventoryId FK "References EventSectionInventories (nullable)"
        INTEGER Quantity "Number of tickets (default 1)"
    }
```

---

## Database Architecture Overview

### Design Pattern: Table Per Hierarchy (TPH)

The **Events** table uses a discriminator pattern to store three event types in a single table:

```mermaid
graph TB
    subgraph "Events Table - TPH Pattern"
        E[Events Table]
        E --> GA[General Admission<br/>EventType='GeneralAdmission']
        E --> SB[Section-Based<br/>EventType='SectionBased']
        E --> RS[Reserved Seating<br/>EventType='ReservedSeating']
        
        GA --> GAF["Uses: GA_Capacity<br/>GA_Attendees<br/>GA_Price<br/>GA_CapacityOverride"]
        SB --> SBF["Uses: SB_CapacityOverride<br/>+ EventSectionInventories"]
        RS --> RSF["Uses: EventSeats"]
    end
    
    style E fill:#e1f5ff
    style GA fill:#c8e6c9
    style SB fill:#fff9c4
    style RS fill:#ffccbc
```

---

## Data Flow Diagrams

### Flow 1: General Admission Booking

```mermaid
flowchart LR
    U[User] -->|Books tickets| B[Booking]
    B -->|Contains| BI[BookingItem]
    BI -->|Quantity: 3| E[Event<br/>GeneralAdmission]
    E -->|Increments| GA[GA_Attendees]
    E -->|Hosted at| V[Venue]
    
    style E fill:#c8e6c9
```

### Flow 2: Section-Based Booking

```mermaid
flowchart LR
    U[User] -->|Books tickets| B[Booking]
    B -->|Contains| BI[BookingItem]
    BI -->|Quantity: 2| ESI[EventSectionInventory<br/>Orchestra: $100]
    ESI -->|Increments Booked| COUNT[Booked += 2]
    ESI -->|For event| E[Event<br/>SectionBased]
    ESI -->|References| VS[VenueSection<br/>Orchestra]
    E -->|Hosted at| V[Venue]
    
    style E fill:#fff9c4
    style ESI fill:#ffe0b2
```

### Flow 3: Reserved Seating Booking

```mermaid
flowchart LR
    U[User] -->|Books seats| B[Booking]
    B -->|Contains| BI1[BookingItem 1]
    B -->|Contains| BI2[BookingItem 2]
    BI1 -->|References| ES1[EventSeat A1<br/>Status: Reserved]
    BI2 -->|References| ES2[EventSeat A2<br/>Status: Reserved]
    ES1 -->|For event| E[Event<br/>ReservedSeating]
    ES2 -->|For event| E
    ES1 -->|Maps to| VSEAT1[VenueSeat A1]
    ES2 -->|Maps to| VSEAT2[VenueSeat A2]
    E -->|Hosted at| V[Venue]
    
    style E fill:#ffccbc
    style ES1 fill:#ef9a9a
    style ES2 fill:#ef9a9a
```

---

## Venue Hierarchy

```mermaid
graph TB
    V[Venue<br/>Madison Square Garden]
    V --> S1[VenueSection<br/>Orchestra]
    V --> S2[VenueSection<br/>Mezzanine]
    V --> S3[VenueSection<br/>Balcony]
    
    S1 --> SEAT1[VenueSeat<br/>Row A, Seat 1]
    S1 --> SEAT2[VenueSeat<br/>Row A, Seat 2]
    S1 --> SEAT3[VenueSeat<br/>Row B, Seat 1]
    
    S2 --> SEAT4[VenueSeat<br/>Row C, Seat 1]
    S2 --> SEAT5[VenueSeat<br/>Row C, Seat 2]
    
    S3 --> SEAT6[VenueSeat<br/>Row D, Seat 1]
    S3 --> SEAT7[VenueSeat<br/>Row D, Seat 2]
    
    style V fill:#e1f5ff
    style S1 fill:#fff9c4
    style S2 fill:#fff9c4
    style S3 fill:#fff9c4
```

---

## Booking Item Relationships

### Mutual Exclusivity Pattern

```mermaid
graph TB
    BI[BookingItem]
    
    BI -->|XOR| CHOICE{Booking Type?}
    
    CHOICE -->|General Admission| GA[EventSeatId = NULL<br/>EventSectionInventoryId = NULL<br/>Quantity = 3]
    CHOICE -->|Section-Based| SECT[EventSeatId = NULL<br/>EventSectionInventoryId = 5<br/>Quantity = 2]
    CHOICE -->|Reserved Seating| SEAT[EventSeatId = 100<br/>EventSectionInventoryId = NULL<br/>Quantity = 1]
    
    style BI fill:#e1f5ff
    style GA fill:#c8e6c9
    style SECT fill:#fff9c4
    style SEAT fill:#ffccbc
```

**Database Constraint**:
```sql
CHECK(
  (EventSeatId IS NOT NULL AND EventSectionInventoryId IS NULL) OR 
  (EventSeatId IS NULL AND EventSectionInventoryId IS NOT NULL) OR
  (EventSeatId IS NULL AND EventSectionInventoryId IS NULL)
)
```

---

## Index Strategy

```mermaid
graph LR
    subgraph "Query Patterns"
        Q1[Find events at venue]
        Q2[Find available seats]
        Q3[Find user bookings]
        Q4[Find events by date]
        Q5[Section inventory lookup]
    end
    
    subgraph "Indexes"
        I1[idx_events_venue_id]
        I2[idx_event_seats_status]
        I3[idx_bookings_user_id]
        I4[idx_events_starts_at]
        I5[idx_event_section_inventories_event_id]
    end
    
    Q1 --> I1
    Q2 --> I2
    Q3 --> I3
    Q4 --> I4
    Q5 --> I5
    
    style I1 fill:#b3e5fc
    style I2 fill:#b3e5fc
    style I3 fill:#b3e5fc
    style I4 fill:#b3e5fc
    style I5 fill:#b3e5fc
```

---

## Cascade Delete Strategy

```mermaid
graph TB
    V[Venue Deleted]
    V -->|CASCADE| VS[VenueSections Deleted]
    V -->|CASCADE| E[Events Deleted]
    
    VS -->|CASCADE| VSEATS[VenueSeats Deleted]
    
    E -->|CASCADE| ESI[EventSectionInventories Deleted]
    E -->|CASCADE| ESEATS[EventSeats Deleted]
    E -->|CASCADE| B[Bookings Deleted]
    
    B -->|CASCADE| BI[BookingItems Deleted]
    
    style V fill:#ffcdd2
    style VS fill:#f8bbd0
    style E fill:#f8bbd0
    style VSEATS fill:#f48fb1
    style ESI fill:#f48fb1
    style ESEATS fill:#f48fb1
    style B fill:#f48fb1
    style BI fill:#f48fb1
```

**Key Cascade Rules**:
- Deleting a **Venue** removes all sections, seats, events, and bookings
- Deleting an **Event** removes all inventories, seat reservations, and bookings
- Deleting a **Booking** removes all booking items
- Deleting a **User** does NOT cascade (must handle bookings separately)

---

## Event Type Comparison

| Feature | General Admission | Section-Based | Reserved Seating |
|---------|-------------------|---------------|------------------|
| **Table Used** | Events only | Events + EventSectionInventories | Events + EventSeats |
| **Capacity Management** | Simple counter | Per-section counters | Per-seat status |
| **Pricing** | Single price | Per-section pricing | Can vary by seat/section |
| **BookingItem References** | None (quantity only) | EventSectionInventoryId | EventSeatId |
| **Use Case** | Festivals, standing room | Theaters with tiered pricing | Concerts, sports with assigned seats |

---

## Schema Statistics

### Table Sizes (Typical Production)

```mermaid
pie title "Relative Record Counts"
    "VenueSeats (10,000)" : 40
    "EventSeats (8,000)" : 32
    "BookingItems (3,000)" : 12
    "Bookings (1,500)" : 6
    "Events (500)" : 2
    "EventSectionInventories (200)" : 1
    "Users (5,000)" : 20
    "Venues (50)" : 0.5
    "VenueSections (150)" : 0.5
```

---

## Normalization Level

The schema achieves **Third Normal Form (3NF)**:

? **1NF**: All attributes are atomic (no repeating groups)  
? **2NF**: All non-key attributes depend on the entire primary key  
? **3NF**: No transitive dependencies (no derived/calculated data stored)

**Key Design Decisions**:
- Capacity is **calculated**, not stored (from seat counts)
- Booked counts are stored for performance (denormalized for query optimization)
- TPH pattern trades some duplication for query simplicity

---

## Related Documentation

- ?? **[CompleteSchema.sql](../tests/EventBookingSystem.Infrastructure.Tests/TestData/CompleteSchema.sql)** - Full DDL script
- ?? **[SOLID-Principles-Overview.md](./SOLID-Principles-Overview.md)** - Design principles applied
- ?? **[DesignPrinciples.md](./DesignPrinciples.md)** - Comprehensive architectural guide

---

**Document Version:** 1.0  
**Last Updated:** 2025-01-XX  
**Database:** SQLite  
**Schema Version:** 1.0  
