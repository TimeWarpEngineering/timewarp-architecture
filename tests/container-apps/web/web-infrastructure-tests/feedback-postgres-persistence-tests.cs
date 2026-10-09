namespace Feedback_Postgres_Persistence_;

using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Testing;

/// <summary>
/// Live Postgres round-trip for a feedback filing. Prefers an explicit connection string, else an
/// ephemeral Testcontainers Postgres. When neither is available the test is skipped, same as Profile.
/// </summary>
public class Round_Trip
{
  private static readonly Lazy<Task<PostgresTestAvailability>> Availability =
    new(() => PostgresTestAvailability.ResolveAsync("timewarp_feedback_tests"), LazyThreadSafetyMode.ExecutionAndPublication);

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Round_Trip>();

  public static async Task Migrate_creates_feedback_items_and_round_trips()
  {
    if (await SkipIfUnavailableAsync()) return;

    await using PostgresDbContext write = await CreateContextAsync();
    await write.Database.MigrateAsync();

    Guid owner = Guid.NewGuid();
    var filedAt = new DateTimeOffset(2026, 10, 9, 2, 0, 0, TimeSpan.Zero);
    FeedbackItem item = FeedbackItem.File(
      owner,
      FeedbackKind.Complaint,
      "Export failed",
      "Nothing came back.",
      filedAt);
    FeedbackItemId id = item.Id;
    write.FeedbackItems.Add(item);
    await write.SaveChangesAsync();
    item.Version.ShouldBe(0);

    await using PostgresDbContext read = await CreateContextAsync();
    FeedbackItem reloaded = await read.FeedbackItems.SingleAsync(row => row.Id == id);
    reloaded.OwnerPrincipalId.ShouldBe(owner);
    reloaded.Kind.ShouldBe(FeedbackKind.Complaint);
    reloaded.Title.ShouldBe("Export failed");
    reloaded.Body.ShouldBe("Nothing came back.");
    reloaded.FiledAt.ShouldBe(filedAt);
    reloaded.Version.ShouldBe(0);
  }

  private static async Task<bool> SkipIfUnavailableAsync()
  {
    PostgresTestAvailability availability = await Availability.Value;
    if (availability.ConnectionString is not null)
    {
      return false;
    }

    if (PostgresTestAvailability.IsCiEnvironment())
    {
      throw new InvalidOperationException(
        "Feedback Postgres live tests require a connection string or Docker under CI. " +
        (availability.SkipReason ?? "no connection"));
    }

    Console.WriteLine($"[SKIP] Feedback_Postgres_Persistence: {availability.SkipReason ?? "no connection"}");
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
