using System.Data;

namespace StudioSync.Bookings.Infrastructure.Persistence;

/// <summary>
/// Creates connections to the module-owned database. Keeps the rest of the module
/// decoupled from the concrete provider (Npgsql).
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection Create();
}
