namespace StudioSync.Bookings;

/// <summary>
/// Configuration for the Bookings module. Bound from the "Bookings" configuration section.
/// </summary>
public sealed class BookingsModuleOptions
{
    public const string SectionName = "Bookings";

    /// <summary>
    /// PostgreSQL connection string for the database hosting the Booking schema.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
