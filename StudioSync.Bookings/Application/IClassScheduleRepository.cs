using StudioSync.Bookings.Domain;

namespace StudioSync.Bookings.Application;

/// <summary>
/// Persistence port for class schedules and their details.
/// Implementations live in Infrastructure; consumers depend only on this contract (ADR-0002, item 4).
/// </summary>
public interface IClassScheduleRepository
{
    Task CreateWithDetailAsync(ClassDetail detail, ClassSchedule schedule, string outboxPayload, CancellationToken cancellationToken = default);
    Task<ClassSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
