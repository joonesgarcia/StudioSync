namespace StudioSync.Bookings.Domain;

/// <summary>
/// Transactional outbox message guaranteeing reliable integration events (ADR-0002, item 5).
/// Maps to Booking.OutboxMessages (ADR-0003).
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}
