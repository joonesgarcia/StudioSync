using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StudioSync.Bookings.Infrastructure.Migrations;

/// <summary>
/// Applies the module's pending FluentMigrator migrations at host startup.
/// Registered by <see cref="BookingsModule.AddBookingsModule"/>.
/// </summary>
internal sealed class MigrationRunnerService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        await Task.Run(() => runner.MigrateUp(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
