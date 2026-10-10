namespace Feedback_Postgres_Persistence_;

using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Features.Feedback.Infrastructure;
using TimeWarp.Architecture.Testing;

/// <summary>
/// Live Postgres round-trip for a feedback filing, and the attachment store's conditional link,
/// unlink, and pending-expiry statements. Prefers an explicit connection string, else an
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

  public static async Task Attachment_store_links_conditionally_and_expires_pending_rows()
  {
    if (await SkipIfUnavailableAsync()) return;

    await using PostgresDbContext db = await CreateContextAsync();
    await db.Database.MigrateAsync();
    EfFeedbackStore items = new(db);
    EfFeedbackAttachmentStore attachments = new(db);

    Guid owner = Guid.NewGuid();
    DateTimeOffset now = DateTimeOffset.UtcNow;
    FeedbackItem item = FeedbackItem.File(owner, FeedbackKind.Complaint, "With files", "Body", now);
    await items.AddAsync(item);
    FeedbackItem other = FeedbackItem.File(owner, FeedbackKind.Complaint, "Second", "Body", now);
    await items.AddAsync(other);

    var pending = FeedbackAttachment.Create(owner, "notes.txt", "text/plain", 5, now);
    var expired = FeedbackAttachment.Create(owner, "lost.txt", "text/plain", 5, now.AddDays(-2));
    await attachments.AddAsync(pending);
    await attachments.AddAsync(expired);

    (await attachments.TryLinkAsync(pending.Id, Guid.NewGuid(), item.Id)).ShouldBeFalse();
    (await attachments.TryLinkAsync(pending.Id, owner, item.Id)).ShouldBeTrue();
    (await attachments.TryLinkAsync(pending.Id, owner, other.Id)).ShouldBeFalse();
    (await attachments.FindAsync(pending.Id))!.FeedbackItemId.ShouldBe(item.Id);

    await attachments.UnlinkAsync(pending.Id, other.Id);
    (await attachments.FindAsync(pending.Id))!.FeedbackItemId.ShouldBe(item.Id);
    await attachments.UnlinkAsync(pending.Id, item.Id);
    (await attachments.FindAsync(pending.Id))!.FeedbackItemId.ShouldBeNull();

    IReadOnlyList<FeedbackAttachment> removed = await attachments.RemoveExpiredUnlinkedAsync(owner, now.AddDays(-1));
    removed.ShouldHaveSingleItem().Id.ShouldBe(expired.Id);
    (await attachments.FindAsync(expired.Id)).ShouldBeNull();
    (await attachments.CountUnlinkedByOwnerAsync(owner)).ShouldBe(1);

    await items.RemoveAsync(other.Id);
    (await items.FindAsync(other.Id)).ShouldBeNull();
    (await items.FindAsync(item.Id)).ShouldNotBeNull();
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
