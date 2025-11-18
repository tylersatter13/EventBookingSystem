# Event Booking System - Database Relationships Guide

## Visual Guide to Table Relationships

This document provides detailed visualizations of all relationships in the Event Booking System database schema, highlighting cardinality, cascade behaviors, and constraint patterns.

---

## Table of Contents

1. [Complete Relationship Overview](#complete-relationship-overview)
2. [Venue Hierarchy Relationships](#venue-hierarchy-relationships)
3. [Event Type Relationships](#event-type-relationships)
4. [Booking Relationships](#booking-relationships)
5. [Polymorphic Relationships](#polymorphic-relationships)
6. [Cascade Delete Chain](#cascade-delete-chain)
7. [Constraint Patterns](#constraint-patterns)
8. [Relationship Summary Table](#relationship-summary-table)

---

## Complete Relationship Overview

### Master Entity Relationship Diagram

```mermaid
erDiagram
    %% =============================================
    %% VENUE HIERARCHY (Physical Infrastructure)
    %% =============================================
    
    Venues ||--o{ VenueSections : "has"
    Venues {
        int Id PK
        string Name
        string Address
    }
    
    VenueSections ||--o{ VenueSeats : "contains"
    VenueSections {
        int Id PK
        int VenueId FK "CASCADE DELETE"
        string Name
    }
    
    VenueSeats {
        int Id PK
        int VenueSectionId FK "CASCADE DELETE"
        string Row
        string SeatNumber
        string SeatLabel
    }
    
    %% =============================================
    %% EVENT STRUCTURE (Event Configuration)
    %% =============================================
    
    Venues ||--o{ Events : "hosts"
    Events {
        int Id PK
        int VenueId FK "CASCADE DELETE"
        string Name
        datetime StartsAt
        datetime EndsAt
        string EventType "DISCRIMINATOR"
        int GA_Capacity "GeneralAdmission"
        int GA_Attendees "GeneralAdmission"
        int SB_CapacityOverride "SectionBased"
    }
    
    Events ||--o{ EventSectionInventories : "configures sections"
    VenueSections ||--o{ EventSectionInventories : "referenced by"
    EventSectionInventories {
        int Id PK
        int EventId FK "CASCADE DELETE"
        int VenueSectionId FK
        int Capacity
        int Booked
        decimal Price
    }
    
    Events ||--o{ EventSeats : "has seat status"
    VenueSeats ||--o{ EventSeats : "used in events"
    EventSeats {
        int Id PK
        int EventId FK "CASCADE DELETE"
        int VenueSeatId FK
        string Status "Available|Reserved|Locked"
    }
    
    %% =============================================
    %% BOOKING STRUCTURE (Customer Transactions)
    %% =============================================
    
    Users ||--o{ Bookings : "makes"
    Users {
        int Id PK
        string Name
        string Email UK
        string PhoneNumber
    }
    
    Events ||--o{ Bookings : "receives"
    Bookings {
        int Id PK
        int UserId FK
        int EventId FK "CASCADE DELETE"
        string BookingType "GA|Section|Seat"
        string PaymentStatus
        decimal TotalAmount
        datetime CreatedAt
    }
    
    Bookings ||--o{ BookingItems : "contains"
    BookingItems {
        int Id PK
        int BookingId FK "CASCADE DELETE"
        int EventSeatId FK "NULLABLE"
        int EventSectionInventoryId FK "NULLABLE"
        int Quantity
    }
    
    BookingItems }o--|| EventSeats : "references (XOR)"
    BookingItems }o--|| EventSectionInventories : "references (XOR)"
```

---

## Venue Hierarchy Relationships

### Physical Infrastructure Chain

```mermaid
graph TB
    subgraph "Physical Venue Structure"
        V[Venue<br/>Madison Square Garden]
        
        VS1[VenueSection<br/>Orchestra<br/>VenueId=1]
        VS2[VenueSection<br/>Mezzanine<br/>VenueId=1]
        VS3[VenueSection<br/>Balcony<br/>VenueId=1]
        
        VSEAT1[VenueSeat<br/>Row A, Seat 1<br/>VenueSectionId=1]
        VSEAT2[VenueSeat<br/>Row A, Seat 2<br/>VenueSectionId=1]
        VSEAT3[VenueSeat<br/>Row B, Seat 1<br/>VenueSectionId=1]
        
        VSEAT4[VenueSeat<br/>Row C, Seat 1<br/>VenueSectionId=2]
        VSEAT5[VenueSeat<br/>Row C, Seat 2<br/>VenueSectionId=2]
    end
    
    V -->|"1:M<br/>CASCADE DELETE"| VS1
    V -->|"1:M<br/>CASCADE DELETE"| VS2
    V -->|"1:M<br/>CASCADE DELETE"| VS3
    
    VS1 -->|"1:M<br/>CASCADE DELETE"| VSEAT1
    VS1 -->|"1:M<br/>CASCADE DELETE"| VSEAT2
    VS1 -->|"1:M<br/>CASCADE DELETE"| VSEAT3
    
    VS2 -->|"1:M<br/>CASCADE DELETE"| VSEAT4
    VS2 -->|"1:M<br/>CASCADE DELETE"| VSEAT5
    
    style V fill:#e1f5ff
    style VS1 fill:#fff9c4
    style VS2 fill:#fff9c4
    style VS3 fill:#fff9c4
    style VSEAT1 fill:#c8e6c9
    style VSEAT2 fill:#c8e6c9
    style VSEAT3 fill:#c8e6c9
    style VSEAT4 fill:#c8e6c9
    style VSEAT5 fill:#c8e6c9
```

**Relationship Details:**

| Parent | Child | Type | Cardinality | Delete Behavior | Constraint |
|--------|-------|------|-------------|-----------------|------------|
| **Venues** | **VenueSections** | Composition | 1:Many | CASCADE | Required (NOT NULL) |
| **VenueSections** | **VenueSeats** | Composition | 1:Many | CASCADE | Required (NOT NULL) + UNIQUE(VenueSectionId, Row, SeatNumber) |

**Key Points:**
- ? **Composition**: Child cannot exist without parent
- ? **Cascade Delete**: Deleting venue removes all sections and seats
- ? **Unique Constraint**: No duplicate seats in same section (Row + SeatNumber)

---

## Event Type Relationships

### Event Configuration Patterns

```mermaid
graph TB
    subgraph "Event Base (TPH Pattern)"
        E[Events Table<br/>EventType Discriminator]
    end
    
    subgraph "General Admission Event"
        GA[Event Record<br/>EventType='GeneralAdmission'<br/>VenueId=1]
        GA_FIELDS[Uses: GA_Capacity<br/>GA_Attendees<br/>GA_Price]
    end
    
    subgraph "Section-Based Event"
        SB[Event Record<br/>EventType='SectionBased'<br/>VenueId=1]
        ESI1[EventSectionInventory<br/>EventId=2, VenueSectionId=1<br/>Capacity=100, Booked=45]
        ESI2[EventSectionInventory<br/>EventId=2, VenueSectionId=2<br/>Capacity=200, Booked=180]
    end
    
    subgraph "Reserved Seating Event"
        RS[Event Record<br/>EventType='ReservedSeating'<br/>VenueId=1]
        ES1[EventSeat<br/>EventId=3, VenueSeatId=1<br/>Status='Reserved']
        ES2[EventSeat<br/>EventId=3, VenueSeatId=2<br/>Status='Available']
        ES3[EventSeat<br/>EventId=3, VenueSeatId=3<br/>Status='Locked']
    end
    
    E -.discriminates.-> GA
    E -.discriminates.-> SB
    E -.discriminates.-> RS
    
    GA --> GA_FIELDS
    
    SB -->|"1:M<br/>CASCADE DELETE"| ESI1
    SB -->|"1:M<br/>CASCADE DELETE"| ESI2
    
    RS -->|"1:M<br/>CASCADE DELETE"| ES1
    RS -->|"1:M<br/>CASCADE DELETE"| ES2
    RS -->|"1:M<br/>CASCADE DELETE"| ES3
    
    ESI1 -.references.-> VS1[VenueSection: Orchestra]
    ESI2 -.references.-> VS2[VenueSection: Mezzanine]
    
    ES1 -.references.-> VSEAT1[VenueSeat: A1]
    ES2 -.references.-> VSEAT2[VenueSeat: A2]
    ES3 -.references.-> VSEAT3[VenueSeat: A3]
    
    style E fill:#e1f5ff
    style GA fill:#c8e6c9
    style SB fill:#fff9c4
    style RS fill:#ffccbc
    style ESI1 fill:#fff59d
    style ESI2 fill:#fff59d
    style ES1 fill:#ffab91
    style ES2 fill:#ffab91
    style ES3 fill:#ffab91
```

**Relationship Details:**

#### Events ? Venues
| Parent | Child | Type | Cardinality | Delete Behavior |
|--------|-------|------|-------------|-----------------|
| **Venues** | **Events** | Association | 1:Many | CASCADE |

#### Events ? EventSectionInventories (Section-Based Events)
| Parent | Child | Type | Cardinality | Delete Behavior | Constraint |
|--------|-------|------|-------------|-----------------|------------|
| **Events** | **EventSectionInventories** | Composition | 1:Many | CASCADE | UNIQUE(EventId, VenueSectionId) |
| **VenueSections** | **EventSectionInventories** | Reference | 1:Many | RESTRICT | Required (NOT NULL) |

#### Events ? EventSeats (Reserved Seating Events)
| Parent | Child | Type | Cardinality | Delete Behavior | Constraint |
|--------|-------|------|-------------|-----------------|------------|
| **Events** | **EventSeats** | Composition | 1:Many | CASCADE | UNIQUE(EventId, VenueSeatId) |
| **VenueSeats** | **EventSeats** | Reference | 1:Many | RESTRICT | Required (NOT NULL) |

**Key Points:**
- ? **TPH Pattern**: All event types in one table with discriminator
- ? **Conditional Composition**: Only relevant child records exist per event type
- ? **Reference Integrity**: Event-specific records reference physical venue structure
- ? **Unique Constraints**: One inventory per section per event; one status per seat per event

---

## Booking Relationships

### Customer Transaction Structure

```mermaid
graph TB
    subgraph "User Domain"
        U[User<br/>Id=1<br/>john@email.com]
    end
    
    subgraph "Booking Domain"
        B1[Booking<br/>Id=101<br/>UserId=1<br/>EventId=1<br/>BookingType='GA']
        B2[Booking<br/>Id=102<br/>UserId=1<br/>EventId=2<br/>BookingType='Section']
        B3[Booking<br/>Id=103<br/>UserId=1<br/>EventId=3<br/>BookingType='Seat']
    end
    
    subgraph "General Admission Booking Items"
        BI1[BookingItem<br/>BookingId=101<br/>Quantity=3<br/>EventSeatId=NULL<br/>EventSectionInventoryId=NULL]
    end
    
    subgraph "Section-Based Booking Items"
        BI2[BookingItem<br/>BookingId=102<br/>Quantity=2<br/>EventSeatId=NULL<br/>EventSectionInventoryId=5]
    end
    
    subgraph "Reserved Seating Booking Items"
        BI3[BookingItem<br/>BookingId=103<br/>Quantity=1<br/>EventSeatId=10<br/>EventSectionInventoryId=NULL]
        BI4[BookingItem<br/>BookingId=103<br/>Quantity=1<br/>EventSeatId=11<br/>EventSectionInventoryId=NULL]
    end
    
    U -->|"1:M"| B1
    U -->|"1:M"| B2
    U -->|"1:M"| B3
    
    B1 -->|"1:M<br/>CASCADE DELETE"| BI1
    B2 -->|"1:M<br/>CASCADE DELETE"| BI2
    B3 -->|"1:M<br/>CASCADE DELETE"| BI3
    B3 -->|"1:M<br/>CASCADE DELETE"| BI4
    
    BI2 -.references.-> ESI[EventSectionInventory<br/>Id=5]
    BI3 -.references.-> ES10[EventSeat<br/>Id=10]
    BI4 -.references.-> ES11[EventSeat<br/>Id=11]
    
    B1 -.for.-> E1[Event: GA Concert]
    B2 -.for.-> E2[Event: Theater Show]
    B3 -.for.-> E3[Event: Broadway]
    
    style U fill:#e1f5ff
    style B1 fill:#c8e6c9
    style B2 fill:#fff9c4
    style B3 fill:#ffccbc
    style BI1 fill:#a5d6a7
    style BI2 fill:#fff59d
    style BI3 fill:#ffab91
    style BI4 fill:#ffab91
```

**Relationship Details:**

#### Users ? Bookings
| Parent | Child | Type | Cardinality | Delete Behavior |
|--------|-------|------|-------------|-----------------|
| **Users** | **Bookings** | Association | 1:Many | RESTRICT (Not specified, default) |

#### Events ? Bookings
| Parent | Child | Type | Cardinality | Delete Behavior |
|--------|-------|------|-------------|-----------------|
| **Events** | **Bookings** | Association | 1:Many | CASCADE |

#### Bookings ? BookingItems
| Parent | Child | Type | Cardinality | Delete Behavior |
|--------|-------|------|-------------|-----------------|
| **Bookings** | **BookingItems** | Composition | 1:Many | CASCADE |

#### BookingItems ? Event-Specific Records (Polymorphic)
| Parent | Child | Type | Cardinality | Constraint |
|--------|-------|------|-------------|------------|
| **EventSeats** | **BookingItems** | Reference (Optional) | 1:Many | XOR: Either EventSeatId OR EventSectionInventoryId |
| **EventSectionInventories** | **BookingItems** | Reference (Optional) | 1:Many | XOR: Either EventSeatId OR EventSectionInventoryId |

**Key Points:**
- ? **User Association**: Users keep booking history (no cascade delete)
- ? **Event Cascade**: Deleting event removes all bookings
- ? **Booking Composition**: Deleting booking removes all items
- ? **Polymorphic Reference**: BookingItems reference different tables based on booking type
- ? **XOR Constraint**: Items reference either seats OR sections OR neither (GA), never both

---

## Polymorphic Relationships

### BookingItems XOR Pattern

```mermaid
graph TB
    subgraph "Booking Item Polymorphism"
        BI[BookingItem]
        
        CHOICE{XOR Constraint}
        
        GA[General Admission<br/>EventSeatId = NULL<br/>EventSectionInventoryId = NULL<br/>Quantity = N]
        
        SECT[Section-Based<br/>EventSeatId = NULL<br/>EventSectionInventoryId = X<br/>Quantity = N]
        
        SEAT[Reserved Seating<br/>EventSeatId = Y<br/>EventSectionInventoryId = NULL<br/>Quantity = 1]
    end
    
    subgraph "Referenced Tables"
        ESI[EventSectionInventories<br/>Section capacity/pricing]
        ES[EventSeats<br/>Individual seat status]
    end
    
    BI --> CHOICE
    
    CHOICE -->|"Type 1"| GA
    CHOICE -->|"Type 2"| SECT
    CHOICE -->|"Type 3"| SEAT
    
    SECT -.M:1.-> ESI
    SEAT -.M:1.-> ES
    
    style BI fill:#e1f5ff
    style CHOICE fill:#fff9c4
    style GA fill:#c8e6c9
    style SECT fill:#fff59d
    style SEAT fill:#ffab91
    style ESI fill:#b3e5fc
    style ES fill:#b3e5fc
```

**XOR Constraint SQL:**
```sql
CHECK(
  (EventSeatId IS NOT NULL AND EventSectionInventoryId IS NULL) OR 
  (EventSeatId IS NULL AND EventSectionInventoryId IS NOT NULL) OR
  (EventSeatId IS NULL AND EventSectionInventoryId IS NULL)
)
```

**Three Valid States:**

| State | EventSeatId | EventSectionInventoryId | Quantity | Use Case |
|-------|-------------|-------------------------|----------|----------|
| **Type 1** | NULL | NULL | N | General Admission (no specific allocation) |
| **Type 2** | NULL | Not NULL | N | Section-Based (N tickets in section) |
| **Type 3** | Not NULL | NULL | 1 | Reserved Seating (specific seat) |

**Invalid State:**
| EventSeatId | EventSectionInventoryId | Result |
|-------------|-------------------------|--------|
| Not NULL | Not NULL | ? **CONSTRAINT VIOLATION** |

---

## Cascade Delete Chain

### Complete Deletion Cascade Flow

```mermaid
graph TB
    subgraph "Level 1: Root Entities"
        V[Venue<br/>DELETE]
        U[User<br/>DELETE]
    end
    
    subgraph "Level 2: Venue Children"
        VS[VenueSections<br/>CASCADE DELETE]
        E[Events<br/>CASCADE DELETE]
    end
    
    subgraph "Level 3: Deep Venue & Event Children"
        VSEAT[VenueSeats<br/>CASCADE DELETE]
        ESI[EventSectionInventories<br/>CASCADE DELETE]
        ES[EventSeats<br/>CASCADE DELETE]
        B[Bookings<br/>CASCADE DELETE from Event<br/>RESTRICT from User]
    end
    
    subgraph "Level 4: Booking Children"
        BI[BookingItems<br/>CASCADE DELETE]
    end
    
    V -->|CASCADE| VS
    V -->|CASCADE| E
    
    VS -->|CASCADE| VSEAT
    
    E -->|CASCADE| ESI
    E -->|CASCADE| ES
    E -->|CASCADE| B
    
    U -->|RESTRICT| B
    
    B -->|CASCADE| BI
    
    style V fill:#ffcdd2
    style U fill:#ffcdd2
    style VS fill:#ef9a9a
    style E fill:#ef9a9a
    style VSEAT fill:#e57373
    style ESI fill:#e57373
    style ES fill:#e57373
    style B fill:#e57373
    style BI fill:#f44336
```

**Cascade Delete Chains:**

#### Chain 1: Venue Deletion
```
Venue (DELETE)
  ??? VenueSections (CASCADE)
       ??? VenueSeats (CASCADE)
  ??? Events (CASCADE)
       ??? EventSectionInventories (CASCADE)
       ??? EventSeats (CASCADE)
       ??? Bookings (CASCADE)
            ??? BookingItems (CASCADE)
```

#### Chain 2: Event Deletion
```
Event (DELETE)
  ??? EventSectionInventories (CASCADE)
  ??? EventSeats (CASCADE)
  ??? Bookings (CASCADE)
       ??? BookingItems (CASCADE)
```

#### Chain 3: VenueSection Deletion
```
VenueSection (DELETE)
  ??? VenueSeats (CASCADE)
```

#### Chain 4: Booking Deletion
```
Booking (DELETE)
  ??? BookingItems (CASCADE)
```

#### No Cascade: User Deletion
```
User (DELETE)
  ??? Bookings (RESTRICT - must be handled separately)
```

**Important Notes:**
- ?? **Venue deletion is catastrophic** - removes all events and bookings
- ?? **Event deletion removes customer bookings** - may need soft delete strategy
- ?? **User deletion is protected** - must manually handle bookings first
- ? **BookingItems always cascade** with parent booking

---

## Constraint Patterns

### Unique Constraints

```mermaid
graph LR
    subgraph "Unique Constraints Enforced"
        UC1[Users.Email<br/>UNIQUE]
        UC2[VenueSeats<br/>UNIQUE VenueSectionId + Row + SeatNumber]
        UC3[EventSectionInventories<br/>UNIQUE EventId + VenueSectionId]
        UC4[EventSeats<br/>UNIQUE EventId + VenueSeatId]
    end
    
    UC1 -.prevents.-> DUP1[Duplicate email addresses]
    UC2 -.prevents.-> DUP2[Duplicate seats in section]
    UC3 -.prevents.-> DUP3[Duplicate section configs per event]
    UC4 -.prevents.-> DUP4[Duplicate seat status per event]
    
    style UC1 fill:#c8e6c9
    style UC2 fill:#c8e6c9
    style UC3 fill:#c8e6c9
    style UC4 fill:#c8e6c9
    style DUP1 fill:#ffcdd2
    style DUP2 fill:#ffcdd2
    style DUP3 fill:#ffcdd2
    style DUP4 fill:#ffcdd2
```

### Check Constraints

```mermaid
graph TB
    subgraph "Enumeration Constraints (CHECK)"
        CH1[Events.EventType<br/>CHECK IN GeneralAdmission, SectionBased, ReservedSeating]
        CH2[EventSeats.Status<br/>CHECK IN Available, Reserved, Locked]
        CH3[EventSectionInventories.AllocationMode<br/>CHECK IN GeneralAdmission, Reserved, BestAvailable]
        CH4[Bookings.BookingType<br/>CHECK IN Seat, Section, GA]
        CH5[Bookings.PaymentStatus<br/>CHECK IN Pending, Paid, Refunded, Failed]
        CH6[BookingItems XOR<br/>CHECK EventSeatId XOR EventSectionInventoryId]
    end
    
    CH1 -.validates.-> V1[Event type discriminator]
    CH2 -.validates.-> V2[Seat availability status]
    CH3 -.validates.-> V3[Section allocation strategy]
    CH4 -.validates.-> V4[Booking type classification]
    CH5 -.validates.-> V5[Payment lifecycle status]
    CH6 -.validates.-> V6[Polymorphic reference integrity]
    
    style CH1 fill:#fff9c4
    style CH2 fill:#fff9c4
    style CH3 fill:#fff9c4
    style CH4 fill:#fff9c4
    style CH5 fill:#fff9c4
    style CH6 fill:#ffccbc
    style V1 fill:#e1f5ff
    style V2 fill:#e1f5ff
    style V3 fill:#e1f5ff
    style V4 fill:#e1f5ff
    style V5 fill:#e1f5ff
    style V6 fill:#ffab91
```

---

## Relationship Summary Table

### Complete Relationship Matrix

| Relationship | Parent Table | Child Table | Cardinality | Delete Behavior | Constraint Type | Notes |
|--------------|--------------|-------------|-------------|-----------------|-----------------|-------|
| **Venue Hierarchy** |
| 1 | **Venues** | **VenueSections** | 1:Many | CASCADE | FK, NOT NULL | Venue owns sections |
| 2 | **VenueSections** | **VenueSeats** | 1:Many | CASCADE | FK, NOT NULL, UNIQUE(SectionId, Row, Number) | Section owns seats |
| **Event Structure** |
| 3 | **Venues** | **Events** | 1:Many | CASCADE | FK, NOT NULL | Venue hosts events |
| 4 | **Events** | **EventSectionInventories** | 1:Many | CASCADE | FK, NOT NULL, UNIQUE(EventId, SectionId) | Event configures sections |
| 5 | **VenueSections** | **EventSectionInventories** | 1:Many | RESTRICT | FK, NOT NULL | References physical section |
| 6 | **Events** | **EventSeats** | 1:Many | CASCADE | FK, NOT NULL, UNIQUE(EventId, VenueSeatId) | Event manages seat status |
| 7 | **VenueSeats** | **EventSeats** | 1:Many | RESTRICT | FK, NOT NULL | References physical seat |
| **Booking Structure** |
| 8 | **Users** | **Bookings** | 1:Many | RESTRICT | FK, NOT NULL | User makes bookings |
| 9 | **Events** | **Bookings** | 1:Many | CASCADE | FK, NOT NULL | Event receives bookings |
| 10 | **Bookings** | **BookingItems** | 1:Many | CASCADE | FK, NOT NULL | Booking contains items |
| 11 | **EventSeats** | **BookingItems** | 1:Many | RESTRICT | FK, NULLABLE, XOR | Optional seat reference |
| 12 | **EventSectionInventories** | **BookingItems** | 1:Many | RESTRICT | FK, NULLABLE, XOR | Optional section reference |

### Relationship Type Distribution

```mermaid
pie title "Relationship Types"
    "Composition (Cascade)" : 7
    "Association (Restrict)" : 5
```

### Delete Behavior Summary

```mermaid
pie title "Delete Behaviors"
    "CASCADE (Parent owns child)" : 7
    "RESTRICT (Reference only)" : 5
```

---

## Relationship Navigation Paths

### Common Query Patterns

#### Pattern 1: Find All Seats in a Venue
```
Venues ? VenueSections ? VenueSeats
```

#### Pattern 2: Find Event Availability (Section-Based)
```
Events ? EventSectionInventories ? VenueSections ? VenueSeats
```

#### Pattern 3: Find Event Availability (Reserved Seating)
```
Events ? EventSeats ? VenueSeats ? VenueSections ? Venues
```

#### Pattern 4: Find User's Bookings with Details
```
Users ? Bookings ? Events ? Venues
Users ? Bookings ? BookingItems ? [EventSeats OR EventSectionInventories]
```

#### Pattern 5: Find All Bookings for an Event
```
Events ? Bookings ? BookingItems
BookingItems ? EventSeats (if reserved seating)
BookingItems ? EventSectionInventories (if section-based)
```

---

## Visual Relationship Legend

### Cardinality Notation

```mermaid
graph LR
    A[Parent] -->|"1:1<br/>One-to-One"| B[Child]
    C[Parent] -->|"1:M<br/>One-to-Many"| D[Child]
    E[Parent] -->|"M:M<br/>Many-to-Many"| F[Child]
    G[Parent] -.->|"0..1<br/>Optional"| H[Child]
```

### Delete Behavior Notation

```mermaid
graph TB
    P1[Parent] -->|CASCADE<br/>Delete children| C1[Child]
    P2[Parent] -->|RESTRICT<br/>Prevent if children exist| C2[Child]
    P3[Parent] -->|SET NULL<br/>Nullify foreign keys| C3[Child]
    
    style C1 fill:#ffcdd2
    style C2 fill:#fff9c4
    style C3 fill:#e1f5ff
```

### Constraint Notation

| Symbol | Meaning |
|--------|---------|
| **PK** | Primary Key |
| **FK** | Foreign Key |
| **UK** | Unique Key |
| **NOT NULL** | Required Field |
| **NULLABLE** | Optional Field |
| **CHECK** | Value Constraint |
| **XOR** | Mutual Exclusivity |

---

## Key Relationship Insights

### Design Principles

1. **Composition vs Association**
   - **Composition** (CASCADE): Child cannot exist without parent (VenueSections, EventSeats, BookingItems)
   - **Association** (RESTRICT): Child can reference parent but has independent lifecycle (EventSectionInventories ? VenueSections)

2. **Physical vs Event-Specific**
   - **Physical**: VenueSeats, VenueSections (permanent venue infrastructure)
   - **Event-Specific**: EventSeats, EventSectionInventories (temporary event configuration)

3. **Polymorphic References**
   - **BookingItems** uses XOR constraint to reference different tables based on booking type
   - Enables flexibility while maintaining referential integrity

4. **Cascade Boundaries**
   - **Venue/Event hierarchy**: Aggressive cascade (administrative entities)
   - **User/Booking**: Restricted cascade (customer data protection)

5. **Unique Constraints**
   - Prevent duplicate physical infrastructure (seats, sections)
   - Prevent duplicate event configurations (one status per seat per event)
   - Enforce business rules at database level

---

## Related Documentation

- ?? **[DatabaseSchema-Diagram.md](./DatabaseSchema-Diagram.md)** - Complete schema visualization
- ?? **[CodeArchitecture-Diagrams.md](./CodeArchitecture-Diagrams.md)** - Code structure diagrams
- ?? **[SOLID-Principles-Overview.md](./SOLID-Principles-Overview.md)** - Design principles
- ?? **[CompleteSchema.sql](../tests/EventBookingSystem.Infrastructure.Tests/TestData/CompleteSchema.sql)** - DDL script

---

**Document Version:** 1.0  
**Last Updated:** 2025-01-XX  
**Database:** SQLite  
**Total Relationships:** 12 foreign key relationships  
**Cascade Chains:** 4 major deletion cascades  
