using System.Text.Json;

namespace StudioSync.Bookings.Application.Requests;

/// <summary>
/// Command: register a booking request as PENDING and emit BookingRequested (ADR-0004, script 2).
/// Fulfillment (script 2.1) is processed asynchronously from the outbox.
/// </summary>
public sealed record RequestBookingRequest(Guid ClassScheduleId, Guid CustomerId);

public sealed record RequestBookingResult(Guid BookingId);

public sealed class RequestBookingHandler(IBookingRepository repository)
{
    public async Task<RequestBookingResult> HandleAsync(
        RequestBookingRequest request, CancellationToken cancellationToken = default)
    {
        var booking = new Domain.Booking
        {
            Id = Guid.NewGuid(),
            ClassScheduleId = request.ClassScheduleId,
            CustomerId = request.CustomerId,
            Status = Domain.BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var payload = JsonSerializer.Serialize(new
        {
            BookingId = booking.Id,
            booking.ClassScheduleId,
            booking.CustomerId
        });

        await repository.CreatePendingAsync(booking, payload, cancellationToken);

        return new RequestBookingResult(booking.Id);
    }
}
