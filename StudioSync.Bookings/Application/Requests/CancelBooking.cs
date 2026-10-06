namespace StudioSync.Bookings.Application.Requests;

/// <summary>Command: idempotent cancellation with waitlist promotion (ADR-0004, script 3).</summary>
public sealed record CancelBookingRequest(Guid BookingId);

public sealed class CancelBookingHandler(IBookingRepository repository)
{
    public Task HandleAsync(CancelBookingRequest request, CancellationToken cancellationToken = default)
        => repository.CancelAsync(request.BookingId, cancellationToken);
}
