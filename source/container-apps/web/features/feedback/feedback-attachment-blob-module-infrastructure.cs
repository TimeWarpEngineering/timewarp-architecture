#region Purpose
// Registers the feedback attachment blob store: Azure when configured, in-memory otherwise.
#endregion

#region Design
// Azure Blob Storage is the real backing store. Cloudflare R2 is not: the template already
// deploys to Azure, and an S3 client would be a second object stack nothing else uses.
// FeedbackAttachments:ConnectionString selects the mode. Empty (the committed appsettings
// value) keeps InMemoryFeedbackAttachmentBlobStore, which is what tests, mock mode, and an
// unconfigured host use. No Azurite resource is added to AppHost.
// FeedbackAttachments:ContainerName defaults to "feedback-attachments". The name is checked
// here so a bad value fails at startup instead of on the first upload. The container stays
// private. Postgres swaps the row store only; this registration does not follow that flag.
// The in-memory fallback loses bytes on restart while EF rows survive, so every older
// download would 404. InMemoryBlobWarning logs a startup warning when the in-memory blob
// store runs next to a durable row store or outside Development. It only warns: tests and
// mock mode keep the fallback, and a host may knowingly run without durable files.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Feedback.Application;

public sealed class FeedbackAttachmentBlobModule : IModule
{
  public const string DefaultContainerName = "feedback-attachments";

  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    string? connectionString = configuration["FeedbackAttachments:ConnectionString"];
    if (string.IsNullOrWhiteSpace(connectionString))
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
    serviceCollection.AddSingleton<IFeedbackAttachmentBlobStore>(
      _ => new AzureFeedbackAttachmentBlobStore(connectionString, containerName));
  }

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
