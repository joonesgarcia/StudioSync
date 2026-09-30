# 0003.DB relational modeling

* **Status**: Proposed
* **Data**: 29-09-2026

## Definition

A customer must be able to log in and book a specific time slot for a service or event at a studio.

A studio tenant must be able to manage its own bookings. 
	By manage bookings we mean a reservation of a specific time slot for a service or event, for X number of people at its studio.

The platform must manage the capacity of each booking, including tracking available slots and managing waitlists.

Three modules will be created.
1. **Customer**: Responsible for managing customer information, including creating, updating, and deleting customer records.
2. **Studios**: Responsible for managing studios, including creating, updating, and deleting tenants.
3. **Booking.ClassDetails**: Responsible for managing class details, including creating, updating, and deleting class information.

4. **Booking.ClassSchedules**: Responsible for managing classes, including creating, updating, and canceling sessions.
5. **Booking.Bookings**: Source of truth for user bookings status.

6. **Booking.OutboxMessages**: Responsible serving integration events without fail to external systems.

Customers table will have the following columns:
- Id (Guid) (Primary Key)
- Email (String)
- Name (String)
- Phone (String)
- Address (String)
- CreatedAt (DateTime)
- PasswordHash (String)

Studios table will have the following columns:
- Id (Guid) (Primary Key)
- Name (String)
- Address (String)
- Phone (String)
- Email (String)
- CreatedAt (DateTime)
- PasswordHash (String)

Booking.ClassDetails table will have the following columns:
- Id (Guid) (Primary Key)
- StudioId (Guid)
- Title (String)
- Description (String)
- RegistrationOpenTime (DateTime)
- RegistrationCloseTime (DateTime)
- StartTime (DateTime)
- EndTime (DateTime)

Booking.ClassSchedules table will have the following columns:
- Id (Guid) (Primary Key)
- ClassDetailId (Foreign Key to Booking.ClassDetails)
- Capacity (Integer)
- BookedSlots (Integer)
- RowVersion (Integer)

Booking.Bookings table will have the following columns:
- Id (Guid) (Primary Key)
- ClassScheduleId (Foreign Key to Booking.ClassSchedules)
- Status (Enum: Pending, Booked, Waitlisted)
- WaitlistPosition (Integer)
- CustomerId
- CreatedAt (DateTime)

Booking.OutboxMessages table will have the following columns:
- Id (Guid) (Primary Key)
- EventType (String)
- Payload (String)
- OccurredAt (DateTime)
- ProcessedAt (DateTime)
- Error (String, Nullable)
