# Quick Reference: Capacity Management

## Entity Hierarchy

```
Venue
?? VenueSections[]
?  ?? VenueSeats[]
?? Events[]
   ?? EventSectionInventories[]
   ?  ?? BookingItems[]
   ?? EventSeats[] (for reserved seating only)
```

## Calculated Properties

| Entity | Property | Calculation |
|--------|----------|-------------|
| `VenueSection` | `Capacity` | `VenueSeats.Count` |
| `Venue` | `MaxCapacity` | `VenueSections.Sum(s => s.Capacity)` |
| `Venue` | `TotalSeats` | `VenueSections.Sum(s => s.VenueSeats.Count)` |
| `Event` | `TotalCapacity` | Based on EventType (see below) |
| `Event` | `TotalReserved` | Based on EventType (see below) |
| `Event` | `AvailableCapacity` | `TotalCapacity - TotalReserved` |
| `Event` | `IsSoldOut` | `AvailableCapacity <= 0` |
| `EventSectionInventory` | `Remaining` | `Capacity - Booked` |
| `EventSectionInventory` | `IsSoldOut` | `Remaining <= 0` |

## Event Capacity Logic

### TotalCapacity
```
IF EventType == Reserved AND EventSeats.Any()
    RETURN EventSeats.Count
ELSE IF SectionInventories.Any()
    RETURN SectionInventories.Sum(si => si.Capacity)
ELSE
    RETURN CapacityOverride ?? Venue.MaxCapacity ?? 0
```

### TotalReserved
```
IF EventType == Reserved AND EventSeats.Any()
    RETURN EventSeats.Count(s => s.Status == Reserved)
ELSE IF SectionInventories.Any()
    RETURN SectionInventories.Sum(si => si.Booked)
ELSE
    RETURN EventSeats.Count(s => s.Status == Reserved) ?? 0
```

## Common Operations

### Create Venue with Capacity
```csharp
var venue = TestDataBuilder.CreateVenueWithCapacity(
    name: "Arena",
    address: "123 Main St",
    capacity: 1000
);
// Creates venue with 1 section containing 1000 seats
```

### Create Venue with Multiple Sections
```csharp
var venue = TestDataBuilder.CreateVenueWithSections(
    name: "Theatre",
    address: "456 Broadway",
    ("Orchestra", 500),
    ("Balcony", 300),
    ("VIP", 100)
);
// Total capacity: 900 seats
```

### Create General Admission Event
```csharp
var evnt = TestDataBuilder.CreateEventWithSectionInventories(
    venue: venue,
    eventName: "Concert",
    eventType: EventType.GeneralAdmission,
    startsAt: DateTime.Now.AddDays(1)
);
// Creates EventSectionInventory for each venue section
```

### Create Reserved Seating Event
```csharp
var evnt = TestDataBuilder.CreateEventWithReservedSeating(
    venue: venue,
    eventName: "Play",
    startsAt: DateTime.Now.AddDays(1)
);
// Creates EventSeats for all venue seats
```

### Reserve Section Seats (General Admission)
```csharp
// Get section inventory
var section = evnt.GetSectionInventory(sectionId: 1);

// Validate first
var validation = section.ValidateReservation(quantity: 5);
if (validation.IsValid)
{
    // Reserve seats
    section.ReserveSeats(5);
    // OR use event method
    evnt.ReserveSectionSeats(sectionId: 1, quantity: 5);
}
```

### Reserve Specific Seat (Reserved Seating)
```csharp
// Find specific seat
var seat = evnt.EventSeats
    .FirstOrDefault(s => s.VenueSeatId == seatId);

// Check availability
if (seat.IsAvailable())
{
    seat.Reserve();
}
```

### Check Event Capacity
```csharp
// Check if event can accommodate reservation
var validation = evnt.ValidateCapacity(quantity: 10);
if (validation.IsValid)
{
    // Proceed with reservation
}
else
{
    Console.WriteLine(validation.ErrorMessage);
}

// Quick checks
if (evnt.IsSoldOut) { /* Handle sold out */ }
if (evnt.AvailableCapacity >= requestedQuantity) { /* Can reserve */ }
```

### Release Seats
```csharp
// For section inventory
var section = evnt.GetSectionInventory(sectionId: 1);
section.ReleaseSeats(quantity: 2);

// For reserved seats
var seat = evnt.EventSeats.First(s => s.VenueSeatId == seatId);
if (seat.Status == SeatStatus.Locked)
{
    seat.Release(); // Returns to Available
}
```

## Event Types and Strategies

| EventType | Strategy | Uses |
|-----------|----------|------|
| `GeneralAdmission` | `GeneralAdmissionStrategy` | `EventSectionInventory` |
| `Reserved` | `ReservedSeatingStrategy` | `EventSeat` |
| `Mixed` | Custom (not implemented) | Both |

## Seat Statuses

| Status | Description | Can Reserve? |
|--------|-------------|--------------|
| `Available` | Seat is available | ? Yes |
| `Locked` | Temporarily held (e.g., during checkout) | ? No |
| `Reserved` | Permanently reserved | ? No |

## Validation Methods

### EventSectionInventory.ValidateReservation(quantity)
```csharp
var result = inventory.ValidateReservation(10);
// Returns ValidationResult with IsValid and ErrorMessage
```

**Checks:**
- Quantity is positive
- Section not sold out
- Sufficient remaining capacity

### Event.ValidateCapacity(quantity)
```csharp
var result = evnt.ValidateCapacity(10);
```

**Checks:**
- Quantity is positive
- Event not sold out
- Sufficient available capacity

## Pricing

### Section-Level Pricing
```csharp
var inventory = new EventSectionInventory
{
    VenueSectionId = 1,
    Capacity = 500,
    Price = 75.00m  // Per ticket
};
```

### Calculate Total Price
```csharp
var quantity = 5;
var section = evnt.GetSectionInventory(sectionId: 1);
var totalPrice = section.Price * quantity;
// Example: $75.00 * 5 = $375.00
```

## Allocation Modes

| Mode | Description | Seat Assignment |
|------|-------------|-----------------|
| `GeneralAdmission` | First-come, first-served | None - open seating |
| `Reserved` | Specific seats | Required - exact seat |
| `BestAvailable` | System picks best | Automatic - best available |

### Set Allocation Mode
```csharp
var inventory = new EventSectionInventory
{
    VenueSectionId = 1,
    Capacity = 200,
    AllocationMode = SeatAllocationMode.Reserved
};
```

## Common Scenarios

### Scenario 1: Concert with Price Tiers
```csharp
var evnt = new Event
{
    Name = "Rock Concert",
    EventType = EventType.GeneralAdmission,
    SectionInventories = new List<EventSectionInventory>
    {
        new() { VenueSectionId = 1, Capacity = 500, Price = 100m },  // Floor
        new() { VenueSectionId = 2, Capacity = 300, Price = 75m },   // Balcony
        new() { VenueSectionId = 3, Capacity = 200, Price = 50m }    // Upper
    }
};
```

### Scenario 2: Theatre with Reserved Seating
```csharp
var evnt = TestDataBuilder.CreateEventWithReservedSeating(
    venue, "Hamilton", DateTime.Now.AddMonths(1)
);

// Reserve specific seat
var seat = evnt.EventSeats.First(s => 
    s.VenueSeat.Row == "A" && 
    s.VenueSeat.SeatNumber == "5"
);
seat.Reserve();
```

### Scenario 3: Event with Reduced Capacity
```csharp
var evnt = new Event
{
    Name = "Corporate Event",
    Venue = venue,  // Normally holds 1000
    CapacityOverride = 600,  // Reduced for stage setup
    EventType = EventType.GeneralAdmission,
    SectionInventories = new List<EventSectionInventory>
    {
        new() { Capacity = 600 }
    }
};
// TotalCapacity will be 600, not 1000
```

## Error Messages

| Error | Cause | Solution |
|-------|-------|----------|
| "Quantity must be positive" | quantity <= 0 | Use positive number |
| "Section '[Name]' is sold out" | Remaining <= 0 | Try different section |
| "Insufficient capacity" | Remaining < quantity | Reduce quantity |
| "Event '[Name]' is sold out" | AvailableCapacity <= 0 | Event is full |
| "Seat ID is required" | No seatId for reserved | Provide seatId |
| "Seat not available" | Status != Available | Seat already taken |

## Best Practices

### ? DO
- Always validate before reserving
- Use TestDataBuilder in tests
- Set prices at section level for tiered pricing
- Use CapacityOverride for temporary capacity changes
- Create EventSectionInventory for general admission
- Create EventSeats for reserved seating

### ? DON'T
- Don't try to set Venue.MaxCapacity (it's calculated)
- Don't bypass validation methods
- Don't mix EventSeats with general admission
- Don't reserve without checking availability
- Don't forget to handle sold out scenarios

## Testing

### Unit Test Template
```csharp
[TestMethod]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var venue = TestDataBuilder.CreateVenueWithCapacity("Test", "Address", 100);
    var evnt = TestDataBuilder.CreateEventWithSectionInventories(
        venue, "Event", EventType.GeneralAdmission, DateTime.Now.AddDays(1)
    );
    
    // Act
    var result = evnt.ValidateCapacity(10);
    
    // Assert
    result.IsValid.Should().BeTrue();
}
```

## Quick Diagnostics

### Check Event Capacity
```csharp
Console.WriteLine($"Total Capacity: {evnt.TotalCapacity}");
Console.WriteLine($"Total Reserved: {evnt.TotalReserved}");
Console.WriteLine($"Available: {evnt.AvailableCapacity}");
Console.WriteLine($"Sold Out: {evnt.IsSoldOut}");
```

### Check Section Status
```csharp
foreach (var section in evnt.SectionInventories)
{
    Console.WriteLine($"Section {section.VenueSectionId}:");
    Console.WriteLine($"  Capacity: {section.Capacity}");
    Console.WriteLine($"  Booked: {section.Booked}");
    Console.WriteLine($"  Remaining: {section.Remaining}");
    Console.WriteLine($"  Sold Out: {section.IsSoldOut}");
    Console.WriteLine($"  Price: {section.Price:C}");
}
```

### Check Seat Availability
```csharp
var availableCount = evnt.EventSeats.Count(s => s.IsAvailable());
var reservedCount = evnt.EventSeats.Count(s => s.Status == SeatStatus.Reserved);
var lockedCount = evnt.EventSeats.Count(s => s.Status == SeatStatus.Locked);

Console.WriteLine($"Available: {availableCount}");
Console.WriteLine($"Reserved: {reservedCount}");
Console.WriteLine($"Locked: {lockedCount}");
```
