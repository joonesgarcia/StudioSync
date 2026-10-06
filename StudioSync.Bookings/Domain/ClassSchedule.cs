namespace StudioSync.Bookings.Domain;

/// <summary>
/// Temporal schedule instance with capacity state. Maps to Booking.ClassSchedules (ADR-0003).
/// RowVersion drives optimistic concurrency control (ADR-0002, item 6).
/// </summary>
public sealed class ClassSchedule
{
    public Guid Id { get; set; }
    public Guid ClassDetailId { get; set; }
    public int Capacity { get; set; }
    public int BookedSlots { get; set; }
    public int RowVersion { get; set; }
}
