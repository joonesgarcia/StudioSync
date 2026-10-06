# 0004. Scripts for Base Case

* **Status**: Proposed
* **Data**: 05-10-2026

## Definition

This handles 90+% of the use cases for the platform.
By use case we meant traffic.

Instead of prematurely complicating the stack with Redis dual-writes for every standard booking, 
We will be using PostgreSQL's atomic updates and Transactional Outbox pattern as the base. 

If telemetry indicates specific 'flash drop' bottlenecks on high-demand classes, 
We will introduce an in-memory Redis reservation layer specifically for those endpoints 

1. Create Class Schedule & Emit Creation Event

```sql
BEGIN;

-- 1. Insert static class detail template metadata
INSERT INTO Booking.ClassDetails (
    Id, 
    StudioId, 
    Title, 
    Description, 
    RegistrationOpenTime, 
    RegistrationCloseTime
) VALUES (
    :ClassDetailId, 
    :StudioId, 
    :Title, 
    :Description, 
    :RegistrationOpenTime, 
    :RegistrationCloseTime
);

-- 2. Insert temporal schedule instance with initial capacity state
INSERT INTO Booking.ClassSchedules (
    Id, 
    ClassDetailId, 
    StartTime, 
    EndTime, 
    Capacity, 
    BookedSlots, 
    RowVersion
) VALUES (
    :ClassScheduleId, 
    :ClassDetailId, 
    :StartTime, 
    :EndTime, 
    :Capacity, 
    0, 
    1
);

-- 3. Write domain event to Outbox using a unique message UUID
INSERT INTO Booking.OutboxMessages (
    Id, 
    EventType, 
    Payload, 
    OccurredAt
) VALUES (
    gen_random_uuid(), 
    'ClassCreated', 
    :Payload, 
    NOW()
);

COMMIT;
```

A process receives the ClassCreated event and notifies clients.

---------------------------------------------------------------------------------

2. Request Booking & Process Asynchronous Fulfillment
Registers the customer's request in a fast PENDING state and emits a BookingRequested event.

```sql
BEGIN;

-- 1. Create initial pending booking
INSERT INTO Booking.Bookings (
    Id, 
    ClassScheduleId, 
    Status, 
    WaitlistPosition, 
    CustomerId, 
    CreatedAt
) VALUES (
    :BookingId, 
    :ClassScheduleId, 
    'PENDING', 
    NULL, 
    :CustomerId, 
    NOW()
);

-- 2. Write event to Outbox using a unique Outbox Message ID
INSERT INTO Booking.OutboxMessages (
    Id, 
    EventType, 
    Payload, 
    OccurredAt
) VALUES (
    gen_random_uuid(), 
    'BookingRequested', 
    :Payload, 
    NOW()
);

COMMIT;
```

2.1 Executes the atomic reservation check-and-set or routes the request to a deterministic FIFO waitlist position.

```sql
DO $$
DECLARE
    v_rows_affected INT;
    v_next_waitlist_pos INT;
BEGIN
    -- 1. Attempt atomic slot reservation
    UPDATE Booking.ClassSchedules
    SET 
        BookedSlots = BookedSlots + 1,
        RowVersion = RowVersion + 1
    WHERE 
        Id = :ClassScheduleId 
        AND BookedSlots < Capacity
        -- GUARD: Block direct bookings if an active waitlist exists
        AND NOT EXISTS (
            SELECT 1 
            FROM Booking.Bookings 
            WHERE ClassScheduleId = :ClassScheduleId 
              AND Status = 'WAITLISTED'
        );

    GET DIAGNOSTICS v_rows_affected = ROW_COUNT;

    -- 2. Check if slot was successfully claimed
    IF v_rows_affected > 0 THEN
        -- SUCCESS: Confirm booking reservation
        UPDATE Booking.Bookings
        SET 
            Status = 'BOOKED',
            WaitlistPosition = NULL,
            UpdatedAt = NOW()
        WHERE Id = :BookingId;

        -- Write confirmation event to Outbox
        INSERT INTO Booking.OutboxMessages (Id, EventType, Payload, OccurredAt)
        VALUES (gen_random_uuid(), 'BookingConfirmed', :ConfirmedPayload, NOW());

    ELSE
        -- FAILURE: Class is full or waitlist is active.
        -- Lock schedule row to serialize waitlist sequence generation safely
        PERFORM 1 
        FROM Booking.ClassSchedules 
        WHERE Id = :ClassScheduleId 
        FOR UPDATE;

        SELECT COALESCE(MAX(WaitlistPosition), 0) + 1
        INTO v_next_waitlist_pos
        FROM Booking.Bookings
        WHERE ClassScheduleId = :ClassScheduleId 
          AND Status = 'WAITLISTED';

        -- Update booking status to WAITLISTED
        UPDATE Booking.Bookings
        SET 
            Status = 'WAITLISTED',
            WaitlistPosition = v_next_waitlist_pos,
            UpdatedAt = NOW()
        WHERE Id = :BookingId;

        -- Write waitlisted event to Outbox
        INSERT INTO Booking.OutboxMessages (Id, EventType, Payload, OccurredAt)
        VALUES (gen_random_uuid(), 'BookingWaitlisted', :WaitlistedPayload, NOW());
    END IF;
END $$;

```

---------------------------------------------------------------------------------

3. Executes idempotently when a user cancels a reservation. 
Promotes the top waitlisted user if a confirmed spot was freed, or decrements capacity if no waitlist exists.

```sql
DO $$
DECLARE
    v_previous_status VARCHAR(30);
    v_class_schedule_id UUID;
    v_waitlisted_booking_id UUID;
BEGIN
    -- 1. Lock and retrieve target booking details
    SELECT Status, ClassScheduleId 
    INTO v_previous_status, v_class_schedule_id
    FROM Booking.Bookings 
    WHERE Id = :BookingId 
    FOR UPDATE;

    -- IDEMPOTENCY / VALIDITY GUARD: Exit if booking does not exist or is already CANCELLED
    IF v_previous_status IS NULL OR v_previous_status = 'CANCELLED' THEN
        RETURN;
    END IF;

    -- 2. Mark reservation as CANCELLED
    UPDATE Booking.Bookings 
    SET Status = 'CANCELLED', UpdatedAt = NOW() 
    WHERE Id = :BookingId;

    -- 3. CONDITION: Only promote waitlist or decrement capacity if a confirmed spot was vacated
    IF v_previous_status = 'BOOKED' THEN
        -- Select top waitlisted customer (skip locked rows to prevent worker contention)
        SELECT Id 
        INTO v_waitlisted_booking_id
        FROM Booking.Bookings
        WHERE ClassScheduleId = v_class_schedule_id 
          AND Status = 'WAITLISTED'
        ORDER BY WaitlistPosition ASC 
        LIMIT 1
        FOR UPDATE SKIP LOCKED;

        IF v_waitlisted_booking_id IS NOT NULL THEN
            -- Promote waitlisted customer to BOOKED
            UPDATE Booking.Bookings 
            SET 
                Status = 'BOOKED', 
                WaitlistPosition = NULL, 
                UpdatedAt = NOW() 
            WHERE Id = v_waitlisted_booking_id;

            -- Emit outbox event for async promotion notification
            INSERT INTO Booking.OutboxMessages (Id, EventType, Payload, OccurredAt) 
            VALUES (
                gen_random_uuid(), 
                'BookingPromotedFromWaitlist', 
                json_build_object('BookingId', v_waitlisted_booking_id)::text, 
                NOW()
            );
        ELSE
            -- No waitlist present: Safely decrement booked capacity counter
            UPDATE Booking.ClassSchedules 
            SET BookedSlots = BookedSlots - 1 
            WHERE Id = v_class_schedule_id;
        END IF;
    END IF;
END $$;
```