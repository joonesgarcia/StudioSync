namespace StudioSync.Bookings.Domain;

/// <summary>
/// Source of truth for a customer booking status. Maps to Booking.Bookings (ADR-0003).
/// </summary>
public sealed class Booking
{
    public Guid Id { get; set; }
    public Guid ClassScheduleId { get; set; }
    public BookingStatus Status { get; set; }
    public int? WaitlistPosition { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
