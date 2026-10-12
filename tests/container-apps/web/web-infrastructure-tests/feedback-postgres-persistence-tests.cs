namespace Feedback_Postgres_Persistence_;

using TimeWarp.Architecture.Features.Feedback.Application;
using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Features.Feedback.Infrastructure;
using TimeWarp.Architecture.Testing;

/// <summary>
/// Live Postgres round-trip for a feedback filing, the attachment store's conditional link,
/// unlink-by-item, and pending-expiry statements, and the triage paths of EfFeedbackStore (task 314):
/// UpdateAsync inserting only new thread entries, a stale copy returning false, and a filtered,
/// paged ListAllAsync. Prefers an explicit connection string, else an
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

    var second = FeedbackAttachment.Create(owner, "second.txt", "text/plain", 5, now);
    await attachments.AddAsync(second);
    (await attachments.TryLinkAsync(second.Id, owner, item.Id)).ShouldBeTrue();

    await attachments.UnlinkAllAsync(other.Id);
    (await attachments.FindAsync(pending.Id))!.FeedbackItemId.ShouldBe(item.Id);
    await attachments.UnlinkAllAsync(item.Id);
    (await attachments.FindAsync(pending.Id))!.FeedbackItemId.ShouldBeNull();
    (await attachments.FindAsync(second.Id))!.FeedbackItemId.ShouldBeNull();

    IReadOnlyList<FeedbackAttachment> removed = await attachments.RemoveExpiredUnlinkedAsync(owner, now.AddDays(-1));
    removed.ShouldHaveSingleItem().Id.ShouldBe(expired.Id);
    (await attachments.FindAsync(expired.Id)).ShouldBeNull();
    (await attachments.CountUnlinkedByOwnerAsync(owner)).ShouldBe(2);

    await items.RemoveAsync(other.Id);
    (await items.FindAsync(other.Id)).ShouldBeNull();
    (await items.FindAsync(item.Id)).ShouldNotBeNull();
  }

  public static async Task Update_appends_the_thread_and_rejects_a_stale_copy()
  {
    if (await SkipIfUnavailableAsync()) return;

    await using PostgresDbContext setup = await CreateContextAsync();
    await setup.Database.MigrateAsync();
    Guid owner = Guid.NewGuid();
    Guid admin = Guid.NewGuid();
    DateTimeOffset now = DateTimeOffset.UtcNow;
    FeedbackItem filed = FeedbackItem.File(owner, FeedbackKind.BugReport, "Triage me", "Body", now);
    await new EfFeedbackStore(setup).AddAsync(filed);

    await using PostgresDbContext firstDb = await CreateContextAsync();
    await using PostgresDbContext staleDb = await CreateContextAsync();
    EfFeedbackStore first = new(firstDb);
    EfFeedbackStore stale = new(staleDb);
    FeedbackItem copy = (await first.FindAsync(filed.Id)).ShouldNotBeNull();
    FeedbackItem staleCopy = (await stale.FindAsync(filed.Id)).ShouldNotBeNull();

    copy.ChangeStatus(FeedbackStatus.Planned, admin, now.AddMinutes(1));
    copy.AddAdminReply(admin, "On the roadmap.", now.AddMinutes(2));
    copy.AddInternalNote(admin, "Secret note", now.AddMinutes(3));
    (await first.UpdateAsync(copy)).ShouldBeTrue();

    staleCopy.ChangeStatus(FeedbackStatus.Declined, admin, now.AddMinutes(4));
    (await stale.UpdateAsync(staleCopy)).ShouldBeFalse();

    await using PostgresDbContext appendDb = await CreateContextAsync();
    EfFeedbackStore append = new(appendDb);
    FeedbackItem reloaded = (await append.FindAsync(filed.Id)).ShouldNotBeNull();
    reloaded.Status.ShouldBe(FeedbackStatus.Planned);
    reloaded.Activity.Select(entry => entry.Kind).ShouldBe(
      [FeedbackActivityKind.StatusChange, FeedbackActivityKind.AdminReply, FeedbackActivityKind.InternalNote]);
    reloaded.AddFilerReply(owner, "Thank you!", now.AddMinutes(5));
    (await append.UpdateAsync(reloaded)).ShouldBeTrue();

    await using PostgresDbContext readDb = await CreateContextAsync();
    FeedbackItem final = (await new EfFeedbackStore(readDb).FindAsync(filed.Id)).ShouldNotBeNull();
    final.Activity.Select(entry => entry.Sequence).ShouldBe([1, 2, 3, 4]);
    final.Activity[^1].Kind.ShouldBe(FeedbackActivityKind.FilerReply);
    final.Title.ShouldBe("Triage me");
  }

  public static async Task ListAll_filters_by_status_kind_and_window_and_pages_newest_first()
  {
    if (await SkipIfUnavailableAsync()) return;

    await using PostgresDbContext db = await CreateContextAsync();
    await db.Database.MigrateAsync();
    EfFeedbackStore store = new(db);
    // A private FiledAt window keeps rows from other tests and earlier runs out of the result.
    DateTimeOffset start = new DateTimeOffset(2090, 1, 1, 0, 0, 0, TimeSpan.Zero)
      .AddMinutes(System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 5_000_000) * 10);
    Guid owner = Guid.NewGuid();
    Guid admin = Guid.NewGuid();
    FeedbackItem oldest = FeedbackItem.File(owner, FeedbackKind.BugReport, "Oldest", "Body", start.AddMinutes(1));
    FeedbackItem middle = FeedbackItem.File(owner, FeedbackKind.BugReport, "Middle", "Body", start.AddMinutes(2));
    FeedbackItem newest = FeedbackItem.File(owner, FeedbackKind.BugReport, "Newest", "Body", start.AddMinutes(3));
    FeedbackItem otherKind = FeedbackItem.File(owner, FeedbackKind.Complaint, "Other kind", "Body", start.AddMinutes(4));
    FeedbackItem closed = FeedbackItem.File(owner, FeedbackKind.BugReport, "Closed", "Body", start.AddMinutes(5));
    foreach (FeedbackItem item in new[] { oldest, middle, newest, otherKind, closed })
    {
      await store.AddAsync(item);
    }

    FeedbackItem closing = (await store.FindAsync(closed.Id)).ShouldNotBeNull();
    closing.ChangeStatus(FeedbackStatus.Completed, admin, start.AddMinutes(6));
    (await store.UpdateAsync(closing)).ShouldBeTrue();

    FeedbackStatus[] open = [FeedbackStatus.New, FeedbackStatus.UnderReview, FeedbackStatus.Planned, FeedbackStatus.InProgress];
    FeedbackPage first = await store.ListAllAsync(
      new FeedbackListFilter(open, FeedbackKind.BugReport, start, start.AddMinutes(10), Skip: 0, Take: 2));
    first.TotalCount.ShouldBe(3);
    first.Items.Select(item => item.Title).ShouldBe(["Newest", "Middle"]);

    FeedbackPage second = await store.ListAllAsync(
      new FeedbackListFilter(open, FeedbackKind.BugReport, start, start.AddMinutes(10), Skip: 2, Take: 2));
    second.Items.Select(item => item.Title).ShouldBe(["Oldest"]);

    FeedbackPage closedOnly = await store.ListAllAsync(
      new FeedbackListFilter([FeedbackStatus.Completed], null, start, start.AddMinutes(10), Skip: 0, Take: 10));
    closedOnly.Items.ShouldHaveSingleItem().Id.ShouldBe(closed.Id);
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
