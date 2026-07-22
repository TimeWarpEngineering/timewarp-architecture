#region Purpose
// One-call DI wiring for the shared substrate (DbContext factory + recording publisher + baseline
// store) plus the spike's ephemeral-Postgres connection/schema helpers.
#endregion

#region Design
// Connection default targets the EPHEMERAL spike container on port 5433 (docker run --rm ...
// postgres:17), never the dev-run Postgres/volume — the 113-001 WAL lesson. SPIKE_POSTGRES_CONNECTION
// overrides it. EnsureFreshSchemaAsync does EnsureDeleted+EnsureCreated per test run (no migrations;
// the spike is throwaway). RecordingIntegrationEventPublisher is a singleton so a test can read back
// what the actor/grain published through the abstract seam.
#endregion

namespace TimeWarp.Spike.DualActor;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class SpikePostgres
{
  public const string DefaultConnectionString =
    "Host=localhost;Port=5433;Database=spike_ledger;Username=postgres;Password=spike;Include Error Detail=true";

  public static string ConnectionString =>
    Environment.GetEnvironmentVariable("SPIKE_POSTGRES_CONNECTION") ?? DefaultConnectionString;

  public static IServiceCollection AddLedgerSubstrate(this IServiceCollection services, string? connectionString = null)
  {
    string resolved = connectionString ?? ConnectionString;

    services.AddDbContextFactory<LedgerDbContext>(options => options.UseNpgsql(resolved));
    services.AddSingleton<RecordingIntegrationEventPublisher>();
    services.AddSingleton<IIntegrationEventPublisher>(sp => sp.GetRequiredService<RecordingIntegrationEventPublisher>());
    services.AddSingleton<LedgerStore>();
    return services;
  }

  public static async Task EnsureFreshSchemaAsync(IServiceProvider services, CancellationToken cancellationToken = default)
  {
    IDbContextFactory<LedgerDbContext> factory = services.GetRequiredService<IDbContextFactory<LedgerDbContext>>();
    await using LedgerDbContext context = await factory.CreateDbContextAsync(cancellationToken);
    await context.Database.EnsureDeletedAsync(cancellationToken);
    await context.Database.EnsureCreatedAsync(cancellationToken);
  }
}
