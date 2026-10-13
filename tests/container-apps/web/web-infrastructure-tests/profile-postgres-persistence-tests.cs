namespace Profile_Postgres_Persistence_;

using TimeWarp.Architecture.Testing;

/// <summary>
/// Live Postgres round-trips for the Profile teaching aggregate. Prefers an explicit connection
/// string (env), else an ephemeral Testcontainers Postgres (never a shared WithDataVolume).
/// When neither is available the tests are skipped — same spirit as PostgresDbModule skip-mode.
/// </summary>
public class Round_Trip
{
  private static readonly Lazy<Task<PostgresTestAvailability>> Availability =
    new(() => PostgresTestAvailability.ResolveAsync("timewarp_profile_tests"), LazyThreadSafetyMode.ExecutionAndPublication);

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

  public static async Task Store_FindMany_returns_only_existing_ids()
  {
    if (await SkipIfUnavailableAsync()) return;

    await using PostgresDbContext write = await CreateContextAsync();
    await write.Database.MigrateAsync();
    Profile ada = Profile.Create("Ada FindMany", "en-US", "US", "dark");
    Profile grace = Profile.Create("Grace FindMany", "en-US", "US", "dark");
    Profile unrelated = Profile.Create("Unrelated FindMany", "en-US", "US", "dark");
    write.Profiles.AddRange(ada, grace, unrelated);
    await write.SaveChangesAsync();

    await using PostgresDbContext read = await CreateContextAsync();
    EfProfileStore store = new(read);
    IReadOnlyList<Profile> found = await store.FindManyAsync([ada.Id, grace.Id, ProfileId.New(), ada.Id]);

    found.Select(profile => profile.Id).OrderBy(id => id.Value).ShouldBe(new[] { ada.Id, grace.Id }.OrderBy(id => id.Value));
    (await store.FindManyAsync([])).ShouldBeEmpty();
  }

  public static async Task Store_Search_matches_name_or_email_ignoring_case_and_treats_wildcards_literally()
  {
    if (await SkipIfUnavailableAsync()) return;

    string token = Guid.NewGuid().ToString("N");
    await using PostgresDbContext write = await CreateContextAsync();
    await write.Database.MigrateAsync();
    Profile byName = Profile.Create($"Searchable {token} Name", "en-US", "US", "dark");
    Profile byEmail = Profile.Create("Plain Person", "en-US", "US", "dark");
    byEmail.SetEmail($"Mail.{token}@Example.com");
    Profile other = Profile.Create("Somebody Else", "en-US", "US", "dark");
    other.SetEmail("else@example.com");
    write.Profiles.AddRange(byName, byEmail, other);
    await write.SaveChangesAsync();

    await using PostgresDbContext read = await CreateContextAsync();
    EfProfileStore store = new(read);

    (await store.SearchAsync(token.ToUpperInvariant())).Select(profile => profile.Id).OrderBy(id => id.Value)
      .ShouldBe(new[] { byName.Id, byEmail.Id }.OrderBy(id => id.Value));
    (await store.SearchAsync($"mail.{token}@")).Select(profile => profile.Id).ShouldBe([byEmail.Id]);
    (await store.SearchAsync("%")).ShouldBeEmpty("a percent sign is a literal, not a LIKE wildcard");
    await Should.ThrowAsync<ArgumentException>(() => store.SearchAsync("  "));
  }

  private static async Task<bool> SkipIfUnavailableAsync()
  {
    PostgresTestAvailability availability = await Availability.Value;
    if (availability.ConnectionString is not null)
    {
      return false;
    }

    // CI must exercise real Postgres concurrency — soft-skip would green a broken Docker host.
    // Interactive local agents without Docker still soft-skip (model-mapping tests always run).
    if (PostgresTestAvailability.IsCiEnvironment())
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

  private static async Task<PostgresDbContext> CreateContextAsync()
  {
    PostgresTestAvailability availability = await Availability.Value;
    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql(availability.ConnectionString, PostgresRetryPolicy.Configure)
      .Options;
    return new PostgresDbContext(options);
  }
}
