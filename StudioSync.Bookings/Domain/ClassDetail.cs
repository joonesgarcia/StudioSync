namespace StudioSync.Bookings.Domain;

/// <summary>
/// Static class template metadata. Maps to Booking.ClassDetails (ADR-0003).
/// </summary>
public sealed class ClassDetail
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime RegistrationOpenTime { get; set; }
    public DateTime RegistrationCloseTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
