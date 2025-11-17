-- =============================================
-- Event Booking System - Venue Schema
-- Database: SQLite
-- Description: Creates the schema for Venues, VenueSections, and VenueSeats
-- =============================================

-- =============================================
-- Table: Venues
-- Description: Stores venue information
-- =============================================
CREATE TABLE IF NOT EXISTS Venues (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Address TEXT NOT NULL,
    Capacity INTEGER NOT NULL DEFAULT 0
);

-- =============================================
-- Table: VenueSections
-- Description: Stores sections within a venue (e.g., Orchestra, Balcony, VIP)
-- =============================================
CREATE TABLE IF NOT EXISTS VenueSections (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VenueId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    FOREIGN KEY (VenueId) REFERENCES Venues(Id) ON DELETE CASCADE
);

-- =============================================
-- Table: VenueSeats
-- Description: Stores individual seats within venue sections
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
-- Indexes for Performance
-- =============================================
CREATE INDEX IF NOT EXISTS idx_venue_sections_venue_id ON VenueSections(VenueId);
CREATE INDEX IF NOT EXISTS idx_venue_seats_section_id ON VenueSeats(VenueSectionId);
CREATE INDEX IF NOT EXISTS idx_venue_seats_lookup ON VenueSeats(VenueSectionId, Row, SeatNumber);
