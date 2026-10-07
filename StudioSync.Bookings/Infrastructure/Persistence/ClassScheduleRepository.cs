using System.Data;
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
        var sql = $$"""
            INSERT INTO "Booking"."ClassDetails" (
                "Id", "StudioId", "Title", "Description",
                "RegistrationOpenTime", "RegistrationCloseTime", "StartTime", "EndTime"
            ) VALUES (
                '{{detail.Id}}'::uuid, '{{detail.StudioId}}'::uuid, {{SqlString(detail.Title)}}, {{SqlString(detail.Description)}},
                '{{Utc(detail.RegistrationOpenTime)}}'::timestamptz, '{{Utc(detail.RegistrationCloseTime)}}'::timestamptz,
                '{{Utc(detail.StartTime)}}'::timestamptz, '{{Utc(detail.EndTime)}}'::timestamptz
            );

            INSERT INTO "Booking"."ClassSchedules" (
                "Id", "ClassDetailId", "Capacity", "BookedSlots", "RowVersion"
            ) VALUES (
                '{{schedule.Id}}'::uuid, '{{detail.Id}}'::uuid, {{schedule.Capacity}}, 0, 1
            );

            INSERT INTO "Booking"."OutboxMessages" (
                "Id", "EventType", "Payload", "OccurredAt"
            ) VALUES (
                gen_random_uuid(), 'ClassCreated', {{SqlString(outboxPayload)}}::jsonb, NOW()
            );
            """;

        await ExecuteInTransactionAsync(_ => { }, sql, cancellationToken);
    }

    public Task<ClassSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Query support added with the read-side requirements of Milestone 2.
        throw new NotImplementedException();
    }

    /// <summary>Escapes a string value for safe inline embedding in SQL single quotes.</summary>
    private static string SqlString(string value) => $"'{value.Replace("'", "''")}'";

    /// <summary>Normalizes a DateTime to UTC and formats it as ISO 8601 for timestamptz.</summary>
    private static string Utc(DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc => dt.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
        DateTimeKind.Local => dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
        _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
    };

    private async Task ExecuteInTransactionAsync(Action<IDbCommand> configure, string sql, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        configure(command);

        if (command is System.Data.Common.DbCommand dbCommand)
        {
            await dbCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}
