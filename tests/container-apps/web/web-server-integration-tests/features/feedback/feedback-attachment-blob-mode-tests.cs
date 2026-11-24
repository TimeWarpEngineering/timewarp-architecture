#region Purpose
// Proves feedback attachment byte-store selection: Azure, then Postgres, else no storage at all.
#endregion

#region Design
// Task 352. Runs the two modules in Program's order (FeedbackModule, then PostgresDbModule)
// against in-memory configuration and resolves IFeedbackAttachmentBlobStore from a scope. Task
// 522 removed the in-memory store: with neither connection string the port is
// UnavailableFeedbackStorage, which keeps nothing. Nothing connects: PostgresDbContext and BlobContainerClient are only
// constructed. Host-free — no HostGraph. Template-excluded when postgres is off, because
// PostgresDbModule and EfFeedbackAttachmentBlobStore are.
#endregion

namespace FeedbackAttachmentBlobMode_;

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features.Feedback.Application;
using TimeWarp.Architecture.Features.Feedback.Infrastructure;
using TimeWarp.Architecture.Modules;

[TestTag("Feedback")]
public class Selection_Given_
{
  private const string PostgresConnection = "Host=localhost;Database=never-opened;Username=u;Password=p";
  private const string AzureConnection = "UseDevelopmentStorage=true";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Selection_Given_>();

  public static Task NothingConfigured_Should_UseUnavailableStorage()
  {
    Resolve(postgres: null, azure: null).ShouldBeOfType<UnavailableFeedbackStorage>();
    return Task.CompletedTask;
  }

  public static Task PostgresOnly_Should_RegisterTheRowStores()
  {
    IConfiguration configuration = Configuration(PostgresConnection, azure: null);
    ServiceCollection services = new();
    FeedbackModule.ConfigureServices(services, configuration);
    PostgresDbModule.ConfigureServices(services, configuration);

    services.Count(descriptor => descriptor.ServiceType == typeof(IFeedbackStore)).ShouldBe(1);
    services.Count(descriptor => descriptor.ServiceType == typeof(IFeedbackAttachmentStore)).ShouldBe(1);
    services.Count(descriptor => descriptor.ServiceType == typeof(IFeedbackAttachmentBlobStore)).ShouldBe(1);
    return Task.CompletedTask;
  }

  public static Task PostgresOnly_Should_UsePostgres()
  {
    Resolve(PostgresConnection, azure: null).ShouldBeOfType<EfFeedbackAttachmentBlobStore>();
    return Task.CompletedTask;
  }

  public static Task AzureAndPostgres_Should_UseAzure()
  {
    Resolve(PostgresConnection, AzureConnection).ShouldBeOfType<AzureFeedbackAttachmentBlobStore>();
    return Task.CompletedTask;
  }

  public static Task AzureOnly_Should_UseAzure()
  {
    Resolve(postgres: null, AzureConnection).ShouldBeOfType<AzureFeedbackAttachmentBlobStore>();
    return Task.CompletedTask;
  }

  private static IFeedbackAttachmentBlobStore Resolve(string? postgres, string? azure)
  {
    IConfiguration configuration = Configuration(postgres, azure);
    ServiceCollection services = new();
    services.AddLogging();
    FeedbackModule.ConfigureServices(services, configuration);
    PostgresDbModule.ConfigureServices(services, configuration);

    using ServiceProvider provider = services.BuildServiceProvider();
    using IServiceScope scope = provider.CreateScope();
    return scope.ServiceProvider.GetRequiredService<IFeedbackAttachmentBlobStore>();
  }

  private static IConfiguration Configuration(string? postgres, string? azure) =>
    new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["PostgresDbOptions:ConnectionString"] = postgres,
        ["FeedbackAttachments:ConnectionString"] = azure,
      })
      .Build();
}
