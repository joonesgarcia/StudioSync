namespace StudioSync.Bookings.Domain;

/// <summary>
/// Lifecycle status of a booking. Persisted as text in Booking.Bookings (see ADR-0003).
/// </summary>
public enum BookingStatus
{
    Pending,
    Booked,
    Waitlisted,
    Cancelled
}
