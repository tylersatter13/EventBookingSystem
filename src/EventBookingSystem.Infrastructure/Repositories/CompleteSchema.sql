-- =============================================
-- Event Booking System - Complete Database Schema
-- Description: Complete schema aligned with Clean Architecture domain model
-- =============================================

-- =============================================
-- Table: Venues
-- Description: Physical venues where events take place
-- =============================================
CREATE TABLE IF NOT EXISTS Venues (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Address TEXT NOT NULL
);

-- =============================================
-- Table: VenueSections
-- Description: Physical sections within a venue (e.g., Orchestra, Balcony, VIP)
-- Note: Capacity is calculated from VenueSeats count, not stored
-- =============================================
CREATE TABLE IF NOT EXISTS VenueSections (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VenueId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    FOREIGN KEY (VenueId) REFERENCES Venues(Id) ON DELETE CASCADE
);

-- =============================================
-- Table: VenueSeats
-- Description: Individual physical seats within venue sections
-- =============================================
CREATE TABLE IF NOT EXISTS VenueSeats (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VenueSectionId INTEGER NOT NULL,
    Row TEXT NOT NULL,
    SeatNumber TEXT NOT NULL,
    SeatLabel TEXT,
    FOREIGN KEY (VenueSectionId) REFERENCES VenueSections(Id) ON DELETE CASCADE,
    UNIQUE(VenueSectionId, Row, SeatNumber)
);

-- =============================================
-- Table: Users
-- Description: System users who make bookings
-- =============================================
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Email TEXT NOT NULL UNIQUE,
    PhoneNumber TEXT
);

-- =============================================
-- Table: Events (Base Table with Discriminator Pattern)
-- Description: All event types (GeneralAdmission, SectionBased, ReservedSeating)
-- Note: Uses Table Per Hierarchy (TPH) pattern with EventType discriminator
-- =============================================
CREATE TABLE IF NOT EXISTS Events (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VenueId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    StartsAt TEXT NOT NULL,  -- SQLite uses TEXT for DateTime (ISO 8601 format)
    EndsAt TEXT,
    EstimatedAttendance INTEGER NOT NULL DEFAULT 0,
    
    -- Discriminator column for event type hierarchy
    EventType TEXT NOT NULL CHECK(EventType IN ('GeneralAdmission', 'SectionBased', 'ReservedSeating')),
    
    -- GeneralAdmissionEvent specific fields
    GA_Capacity INTEGER,
    GA_Attendees INTEGER DEFAULT 0,
    GA_Price REAL,
    GA_CapacityOverride INTEGER,
    
    -- SectionBasedEvent specific fields
    SB_CapacityOverride INTEGER,
    
    FOREIGN KEY (VenueId) REFERENCES Venues(Id) ON DELETE CASCADE
);

-- =============================================
-- Table: EventSectionInventories
-- Description: Event-specific section capacity and pricing
-- Used by: SectionBasedEvent
-- =============================================
CREATE TABLE IF NOT EXISTS EventSectionInventories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventId INTEGER NOT NULL,
    VenueSectionId INTEGER NOT NULL,
    Capacity INTEGER NOT NULL,
    Booked INTEGER NOT NULL DEFAULT 0,
    Price REAL,
    AllocationMode TEXT NOT NULL DEFAULT 'GeneralAdmission' 
        CHECK(AllocationMode IN ('GeneralAdmission', 'Reserved', 'BestAvailable')),
    FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE,
    FOREIGN KEY (VenueSectionId) REFERENCES VenueSections(Id),
    UNIQUE(EventId, VenueSectionId)
);

-- =============================================
-- Table: EventSeats
-- Description: Seat reservations for specific events
-- Used by: ReservedSeatingEvent
-- =============================================
CREATE TABLE IF NOT EXISTS EventSeats (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventId INTEGER NOT NULL,
    VenueSeatId INTEGER NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Available' 
        CHECK(Status IN ('Available', 'Reserved', 'Locked')),
    FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE,
    FOREIGN KEY (VenueSeatId) REFERENCES VenueSeats(Id),
    UNIQUE(EventId, VenueSeatId)
);

-- =============================================
-- Table: Bookings
-- Description: Customer bookings/orders
-- =============================================
CREATE TABLE IF NOT EXISTS Bookings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    EventId INTEGER NOT NULL,
    BookingType TEXT NOT NULL CHECK(BookingType IN ('Seat', 'Section', 'GA')),
    PaymentStatus TEXT NOT NULL DEFAULT 'Pending' 
        CHECK(PaymentStatus IN ('Pending', 'Paid', 'Refunded', 'Failed')),
    TotalAmount REAL NOT NULL,
    CreatedAt TEXT NOT NULL,  -- ISO 8601 format
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
);

-- =============================================
-- Table: BookingItems
-- Description: Individual items within a booking
-- Links to either EventSeats or EventSectionInventories
-- =============================================
CREATE TABLE IF NOT EXISTS BookingItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BookingId INTEGER NOT NULL,
    EventSeatId INTEGER,  -- For reserved seating bookings
    EventSectionInventoryId INTEGER,  -- For section-based bookings
    Quantity INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id) ON DELETE CASCADE,
    FOREIGN KEY (EventSeatId) REFERENCES EventSeats(Id),
    FOREIGN KEY (EventSectionInventoryId) REFERENCES EventSectionInventories(Id),
    CHECK((EventSeatId IS NOT NULL AND EventSectionInventoryId IS NULL) OR 
          (EventSeatId IS NULL AND EventSectionInventoryId IS NOT NULL))
);

-- =============================================
-- Indexes for Performance
-- =============================================

-- Venue-related indexes
CREATE INDEX IF NOT EXISTS idx_venue_sections_venue_id 
    ON VenueSections(VenueId);

CREATE INDEX IF NOT EXISTS idx_venue_seats_section_id 
    ON VenueSeats(VenueSectionId);

CREATE INDEX IF NOT EXISTS idx_venue_seats_lookup 
    ON VenueSeats(VenueSectionId, Row, SeatNumber);

-- Event-related indexes
CREATE INDEX IF NOT EXISTS idx_events_venue_id 
    ON Events(VenueId);

CREATE INDEX IF NOT EXISTS idx_events_type 
    ON Events(EventType);

CREATE INDEX IF NOT EXISTS idx_events_starts_at 
    ON Events(StartsAt);

-- Event inventory indexes
CREATE INDEX IF NOT EXISTS idx_event_section_inventories_event_id 
    ON EventSectionInventories(EventId);

CREATE INDEX IF NOT EXISTS idx_event_section_inventories_section_id 
    ON EventSectionInventories(VenueSectionId);

CREATE INDEX IF NOT EXISTS idx_event_seats_event_id 
    ON EventSeats(EventId);

CREATE INDEX IF NOT EXISTS idx_event_seats_status 
    ON EventSeats(Status);

CREATE INDEX IF NOT EXISTS idx_event_seats_venue_seat_id 
    ON EventSeats(VenueSeatId);

-- Booking-related indexes
CREATE INDEX IF NOT EXISTS idx_bookings_user_id 
    ON Bookings(UserId);

CREATE INDEX IF NOT EXISTS idx_bookings_event_id 
    ON Bookings(EventId);

CREATE INDEX IF NOT EXISTS idx_bookings_created_at 
    ON Bookings(CreatedAt);

CREATE INDEX IF NOT EXISTS idx_booking_items_booking_id 
    ON BookingItems(BookingId);

CREATE INDEX IF NOT EXISTS idx_booking_items_event_seat_id 
    ON BookingItems(EventSeatId);

CREATE INDEX IF NOT EXISTS idx_booking_items_section_inventory_id 
    ON BookingItems(EventSectionInventoryId);

-- User indexes
CREATE INDEX IF NOT EXISTS idx_users_email 
    ON Users(Email);
