using System.Data;
using System.Data.Common;
using StudioSync.Bookings.Application;

namespace StudioSync.Bookings.Infrastructure.Persistence;

/// <summary>
/// Executes ADR-0004 scripts 2, 2.1 and 3. Each operation is a single
/// transaction combining state mutation with its transactional outbox event.
/// </summary>
internal sealed class BookingRepository(IDbConnectionFactory connectionFactory) : IBookingRepository
{
    /// <summary>ADR-0004 script 2: register the request as PENDING and emit BookingRequested.</summary>
    public async Task CreatePendingAsync(Domain.Booking booking, string outboxPayload, CancellationToken cancellationToken = default)
    {
        var sql = $$"""
            INSERT INTO "Booking"."Bookings" (
                "Id", "ClassScheduleId", "Status", "WaitlistPosition", "CustomerId", "CreatedAt"
            ) VALUES (
                '{{booking.Id}}'::uuid, '{{booking.ClassScheduleId}}'::uuid, 'Pending', NULL, '{{booking.CustomerId}}'::uuid, NOW()
            );

            INSERT INTO "Booking"."OutboxMessages" (
                "Id", "EventType", "Payload", "OccurredAt"
            ) VALUES (
                gen_random_uuid(), 'BookingRequested', @Payload::jsonb, NOW()
            );
            """;

        await ExecuteInTransactionAsync(command =>
            AddParameter(command, "@Payload", outboxPayload), sql, cancellationToken);
    }

    /// <summary>
    /// ADR-0004 script 2.1: atomic slot reservation guarded against an active waitlist,
    /// otherwise a deterministic FIFO waitlist position. Emits BookingConfirmed or BookingWaitlisted.
    /// </summary>
    public async Task FulfillAsync(Guid bookingId, Guid classScheduleId, CancellationToken cancellationToken = default)
    {
        var sql = $$"""
            DO $$
            DECLARE
                v_rows_affected INT;
                v_next_waitlist_pos INT;
            BEGIN
                UPDATE "Booking"."ClassSchedules"
                SET
                    "BookedSlots" = "BookedSlots" + 1,
                    "RowVersion" = "RowVersion" + 1
                WHERE
                    "Id" = '{{classScheduleId}}'::uuid
                    AND "BookedSlots" < "Capacity"
                    AND NOT EXISTS (
                        SELECT 1
                        FROM "Booking"."Bookings"
                        WHERE "ClassScheduleId" = '{{classScheduleId}}'::uuid
                          AND "Status" = 'Waitlisted'
                    );

                GET DIAGNOSTICS v_rows_affected = ROW_COUNT;

                IF v_rows_affected > 0 THEN
                    UPDATE "Booking"."Bookings"
                    SET
                        "Status" = 'Booked',
                        "WaitlistPosition" = NULL,
                        "UpdatedAt" = NOW()
                    WHERE "Id" = '{{bookingId}}'::uuid;

                    INSERT INTO "Booking"."OutboxMessages" ("Id", "EventType", "Payload", "OccurredAt")
                    VALUES (
                        gen_random_uuid(),
                        'BookingConfirmed',
                        json_build_object('BookingId', '{{bookingId}}'::uuid)::text::jsonb,
                        NOW()
                    );
                ELSE
                    PERFORM 1
                    FROM "Booking"."ClassSchedules"
                    WHERE "Id" = '{{classScheduleId}}'::uuid
                    FOR UPDATE;

                    SELECT COALESCE(MAX("WaitlistPosition"), 0) + 1
                    INTO v_next_waitlist_pos
                    FROM "Booking"."Bookings"
                    WHERE "ClassScheduleId" = '{{classScheduleId}}'::uuid
                      AND "Status" = 'Waitlisted';

                    UPDATE "Booking"."Bookings"
                    SET
                        "Status" = 'Waitlisted',
                        "WaitlistPosition" = v_next_waitlist_pos,
                        "UpdatedAt" = NOW()
                    WHERE "Id" = '{{bookingId}}'::uuid;

                    INSERT INTO "Booking"."OutboxMessages" ("Id", "EventType", "Payload", "OccurredAt")
                    VALUES (
                        gen_random_uuid(),
                        'BookingWaitlisted',
                        json_build_object('BookingId', '{{bookingId}}'::uuid)::text::jsonb,
                        NOW()
                    );
                END IF;
            END $$;
            """;

        await ExecuteInTransactionAsync(_ => { }, sql, cancellationToken);
    }

    /// <summary>
    /// ADR-0004 script 3: idempotent cancellation. Promotes the FIFO head of the waitlist
    /// (SKIP LOCKED) or decrements capacity when no waitlist exists.
    /// </summary>
    public async Task CancelAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var sql = $$"""
            DO $$
            DECLARE
                v_previous_status VARCHAR(30);
                v_class_schedule_id UUID;
                v_waitlisted_booking_id UUID;
            BEGIN
                SELECT "Status", "ClassScheduleId"
                INTO v_previous_status, v_class_schedule_id
                FROM "Booking"."Bookings"
                WHERE "Id" = '{{bookingId}}'::uuid
                FOR UPDATE;

                IF v_previous_status IS NULL OR v_previous_status = 'Cancelled' THEN
                    RETURN;
                END IF;

                UPDATE "Booking"."Bookings"
                SET "Status" = 'Cancelled', "UpdatedAt" = NOW()
                WHERE "Id" = '{{bookingId}}'::uuid;

                IF v_previous_status = 'Booked' THEN
                    SELECT "Id"
                    INTO v_waitlisted_booking_id
                    FROM "Booking"."Bookings"
                    WHERE "ClassScheduleId" = v_class_schedule_id
                      AND "Status" = 'Waitlisted'
                    ORDER BY "WaitlistPosition" ASC
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED;

                    IF v_waitlisted_booking_id IS NOT NULL THEN
                        UPDATE "Booking"."Bookings"
                        SET
                            "Status" = 'Booked',
                            "WaitlistPosition" = NULL,
                            "UpdatedAt" = NOW()
                        WHERE "Id" = v_waitlisted_booking_id;

                        INSERT INTO "Booking"."OutboxMessages" ("Id", "EventType", "Payload", "OccurredAt")
                        VALUES (
                            gen_random_uuid(),
                            'BookingPromotedFromWaitlist',
                            json_build_object('BookingId', v_waitlisted_booking_id)::text::jsonb,
                            NOW()
                        );
                    ELSE
                        UPDATE "Booking"."ClassSchedules"
                        SET "BookedSlots" = "BookedSlots" - 1
                        WHERE "Id" = v_class_schedule_id;
                    END IF;
                END IF;
            END $$;
            """;

        await ExecuteInTransactionAsync(_ => { }, sql, cancellationToken);
    }

    public Task<Domain.Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Query support added with the read-side requirements of Milestone 2.
        throw new NotImplementedException();
    }

    private async Task ExecuteInTransactionAsync(Action<IDbCommand> configure, string sql, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        configure(command);

        if (command is DbCommand dbCommand)
        {
            await dbCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}