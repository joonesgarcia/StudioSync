using FluentMigrator;

namespace StudioSync.Bookings.Infrastructure.Migrations;

/// <summary>
/// Creates the Booking schema owned by this module (ADR-0002, item 1)
/// with the relational model defined in ADR-0003.
/// </summary>
[Migration(2026_10_05_0001)]
public sealed class Migration0001_CreateBookingSchema : Migration
{
    private const string Schema = "Booking";

    public override void Up()
    {
        Create.Schema(Schema);

        Create.Table("ClassDetails").InSchema(Schema)
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("StudioId").AsGuid().NotNullable()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Description").AsString(2000).NotNullable()
            .WithColumn("RegistrationOpenTime").AsDateTimeOffset().NotNullable()
            .WithColumn("RegistrationCloseTime").AsDateTimeOffset().NotNullable()
            .WithColumn("StartTime").AsDateTimeOffset().NotNullable()
            .WithColumn("EndTime").AsDateTimeOffset().NotNullable();

        Create.Table("ClassSchedules").InSchema(Schema)
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("ClassDetailId").AsGuid().NotNullable()
                .ForeignKey("FK_ClassSchedules_ClassDetails", Schema, "ClassDetails", "Id")
            .WithColumn("Capacity").AsInt32().NotNullable()
            .WithColumn("BookedSlots").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("RowVersion").AsInt32().NotNullable().WithDefaultValue(1);

        Create.Table("Bookings").InSchema(Schema)
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("ClassScheduleId").AsGuid().NotNullable()
                .ForeignKey("FK_Bookings_ClassSchedules", Schema, "ClassSchedules", "Id")
            .WithColumn("Status").AsString(30).NotNullable()
            .WithColumn("WaitlistPosition").AsInt32().Nullable()
            .WithColumn("CustomerId").AsGuid().NotNullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

        Create.Table("OutboxMessages").InSchema(Schema)
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("EventType").AsString(100).NotNullable()
            .WithColumn("Payload").AsCustom("jsonb").NotNullable()
            .WithColumn("OccurredAt").AsDateTimeOffset().NotNullable()
            .WithColumn("ProcessedAt").AsDateTimeOffset().Nullable()
            .WithColumn("Error").AsString(2000).Nullable();

        Create.Index("IX_Bookings_ClassScheduleId_Status")
            .OnTable("Bookings").InSchema(Schema)
            .OnColumn("ClassScheduleId").Ascending()
            .OnColumn("Status").Ascending();

        Create.Index("IX_OutboxMessages_ProcessedAt")
            .OnTable("OutboxMessages").InSchema(Schema)
            .OnColumn("ProcessedAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("OutboxMessages").InSchema(Schema);
        Delete.Table("Bookings").InSchema(Schema);
        Delete.Table("ClassSchedules").InSchema(Schema);
        Delete.Table("ClassDetails").InSchema(Schema);
        Delete.Schema(Schema);
    }
}
