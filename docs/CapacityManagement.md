# Event and Venue Seat Capacity Management

## Overview

This document describes the improved capacity management system for the Event Booking System. The new architecture follows Clean Architecture and SOLID principles, providing a flexible and maintainable approach to managing venue seats and event capacity.

## Architecture Patterns

### 1. **Inventory-Based Pattern**

The system uses an **inventory-based pattern** where event-specific capacity is tracked separately from the venue's physical layout.

```
Venue (Physical layout)
  ?? VenueSection (Physical sections like Orchestra, Balcony)
      ?? VenueSeat (Physical seats with Row/Number)

Event (Instance of an event)
  ?? EventSectionInventory (Available capacity per section for THIS event)
  ?   ?? BookingItem (Individual reservations)
  ?? EventSeat (Seat reservations for reserved seating events)
```

### 2. **Calculated Properties**

Capacity is **calculated dynamically** from actual seats rather than stored redundantly:

- **Venue.MaxCapacity**: Calculated from the sum of all section capacities
- **VenueSection.Capacity**: Calculated from the count of VenueSeats
- **Event.TotalCapacity**: Calculated based on event type and configuration
- **Event.TotalReserved**: Calculated from EventSectionInventory or EventSeats

## Key Entities

### VenueSection

Represents a physical section within a venue.

```csharp
public class VenueSection
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public required string Name { get; set; }
    
    public List<VenueSeat> VenueSeats { get; set; } = new();
    
    // Calculated from actual seats
    public int Capacity => VenueSeats?.Count ?? 0;
}
```

**Key Points:**
- Capacity is **calculated**, not stored
- Contains the physical seats in the section
- Represents the venue's physical layout

### VenueSeat

Represents a physical seat within a venue section.

```csharp
public class VenueSeat
{
    public required int Id { get; set; }
    public int VenueSectionId { get; set; }
    public required string Row { get; set; }
    public required string SeatNumber { get; set; }
    public string? SeatLabel { get; set; }
    
    public VenueSection? Section { get; set; }
    public List<EventSeat>? EventSeats { get; set; }
}
```

**Key Points:**
- Belongs to ONE section (not directly to venue)
- Reusable across multiple events via EventSeat
- Contains location information (Row, SeatNumber)

### EventSectionInventory

Represents available inventory for a specific section of an event.

```csharp
public class EventSectionInventory
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int VenueSectionId { get; set; }
    
    public int Capacity { get; set; }
    public int Booked => _booked;
    public int Remaining => Capacity - _booked;
    public bool IsSoldOut => Remaining <= 0;
    
    public decimal? Price { get; set; }
    public SeatAllocationMode AllocationMode { get; set; }
    
    public void ReserveSeats(int quantity);
    public void ReleaseSeats(int quantity);
    public ValidationResult ValidateReservation(int quantity);
}
```

**Key Points:**
- **Event-specific capacity** - can differ from physical capacity
- Tracks bookings via `_booked` private field
- Supports section-level pricing
- Encapsulates capacity logic and validation
- Allows different allocation modes per section

**Use Cases:**
- General admission with multiple price tiers (Floor: $50, Balcony: $30)
- Events with stage setups that reduce available seats
- VIP sections with limited capacity
- Standing room areas with controlled capacity

### Event

Contains calculated capacity properties.

```csharp
public class Event
{
    public int Id { get; set; }
    public string Name { get; set; }
    public EventType EventType { get; set; }
    public int? CapacityOverride { get; set; }
    
    public List<EventSectionInventory> SectionInventories { get; set; } = new();
    public List<EventSeat> EventSeats { get; set; } = new();
    
    // Calculated properties
    public int TotalCapacity { get; }
    public int TotalReserved { get; }
    public int AvailableCapacity => TotalCapacity - TotalReserved;
    public bool IsSoldOut => AvailableCapacity <= 0;
    
    public ValidationResult ValidateCapacity(int quantity);
    public EventSectionInventory? GetSectionInventory(int sectionId);
    public void ReserveSectionSeats(int sectionId, int quantity);
}
```

**Capacity Calculation Logic:**
1. **Reserved Seating** (`EventType.Reserved`): Capacity = count of EventSeats
2. **With Section Inventories**: Capacity = sum of SectionInventory capacities
3. **Simple General Admission**: Capacity = CapacityOverride ?? Venue.MaxCapacity

### EventSeat

Represents the status of a specific seat for a specific event (for reserved seating).

```csharp
public class EventSeat
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int VenueSeatId { get; set; }
    public SeatStatus Status { get; set; }
    
    public bool IsAvailable() => Status == SeatStatus.Available;
    public void Reserve();
    public void Lock();
    public void Release();
}
```

**Key Points:**
- Links a VenueSeat to a specific Event
- Tracks status (Available, Reserved, Locked)
- Used for **reserved seating events only**

## Event Types and Allocation Modes

### EventType

```csharp
public enum EventType
{
    GeneralAdmission,  // No specific seat assignments
    Reserved,          // Specific seat assignments
    Mixed              // Combination of both
}
```

### SeatAllocationMode

```csharp
public enum SeatAllocationMode
{
    GeneralAdmission,  // First-come, no specific seats
    Reserved,          // Specific seat assignments
    BestAvailable      // System picks best seats
}
```

**Use Case Example:**
A concert event with:
- **Floor section**: GeneralAdmission mode (standing room, $50)
- **VIP Balcony**: Reserved mode (assigned seats, $100)
- **Upper Deck**: GeneralAdmission mode (general seating, $30)

## Usage Patterns

### Pattern 1: General Admission with Section Inventories

**Scenario:** Concert with different price sections but no assigned seats.

```csharp
var venue = new Venue
{
    Name = "Arena",
    Address = "123 Main St",
    VenueSections = new List<VenueSection>
    {
        new VenueSection { Name = "Floor", VenueSeats = CreateSeats(500) },
        new VenueSection { Name = "Balcony", VenueSeats = CreateSeats(300) }
    }
};

var concertEvent = new Event
{
    Name = "Rock Concert",
    EventType = EventType.GeneralAdmission,
    SectionInventories = new List<EventSectionInventory>
    {
        new EventSectionInventory 
        { 
            VenueSectionId = 1, 
            Capacity = 500,  // Full capacity
            Price = 75.00m,
            AllocationMode = SeatAllocationMode.GeneralAdmission
        },
        new EventSectionInventory 
        { 
            VenueSectionId = 2, 
            Capacity = 300,
            Price = 50.00m,
            AllocationMode = SeatAllocationMode.GeneralAdmission
        }
    }
};

// Reserve 2 tickets in Floor section
concertEvent.ReserveSectionSeats(sectionId: 1, quantity: 2);
```

### Pattern 2: Reserved Seating

**Scenario:** Theatre show with assigned seats.

```csharp
var venue = new Venue
{
    Name = "Theatre",
    Address = "456 Broadway",
    VenueSections = new List<VenueSection>
    {
        new VenueSection { Name = "Orchestra", VenueSeats = CreateSeats(200) }
    }
};

var theatreEvent = new Event
{
    Name = "Hamilton",
    EventType = EventType.Reserved,
    EventSeats = new List<EventSeat>()
};

// Create EventSeats for all venue seats
foreach (var venueSeat in venue.VenueSections[0].VenueSeats)
{
    theatreEvent.EventSeats.Add(new EventSeat
    {
        VenueSeatId = venueSeat.Id,
        Status = SeatStatus.Available
    });
}

// Reserve specific seat
var seat = theatreEvent.EventSeats.First(s => s.VenueSeatId == 42);
seat.Reserve();
```

### Pattern 3: Capacity Override

**Scenario:** Event with stage setup reducing available seats.

```csharp
var venue = new Venue
{
    Name = "Convention Center",
    VenueSections = new List<VenueSection>
    {
        new VenueSection { Name = "Main Hall", VenueSeats = CreateSeats(1000) }
    }
};
// Venue normally holds 1000 people

var eventWithStage = new Event
{
    Name = "Corporate Event",
    EventType = EventType.GeneralAdmission,
    Venue = venue,
    CapacityOverride = 600,  // Reduced capacity due to stage
    SectionInventories = new List<EventSectionInventory>
    {
        new EventSectionInventory { Capacity = 600 }
    }
};

// TotalCapacity will be 600, not 1000
```

## Seating Strategies

The system uses the **Strategy Pattern** to handle different event types.

### GeneralAdmissionStrategy

Handles events without specific seat assignments.

```csharp
public class GeneralAdmissionStrategy : ISeatingStrategy
{
    public bool CanHandle(EventType eventType) 
        => eventType == EventType.GeneralAdmission;
    
    public void Reserve(Venue venue, Event evnt, int? sectionId = null, int? seatId = null)
    {
        if (sectionId.HasValue)
        {
            evnt.ReserveSectionSeats(sectionId.Value, 1);
        }
        else
        {
            throw new InvalidOperationException(
                "General admission events must use EventSectionInventory.");
        }
    }
}
```

### ReservedSeatingStrategy

Handles events with specific seat assignments.

```csharp
public class ReservedSeatingStrategy : ISeatingStrategy
{
    public bool CanHandle(EventType eventType) 
        => eventType == EventType.Reserved;
    
    public void Reserve(Venue venue, Event evnt, int? sectionId = null, int? seatId = null)
    {
        if (!seatId.HasValue)
            throw new InvalidOperationException("Seat ID is required.");
        
        var eventSeat = evnt.EventSeats.First(s => s.VenueSeatId == seatId.Value);
        eventSeat.Reserve();
    }
}
```

## Benefits of This Architecture

### 1. **Flexibility**
- Events can override venue capacity
- Section-specific pricing and allocation modes
- Mix general admission and reserved seating

### 2. **Maintainability**
- Single source of truth (calculated from seats)
- No redundant capacity fields to sync
- Clear separation of concerns

### 3. **Testability**
- Easy to test capacity calculations
- No database required for unit tests
- Clear validation logic

### 4. **Scalability**
- Supports complex venue configurations
- Handles multiple event types
- Easy to add new allocation modes

### 5. **SOLID Compliance**
- **SRP**: Each class has one responsibility
- **OCP**: Extend via strategies, not modification
- **LSP**: Strategies are substitutable
- **ISP**: Focused interfaces (ISeatingStrategy)
- **DIP**: Depends on abstractions (interfaces)

## Migration from Old Model

### Old Model Issues

```csharp
// ? Old approach - redundant fields
public class Event
{
    public int SeatsReservered { get; set; }  // Manually tracked
    internal void BookSeat() { SeatsReservered++; }
}

public class Venue
{
    public int MaxCapacity { get; set; }  // Manually set
}
```

**Problems:**
- Manual synchronization required
- Capacity could be incorrect
- No section-level tracking
- No event-specific overrides

### New Model

```csharp
// ? New approach - calculated properties
public class Event
{
    public int TotalReserved => CalculateFromActualBookings();
    public int TotalCapacity => CalculateFromConfiguration();
}

public class Venue
{
    public int MaxCapacity => VenueSections.Sum(s => s.Capacity);
}
```

**Benefits:**
- Always accurate
- Automatically updated
- Section-level tracking
- Event-specific configuration

## Testing Helpers

The `TestDataBuilder` class simplifies test setup:

```csharp
// Create a venue with 100 seats
var venue = TestDataBuilder.CreateVenueWithCapacity("Arena", "123 Main St", 100);

// Create a venue with multiple sections
var venue = TestDataBuilder.CreateVenueWithSections(
    "Theatre", "456 Broadway",
    ("Orchestra", 200),
    ("Balcony", 100),
    ("VIP", 50)
);

// Create event with section inventories
var evnt = TestDataBuilder.CreateEventWithSectionInventories(
    venue, "Concert", EventType.GeneralAdmission, DateTime.Now.AddDays(1)
);

// Create event with reserved seating
var evnt = TestDataBuilder.CreateEventWithReservedSeating(
    venue, "Play", DateTime.Now.AddDays(1)
);
```

## Best Practices

### DO ?

1. **Use EventSectionInventory for general admission**
   ```csharp
   var inventory = new EventSectionInventory { Capacity = 500 };
   inventory.ReserveSeats(2);
   ```

2. **Use EventSeat for reserved seating**
   ```csharp
   var eventSeat = evnt.EventSeats.First(s => s.VenueSeatId == seatId);
   eventSeat.Reserve();
   ```

3. **Validate before reserving**
   ```csharp
   var validation = inventory.ValidateReservation(quantity);
   if (validation.IsValid)
       inventory.ReserveSeats(quantity);
   ```

4. **Use capacity override when needed**
   ```csharp
   evnt.CapacityOverride = 800;  // Stage setup reduces capacity
   ```

### DON'T ?

1. **Don't manually set Venue.MaxCapacity**
   ```csharp
   // ? Won't compile - it's calculated
   venue.MaxCapacity = 1000;
   ```

2. **Don't bypass validation**
   ```csharp
   // ? Check capacity first
   inventory.ReserveSeats(1000);  // Will throw if over capacity
   ```

3. **Don't mix allocation modes inappropriately**
   ```csharp
   // ? Don't use EventSeats for general admission
   // Use EventSectionInventory instead
   ```

## Conclusion

The new capacity management system provides a robust, flexible, and maintainable solution for handling venue seats and event capacity. By following Clean Architecture and SOLID principles, the system is easy to understand, test, and extend.

Key takeaways:
- **Calculated properties** eliminate redundancy
- **EventSectionInventory** provides event-specific flexibility
- **Strategy pattern** handles different event types
- **Encapsulated validation** ensures data integrity
- **Test helpers** simplify testing
