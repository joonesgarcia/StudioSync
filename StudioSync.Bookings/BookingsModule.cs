using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudioSync.Bookings.Application;
using StudioSync.Bookings.Application.Requests;
using StudioSync.Bookings.Infrastructure.Migrations;
using StudioSync.Bookings.Infrastructure.Persistence;

namespace StudioSync.Bookings;

/// <summary>
/// Composition root of the Bookings module. The host plugs the module in via
/// <see cref="AddBookingsModule"/>; internals stay encapsulated (ADR-0002, item 3).
/// </summary>
public static class BookingsModule
{
    public static IServiceCollection AddBookingsModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BookingsModuleOptions>()
            .Bind(configuration.GetSection(BookingsModuleOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                "Bookings:ConnectionString is required.")
            .ValidateOnStart();

        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        services.AddScoped<IClassScheduleRepository, ClassScheduleRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddScoped<CreateClassScheduleHandler>();
        services.AddScoped<RequestBookingHandler>();
        services.AddScoped<CancelBookingHandler>();

        // Schema versioning for the module-owned Booking schema (ADR-0002, items 1-2).
        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(configuration
                    .GetSection(BookingsModuleOptions.SectionName)
                    .GetValue<string>("ConnectionString") ?? string.Empty)
                .ScanIn(typeof(BookingsModule).Assembly).For.Migrations())
            .AddLogging(logging => logging.AddFluentMigratorConsole());

        services.AddHostedService<MigrationRunnerService>();

        return services;
    }
}
