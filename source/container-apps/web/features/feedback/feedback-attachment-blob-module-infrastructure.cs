#region Purpose
// Registers the feedback attachment blob store: Azure when configured, in-memory otherwise
// (PostgresDbModule may then swap in the Postgres store).
#endregion

#region Design
// Mode selection is Azure, then Postgres, then in-memory (task 352).
// Azure Blob Storage is the production store. Cloudflare R2 is not: the template already
// deploys to Azure, and an S3 client would be a second object stack nothing else uses.
// FeedbackAttachments:ConnectionString selects Azure (IsAzureConfigured). When it is empty
// (the committed appsettings value) this module registers InMemoryFeedbackAttachmentBlobStore,
// and PostgresDbModule, which runs after it, replaces that with EfFeedbackAttachmentBlobStore
// when a Postgres connection string is present. So the bytes live in the same database as the
// attachment rows, and a restart keeps both. The in-memory store remains for tests, mock mode,
// and apps generated without the postgres flag. No Azurite resource is added to AppHost.
// FeedbackAttachments:ContainerName defaults to "feedback-attachments". The name is checked
// here so a bad value fails at startup instead of on the first upload. The container stays
// private.
// Only the in-memory store loses bytes on restart while rows may survive, so only then would
// every older download 404. InMemoryBlobWarning resolves the final registration at startup
// and logs a warning when it is still in-memory and either the row store is durable or the
// environment is not Development. It only warns: tests and mock mode keep the fallback, and a
// host may knowingly run without durable files.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Feedback.Application;

public sealed class FeedbackAttachmentBlobModule : IModule
{
  public const string DefaultContainerName = "feedback-attachments";

  private const string ConnectionStringKey = "FeedbackAttachments:ConnectionString";

  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    if (!IsAzureConfigured(configuration))
    {
      serviceCollection.AddSingleton<IFeedbackAttachmentBlobStore, InMemoryFeedbackAttachmentBlobStore>();
      serviceCollection.AddHostedService<InMemoryBlobWarning>();
      return;
    }

    string containerName = configuration["FeedbackAttachments:ContainerName"] ?? "";
    if (string.IsNullOrWhiteSpace(containerName))
    {
      containerName = DefaultContainerName;
    }

    ValidateContainerName(containerName);
    string connectionString = configuration[ConnectionStringKey]!;
    serviceCollection.AddSingleton<IFeedbackAttachmentBlobStore>(
      _ => new AzureFeedbackAttachmentBlobStore(connectionString, containerName));
  }

  /// <summary>True when Azure Blob Storage holds the bytes; other stores must not replace it.</summary>
  public static bool IsAzureConfigured(IConfiguration configuration) =>
    !string.IsNullOrWhiteSpace(configuration[ConnectionStringKey]);

  private static void ValidateContainerName(string name)
  {
    if (name.Length is < 3 or > 63)
    {
      throw new InvalidOperationException(
        $"FeedbackAttachments:ContainerName '{name}' must be 3 to 63 characters.");
    }

    if (name[0] == '-' || name[^1] == '-')
    {
      throw new InvalidOperationException(
        $"FeedbackAttachments:ContainerName '{name}' must not start or end with a hyphen.");
    }

    bool previousHyphen = false;
    foreach (char character in name)
    {
      bool allowed = character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-';
      if (!allowed || (character == '-' && previousHyphen))
      {
        throw new InvalidOperationException(
          $"FeedbackAttachments:ContainerName '{name}' must be lowercase letters, digits, and single hyphens.");
      }

      previousHyphen = character == '-';
    }
  }

  private sealed class InMemoryBlobWarning : IHostedService
  {
    private static readonly Action<ILogger, string, string, Exception?> LogInMemoryBlobs =
      LoggerMessage.Define<string, string>
      (
        LogLevel.Warning,
        new EventId(1, nameof(LogInMemoryBlobs)),
        "Feedback attachment bytes are held in memory (FeedbackAttachments:ConnectionString is empty) while rows use {RowStore} in {Environment}; files are lost on restart"
      );

    private readonly IServiceScopeFactory ScopeFactory;
    private readonly IHostEnvironment HostEnvironment;
    private readonly ILogger<FeedbackAttachmentBlobModule> Logger;

    public InMemoryBlobWarning(
      IServiceScopeFactory scopeFactory,
      IHostEnvironment hostEnvironment,
      ILogger<FeedbackAttachmentBlobModule> logger)
    {
      ScopeFactory = scopeFactory;
      HostEnvironment = hostEnvironment;
      Logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      using IServiceScope scope = ScopeFactory.CreateScope();
      IFeedbackAttachmentBlobStore blobStore = scope.ServiceProvider.GetRequiredService<IFeedbackAttachmentBlobStore>();
      if (blobStore is not InMemoryFeedbackAttachmentBlobStore)
      {
        return Task.CompletedTask;
      }

      IFeedbackAttachmentStore rowStore = scope.ServiceProvider.GetRequiredService<IFeedbackAttachmentStore>();
      bool durableRows = rowStore is not InMemoryFeedbackAttachmentStore;
      if (durableRows || !HostEnvironment.IsDevelopment())
      {
        LogInMemoryBlobs(Logger, rowStore.GetType().Name, HostEnvironment.EnvironmentName, null);
      }

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
  }
}
