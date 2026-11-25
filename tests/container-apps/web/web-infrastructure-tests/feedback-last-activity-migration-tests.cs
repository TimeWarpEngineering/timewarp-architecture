#region Purpose
// The AddFeedbackLastActivity migration backfills LastActivityAt on feedback rows that already exist.
#endregion

#region Design
// Task 520. Migrate an empty database to the migration before AddFeedbackLastActivity (the
// migrator creates the database), insert two items with raw SQL in that older shape, then
// migrate to the latest. The current model maps every later column, so rows are read only after
// the whole chain has run. A row with a thread takes its last entry's At; a row without one takes
// FiledAt. A migrated row then passes the save-time invariant: the EF store appends a note and
// LastActivityAt moves to it. The database is dropped at the end.
#endregion

namespace FeedbackLastActivityMigration_;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Features.Feedback.Infrastructure;
using TimeWarp.Architecture.Testing;

public class Backfill_Given_
{
  private static readonly Lazy<Task<PostgresTestAvailability>> Availability =
    new(() => PostgresTestAvailability.ResolveAsync("timewarp_feedback_activity_migration_tests"), LazyThreadSafetyMode.ExecutionAndPublication);

  private static readonly Guid Quiet = Guid.Parse("10000000-0000-0000-0000-000000000001");
  private static readonly Guid Busy = Guid.Parse("10000000-0000-0000-0000-000000000002");
  private static readonly DateTimeOffset QuietFiledAt = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
  private static readonly DateTimeOffset BusyLastEntryAt = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Backfill_Given_>();

  public static async Task Existing_Rows_Should_Take_The_Last_Entry_Or_FiledAt()
  {
    PostgresTestAvailability availability = await Availability.Value;
    if (availability.AdminConnectionString is null)
    {
      if (PostgresTestAvailability.IsCiEnvironment())
      {
        throw new InvalidOperationException(
          "Feedback last-activity migration tests require a connection string or Docker under CI. " +
          (availability.SkipReason ?? "no connection"));
      }

      Console.WriteLine($"[SKIP] FeedbackLastActivityMigration_: {availability.SkipReason ?? "no connection"}");
      return;
    }

    string connectionString = new NpgsqlConnectionStringBuilder(availability.AdminConnectionString)
    {
      Database = "feedback_activity_" + Guid.NewGuid().ToString("N"),
      Pooling = false,
    }.ConnectionString;
    await using PostgresDbContext db = Context(connectionString);
    try
    {
      string[] migrations = [.. db.Database.GetMigrations()];
      string target = migrations.Single(id => id.EndsWith("_AddFeedbackLastActivity", StringComparison.Ordinal));
      await db.GetService<IMigrator>().MigrateAsync(migrations[Array.IndexOf(migrations, target) - 1]);

      await db.Database.ExecuteSqlRawAsync(
        """
        INSERT INTO feedback.feedback_items ("Id", "OwnerPrincipalId", "Kind", "Title", "Body", "FiledAt", "Status", "Version")
        VALUES
          ('10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'BugReport',
           'Quiet', 'No thread', '2026-10-01T00:00:00Z', 'New', 0),
          ('10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', 'BugReport',
           'Busy', 'Has a thread', '2026-10-02T00:00:00Z', 'UnderReview', 2);
        INSERT INTO feedback.feedback_activity ("FeedbackItemId", "Sequence", "Kind", "AuthorPrincipalId", "Body", "FromStatus", "ToStatus", "At")
        VALUES
          ('10000000-0000-0000-0000-000000000002', 1, 'StatusChange', '30000000-0000-0000-0000-000000000001',
           NULL, 'New', 'UnderReview', '2026-10-03T00:00:00Z'),
          ('10000000-0000-0000-0000-000000000002', 2, 'FilerReply', '20000000-0000-0000-0000-000000000001',
           'It happened again.', NULL, NULL, '2026-10-05T00:00:00Z');
        """);

      await db.GetService<IMigrator>().MigrateAsync();

      Dictionary<Guid, DateTimeOffset> lastActivity = await db.FeedbackItems.AsNoTracking()
        .ToDictionaryAsync(item => item.Id.Value, item => item.LastActivityAt);
      lastActivity[Quiet].ShouldBe(QuietFiledAt);
      lastActivity[Busy].ShouldBe(BusyLastEntryAt);

      await using PostgresDbContext storeDb = Context(connectionString);
      EfFeedbackStore store = new(storeDb);
      FeedbackItem busy = (await store.FindAsync(FeedbackItemId.From(Busy))).ShouldNotBeNull();
      DateTimeOffset noteAt = BusyLastEntryAt.AddDays(1);
      busy.AddInternalNote(Guid.Parse("30000000-0000-0000-0000-000000000001"), "Checked after the migration.", noteAt);
      (await store.UpdateAsync(busy)).ShouldBeTrue();
      (await store.FindAsync(FeedbackItemId.From(Busy))).ShouldNotBeNull().LastActivityAt.ShouldBe(noteAt);
    }
    finally
    {
      await db.Database.EnsureDeletedAsync();
    }
  }

  private static PostgresDbContext Context(string connectionString) =>
    new(new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql(connectionString, PostgresRetryPolicy.Configure)
      .Options);
}
