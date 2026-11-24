#region Purpose
// Postgres-backed feedback stores for the feedback runfiles: one fresh, migrated database per world.
#endregion

#region Design
// Task 522 removed the in-memory feedback stores, so the handler runfiles run on the EF stores.
// Postgres comes from PostgresTestAvailability (an env connection string, else Testcontainers).
// The first world in a process migrates a template database once; every world then gets its own
// database cloned from it (CREATE DATABASE ... TEMPLATE), so list and count assertions never see
// another test's rows and a world costs a clone, not a migration. The pools are cleared after the
// migration because Postgres refuses to clone a template that has open connections, and clones
// are serialized for the same reason. A clone's connection string turns pooling off: a pool lives
// per connection string for the process, so one pool per world would hold an idle connection to
// every world's database and exhaust Postgres's max_connections in the family aggregator run.
// The database is created on the first store call, so a world is still built with `new` and the
// test bodies do not change. Each store call opens its own PostgresDbContext and disposes it, the
// same unit of work a request scope gives the handler in the app; a context shared across calls
// would keep tracked rows that the next Update attaches over.
// Without Postgres the first store call throws: under CI that is the failure the suite wants, and
// locally the message names the two ways to supply a database.
// Generated apps compile this file only with the postgres flag (template.json excludes the
// feedback slice otherwise).
#endregion

namespace TimeWarp.Architecture.Features.Feedback
{
  using Microsoft.EntityFrameworkCore;
  using Npgsql;
  using TimeWarp.Architecture.Features.Feedback.Application;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Architecture.Features.Feedback.Infrastructure;
  using TimeWarp.Architecture.Persistence;
  using TimeWarp.Architecture.Testing;

  /// <summary>Clones a migrated template database for each feedback world.</summary>
  internal static class FeedbackTestDatabase
  {
    private static readonly Lazy<Task<string>> Template =
      new(CreateTemplateAsync, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly SemaphoreSlim CloneGate = new(1, 1);

    /// <summary>A connection string for a new database with every migration applied.</summary>
    public static async Task<string> CreateAsync()
    {
      string template = await Template.Value.ConfigureAwait(false);
      string admin = await AdminConnectionStringAsync().ConfigureAwait(false);
      string name = "feedback_" + Guid.NewGuid().ToString("N");
      await CloneGate.WaitAsync().ConfigureAwait(false);
      try
      {
        await ExecuteAsync(admin, $"CREATE DATABASE \"{name}\" TEMPLATE \"{template}\"").ConfigureAwait(false);
      }
      finally
      {
        CloneGate.Release();
      }

      return new NpgsqlConnectionStringBuilder(admin) { Database = name, Pooling = false }.ConnectionString;
    }

    public static PostgresDbContext Context(string connectionString) =>
      new(new DbContextOptionsBuilder<PostgresDbContext>()
        .UseNpgsql(connectionString, PostgresRetryPolicy.Configure)
        .Options);

    private static async Task<string> CreateTemplateAsync()
    {
      string admin = await AdminConnectionStringAsync().ConfigureAwait(false);
      string name = "feedback_template_" + Guid.NewGuid().ToString("N");
      await ExecuteAsync(admin, $"CREATE DATABASE \"{name}\"").ConfigureAwait(false);
      string connectionString = new NpgsqlConnectionStringBuilder(admin) { Database = name }.ConnectionString;
      await using (PostgresDbContext db = Context(connectionString))
      {
        await db.Database.MigrateAsync().ConfigureAwait(false);
      }

      NpgsqlConnection.ClearAllPools();
      return name;
    }

    private static async Task<string> AdminConnectionStringAsync()
    {
      PostgresTestAvailability availability = await Availability.Value.ConfigureAwait(false);
      return availability.AdminConnectionString
        ?? throw new InvalidOperationException(
          "The feedback runfiles need Postgres (task 522: there is no in-memory feedback store). " +
          (availability.SkipReason ?? "No connection."));
    }

    private static readonly Lazy<Task<PostgresTestAvailability>> Availability =
      new(() => PostgresTestAvailability.ResolveAsync("timewarp_feedback_runfiles"), LazyThreadSafetyMode.ExecutionAndPublication);

    // Raw SQL because CREATE DATABASE takes an identifier, which cannot be a parameter. The name
    // is minted from a Guid in this class, never caller input.
    private static async Task ExecuteAsync(string connectionString, string sql)
    {
      await using PostgresDbContext db = Context(connectionString);
      await db.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);
    }
  }

  /// <summary>One world's database, created on first use and shared by its three stores.</summary>
  internal sealed class FeedbackTestStores
  {
    private readonly Lazy<Task<string>> ConnectionString =
      new(FeedbackTestDatabase.CreateAsync, LazyThreadSafetyMode.ExecutionAndPublication);

    public FeedbackTestStores()
    {
      Feedback = new PostgresFeedbackTestStore(this);
      Attachments = new PostgresFeedbackAttachmentTestStore(this);
      Blobs = new PostgresFeedbackAttachmentBlobTestStore(this);
    }

    public PostgresFeedbackTestStore Feedback { get; }
    public PostgresFeedbackAttachmentTestStore Attachments { get; }
    public PostgresFeedbackAttachmentBlobTestStore Blobs { get; }

    public async Task<PostgresDbContext> ContextAsync() =>
      FeedbackTestDatabase.Context(await ConnectionString.Value.ConfigureAwait(false));
  }

  /// <summary>EfFeedbackStore over a new context per call.</summary>
  internal sealed class PostgresFeedbackTestStore(FeedbackTestStores stores) : IFeedbackStore
  {
    public async Task AddAsync(FeedbackItem item, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      await new EfFeedbackStore(db).AddAsync(item, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FeedbackItem?> FindAsync(FeedbackItemId id, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackStore(db).FindAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FeedbackItem>> ListByOwnerAsync(
      Guid ownerPrincipalId,
      CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackStore(db).ListByOwnerAsync(ownerPrincipalId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FeedbackPage> ListAllAsync(FeedbackListFilter filter, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackStore(db).ListAllAsync(filter, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(FeedbackItem item, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackStore(db).UpdateAsync(item, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(FeedbackItemId id, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      await new EfFeedbackStore(db).RemoveAsync(id, cancellationToken).ConfigureAwait(false);
    }
  }

  /// <summary>EfFeedbackAttachmentStore over a new context per call.</summary>
  internal sealed class PostgresFeedbackAttachmentTestStore(FeedbackTestStores stores) : IFeedbackAttachmentStore
  {
    public async Task AddAsync(FeedbackAttachment attachment, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      await new EfFeedbackAttachmentStore(db).AddAsync(attachment, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FeedbackAttachment?> FindAsync(FeedbackAttachmentId id, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackAttachmentStore(db).FindAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FeedbackAttachment>> ListByItemAsync(
      FeedbackItemId itemId,
      CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackAttachmentStore(db).ListByItemAsync(itemId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountUnlinkedByOwnerAsync(Guid ownerPrincipalId, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackAttachmentStore(db).CountUnlinkedByOwnerAsync(ownerPrincipalId, cancellationToken)
        .ConfigureAwait(false);
    }

    public async Task<bool> TryLinkAsync(
      FeedbackAttachmentId id,
      Guid ownerPrincipalId,
      FeedbackItemId itemId,
      CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackAttachmentStore(db).TryLinkAsync(id, ownerPrincipalId, itemId, cancellationToken)
        .ConfigureAwait(false);
    }

    public async Task UnlinkAllAsync(FeedbackItemId itemId, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      await new EfFeedbackAttachmentStore(db).UnlinkAllAsync(itemId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FeedbackAttachment>> RemoveExpiredUnlinkedAsync(
      Guid ownerPrincipalId,
      DateTimeOffset uploadedBefore,
      CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackAttachmentStore(db)
        .RemoveExpiredUnlinkedAsync(ownerPrincipalId, uploadedBefore, cancellationToken)
        .ConfigureAwait(false);
    }

    public async Task RemoveAsync(FeedbackAttachmentId id, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      await new EfFeedbackAttachmentStore(db).RemoveAsync(id, cancellationToken).ConfigureAwait(false);
    }
  }

  /// <summary>EfFeedbackAttachmentBlobStore over a new context per call.</summary>
  internal sealed class PostgresFeedbackAttachmentBlobTestStore(FeedbackTestStores stores) : IFeedbackAttachmentBlobStore
  {
    public async Task PutAsync(
      string key,
      byte[] content,
      string contentType,
      CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      await new EfFeedbackAttachmentBlobStore(db).PutAsync(key, content, contentType, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackAttachmentBlobStore(db).OpenReadAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      return await new EfFeedbackAttachmentBlobStore(db).ExistsAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
      await using PostgresDbContext db = await stores.ContextAsync().ConfigureAwait(false);
      await new EfFeedbackAttachmentBlobStore(db).DeleteAsync(key, cancellationToken).ConfigureAwait(false);
    }
  }
}
