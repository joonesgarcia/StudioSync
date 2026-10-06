using System.Data;
using System.Text.Json;
using StudioSync.Bookings.Application;
using StudioSync.Bookings.Domain;

namespace StudioSync.Bookings.Infrastructure.Persistence;

/// <summary>
/// Executes ADR-0004 script 1 (create schedule + outbox event) in a single transaction.
/// </summary>
internal sealed class ClassScheduleRepository(IDbConnectionFactory connectionFactory) : IClassScheduleRepository
{
    public async Task CreateWithDetailAsync(
        ClassDetail detail,
        ClassSchedule schedule,
        string outboxPayload,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string sql = """
            INSERT INTO "Booking"."ClassDetails" (
                "Id", "StudioId", "Title", "Description",
                "RegistrationOpenTime", "RegistrationCloseTime", "StartTime", "EndTime"
            ) VALUES (
                @ClassDetailId, @StudioId, @Title, @Description,
                @RegistrationOpenTime, @RegistrationCloseTime, @StartTime, @EndTime
            );

            INSERT INTO "Booking"."ClassSchedules" (
                "Id", "ClassDetailId", "Capacity", "BookedSlots", "RowVersion"
            ) VALUES (
                @ClassScheduleId, @ClassDetailId, @Capacity, 0, 1
            );

            INSERT INTO "Booking"."OutboxMessages" (
                "Id", "EventType", "Payload", "OccurredAt"
            ) VALUES (
                gen_random_uuid(), 'ClassCreated', @Payload::jsonb, NOW()
            );
            """;

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        AddParameter(command, "@ClassDetailId", detail.Id);
        AddParameter(command, "@StudioId", detail.StudioId);
        AddParameter(command, "@Title", detail.Title);
        AddParameter(command, "@Description", detail.Description);
        AddParameter(command, "@RegistrationOpenTime", detail.RegistrationOpenTime);
        AddParameter(command, "@RegistrationCloseTime", detail.RegistrationCloseTime);
        AddParameter(command, "@StartTime", scheduleStart(detail));
        AddParameter(command, "@EndTime", detail.EndTime);
        AddParameter(command, "@ClassScheduleId", schedule.Id);
        AddParameter(command, "@Capacity", schedule.Capacity);
        AddParameter(command, "@Payload", outboxPayload);

        await ExecuteAsync(command, cancellationToken);
        transaction.Commit();

        static DateTime scheduleStart(ClassDetail d) => d.StartTime;
    }

    public Task<ClassSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Query support added with the read-side requirements of Milestone 2.
        throw new NotImplementedException();
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static async Task ExecuteAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is System.Data.Common.DbCommand dbCommand)
        {
            await dbCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            command.ExecuteNonQuery();
        }
    }
}
