using System.Text.Json;
using StudioSync.Bookings.Domain;

namespace StudioSync.Bookings.Application.Requests;

/// <summary>Command: create a class detail + schedule (ADR-0004, script 1).</summary>
public sealed record CreateClassScheduleRequest(
    Guid StudioId,
    string Title,
    string Description,
    DateTime RegistrationOpenTime,
    DateTime RegistrationCloseTime,
    DateTime StartTime,
    DateTime EndTime,
    int Capacity);

public sealed record CreateClassScheduleResult(Guid ClassDetailId, Guid ClassScheduleId);

public sealed class CreateClassScheduleHandler(IClassScheduleRepository repository)
{
    public async Task<CreateClassScheduleResult> HandleAsync(
        CreateClassScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var detail = new ClassDetail
        {
            Id = Guid.NewGuid(),
            StudioId = request.StudioId,
            Title = request.Title,
            Description = request.Description,
            RegistrationOpenTime = request.RegistrationOpenTime,
            RegistrationCloseTime = request.RegistrationCloseTime,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        var schedule = new ClassSchedule
        {
            Id = Guid.NewGuid(),
            ClassDetailId = detail.Id,
            Capacity = request.Capacity
        };

        var payload = JsonSerializer.Serialize(new
        {
            ClassDetailId = detail.Id,
            ClassScheduleId = schedule.Id,
            detail.StudioId
        });

        await repository.CreateWithDetailAsync(detail, schedule, payload, cancellationToken);

        return new CreateClassScheduleResult(detail.Id, schedule.Id);
    }
}
