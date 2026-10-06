using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;

namespace StudioSync.Bookings.Infrastructure.Persistence;

internal sealed class NpgsqlConnectionFactory(IOptions<BookingsModuleOptions> options) : IDbConnectionFactory
{
    private readonly string _connectionString = options.Value.ConnectionString
        ?? throw new InvalidOperationException(
            $"The '{BookingsModuleOptions.SectionName}:ConnectionString' configuration value is required.");

    public IDbConnection Create() => new NpgsqlConnection(_connectionString);
}
