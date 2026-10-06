using StudioSync.Bookings.Domain;

namespace StudioSync.Bookings.Application;

/// <summary>
/// Persistence port for bookings. All write operations participate in a single
/// transaction with their outbox messages (ADR-0002, item 5; ADR-0004).
/// </summary>
public interface IBookingRepository
{
    /// <summary>Script 2: insert a PENDING booking and the BookingRequested outbox event atomically.</summary>
    Task CreatePendingAsync(Domain.Booking booking, string outboxPayload, CancellationToken cancellationToken = default);

    /// <summary>Script 2.1: atomic check-and-set reservation, or deterministic FIFO waitlisting.</summary>
    Task FulfillAsync(Guid bookingId, Guid classScheduleId, CancellationToken cancellationToken = default);

    /// <summary>Script 3: idempotent cancellation with waitlist promotion.</summary>
    Task CancelAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<Domain.Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
