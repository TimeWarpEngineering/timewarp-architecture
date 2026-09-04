namespace Profile_Postgres_Persistence_;

using Docker.DotNet;
using Testcontainers.PostgreSql;

/// <summary>
/// Live Postgres round-trips for the Profile teaching aggregate. Prefers an explicit connection
/// string (env), else an ephemeral Testcontainers Postgres (never a shared WithDataVolume).
/// When neither is available the tests are skipped — same spirit as PostgresDbModule skip-mode.
/// </summary>
public class Round_Trip
{
  private static readonly Lazy<Task<PostgresAvailability>> Availability =
    new(ResolveAvailabilityAsync, LazyThreadSafetyMode.ExecutionAndPublication);

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Round_Trip>();

  public static async Task Migrate_creates_profile_table_and_round_trips()
  {
    if (await SkipIfUnavailableAsync()) return;

    await using PostgresDbContext write = await CreateContextAsync();
    await write.Database.MigrateAsync();

    Profile profile = Profile.Create("Ada Lovelace", "en-US", "US", "dark");
    profile.SetEmail("ada@example.com");
    ProfileId id = profile.Id;
    write.Profiles.Add(profile);
    await write.SaveChangesAsync();
    profile.Version.ShouldBe(0);

    await using PostgresDbContext read = await CreateContextAsync();
    Profile reloaded = await read.Profiles.SingleAsync(p => p.Id == id);

    reloaded.DisplayName.ShouldBe("Ada Lovelace");
    reloaded.Email.ShouldBe("ada@example.com");
    reloaded.Language.ShouldBe("en-US");
    reloaded.Region.ShouldBe("US");
    reloaded.Theme.ShouldBe("dark");
    reloaded.Notifications.ShouldBeFalse();
    reloaded.Version.ShouldBe(0);
  }

  public static async Task Concurrent_update_throws_db_update_concurrency_exception()
  {
    if (await SkipIfUnavailableAsync()) return;

    await using PostgresDbContext seed = await CreateContextAsync();
    await seed.Database.MigrateAsync();

    Profile profile = Profile.Create("Grace Hopper", "en-US", "US", "light");
    ProfileId id = profile.Id;
    seed.Profiles.Add(profile);
    await seed.SaveChangesAsync();

    await using PostgresDbContext first = await CreateContextAsync();
    await using PostgresDbContext second = await CreateContextAsync();
    Profile a = await first.Profiles.SingleAsync(p => p.Id == id);
    Profile b = await second.Profiles.SingleAsync(p => p.Id == id);

    a.Rename("Grace M. Hopper");
    await first.SaveChangesAsync();
    a.Version.ShouldBe(1);

    b.Rename("Amazing Grace");
    DbUpdateConcurrencyException exception =
      await Should.ThrowAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    exception.Entries.ShouldNotBeEmpty();
  }

  public static async Task Modified_save_increments_version_through_aggregate_savechanges_hook()
  {
    if (await SkipIfUnavailableAsync()) return;

    await using PostgresDbContext db = await CreateContextAsync();
    await db.Database.MigrateAsync();

    Profile profile = Profile.Create("Katherine Johnson", "en-US", "US", "dark");
    ProfileId id = profile.Id;
    db.Profiles.Add(profile);
    await db.SaveChangesAsync();
    profile.Version.ShouldBe(0);

    profile.EnableNotifications();
    await db.SaveChangesAsync();
    profile.Version.ShouldBe(1);

    await using PostgresDbContext read = await CreateContextAsync();
    Profile reloaded = await read.Profiles.SingleAsync(p => p.Id == id);
    reloaded.Notifications.ShouldBeTrue();
    reloaded.Version.ShouldBe(1);
  }

  private static async Task<bool> SkipIfUnavailableAsync()
  {
    PostgresAvailability availability = await Availability.Value;
    if (availability.ConnectionString is not null)
    {
      return false;
    }

    // CI must exercise real Postgres concurrency — soft-skip would green a broken Docker host.
    // Interactive local agents without Docker still soft-skip (model-mapping tests always run).
    if (IsCiEnvironment())
    {
      throw new InvalidOperationException(
        "Profile Postgres live tests require a connection string or Docker under CI. " +
        (availability.SkipReason ?? "no connection"));
    }

    // Jaribu has no public conditional-skip exception used here; print and pass so hosts without
    // Docker (and without a connection string) still green the suite. Model-mapping tests cover mapping.
    Console.WriteLine($"[SKIP] Profile_Postgres_Persistence: {availability.SkipReason ?? "no connection"}");
    return true;
  }

  private static bool IsCiEnvironment() =>
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
    || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

  private static async Task<PostgresDbContext> CreateContextAsync()
  {
    PostgresAvailability availability = await Availability.Value;
    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql(availability.ConnectionString)
      .Options;
    return new PostgresDbContext(options);
  }

  private static async Task<PostgresAvailability> ResolveAvailabilityAsync()
  {
    string? fromEnv = Environment.GetEnvironmentVariable("PostgresDbOptions__ConnectionString")
      ?? Environment.GetEnvironmentVariable("ConnectionStrings__postgres-db");

    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
      return new PostgresAvailability(fromEnv, Container: null, SkipReason: null);
    }

    try
    {
      PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("timewarp_profile_tests")
        .WithUsername("timewarp")
        .WithPassword("timewarp")
        .Build();

      await container.StartAsync();
      // Container lifetime is process-scoped; Testcontainers Ryuk reaps it after the test host exits.
      return new PostgresAvailability(container.GetConnectionString(), container, SkipReason: null);
    }
    catch (Exception exception) when (
      exception is DockerApiException
        or DockerContainerNotFoundException
        or HttpRequestException
        or TimeoutException
        or IOException)
    {
      // Narrow filter: InvalidOperationException / NotSupportedException from Testcontainers
      // misconfiguration must surface, not look like "no Docker".
      string skipReason =
        "No Postgres connection available (set PostgresDbOptions__ConnectionString or " +
        "ConnectionStrings__postgres-db, or enable Docker for Testcontainers). " +
        exception.Message;
      return new PostgresAvailability(ConnectionString: null, Container: null, skipReason);
    }
  }

  private sealed record PostgresAvailability
  (
    string? ConnectionString,
    PostgreSqlContainer? Container,
    string? SkipReason
  );
}
