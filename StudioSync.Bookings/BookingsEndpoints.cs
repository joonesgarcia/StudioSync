using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using StudioSync.Bookings.Application.Requests;

namespace StudioSync.Bookings;

/// <summary>
/// HTTP surface of the Bookings module. Error responses follow RFC 7807
/// Problem Details (ADR-0002, item 8) via the ASP.NET Core ProblemDetails service.
/// </summary>
public static class BookingsEndpoints
{
    public static IEndpointRouteBuilder MapBookingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings")
            .WithTags("Bookings");

        group.MapPost("/schedules", async (
                CreateClassScheduleRequest request,
                CreateClassScheduleHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return Results.Created(
                    $"/api/bookings/schedules/{result.ClassScheduleId}", result);
            })
            .WithName("CreateClassSchedule")
            .Produces<CreateClassScheduleResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/", async (
                RequestBookingRequest request,
                RequestBookingHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return Results.Accepted($"/api/bookings/{result.BookingId}", result);
            })
            .WithName("RequestBooking")
            .Produces<RequestBookingResult>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/{bookingId:guid}", async (
                [FromRoute] Guid bookingId,
                CancelBookingHandler handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(new CancelBookingRequest(bookingId), cancellationToken);
                return Results.NoContent();
            })
            .WithName("CancelBooking")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
