#region Purpose
// Registers the feedback slice's store-agnostic services and, when configured, Azure Blob Storage
// for attachment bytes.
#endregion

#region Design
// Mode selection for attachment bytes is Azure, then Postgres (tasks 352 and 522).
// Azure Blob Storage is the production store. Cloudflare R2 is not: the template already
// deploys to Azure, and an S3 client would be a second object stack nothing else uses.
// FeedbackAttachments:ConnectionString selects Azure (IsAzureConfigured). When it is empty
// (the committed appsettings value) PostgresDbModule, which runs after this module, registers
// EfFeedbackAttachmentBlobStore next to the EF row stores. So the bytes live in the same database
// as the attachment rows, and a restart keeps both.
// There is no in-memory fallback (task 522): a lossy byte store over durable rows is what left
// two production attachments without files. The whole slice compiles only with the postgres
// template flag, so an app generated without a database has no feedback feature. With the flag
// on but no connection string, the ports stay UnavailableFeedbackStorage (reads empty, writes
// refused), registered here so Development's build-time DI validation passes; PostgresDbModule
// replaces the row ports, and the byte port unless Azure took it.
// FeedbackOwnerNames (task 336) is scoped and store-agnostic, so it is registered here.
// FeedbackAttachments:ContainerName defaults to "feedback-attachments". The name is checked
// here so a bad value fails at startup instead of on the first upload. The container stays
// private. No Azurite resource is added to AppHost.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using TimeWarp.Architecture.Features.Feedback.Application;

public sealed class FeedbackModule : IModule
{
  public const string DefaultContainerName = "feedback-attachments";

  private const string ConnectionStringKey = "FeedbackAttachments:ConnectionString";

  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddScoped<FeedbackOwnerNames>();
    serviceCollection.AddSingleton<UnavailableFeedbackStorage>();
    serviceCollection.AddSingleton<IFeedbackStore>(static services => services.GetRequiredService<UnavailableFeedbackStorage>());
    serviceCollection.AddSingleton<IFeedbackAttachmentStore>(static services => services.GetRequiredService<UnavailableFeedbackStorage>());
    if (!IsAzureConfigured(configuration))
    {
      serviceCollection.AddSingleton<IFeedbackAttachmentBlobStore>(static services => services.GetRequiredService<UnavailableFeedbackStorage>());
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
}
