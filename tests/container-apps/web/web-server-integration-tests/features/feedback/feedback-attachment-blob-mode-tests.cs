#region Purpose
// Proves feedback attachment byte-store selection: Azure, then Postgres, then in-memory.
#endregion

#region Design
// Task 352. Runs the two modules in Program's order (FeedbackAttachmentBlobModule, then
// PostgresDbModule) against in-memory configuration and resolves IFeedbackAttachmentBlobStore
// from a scope. Nothing connects: PostgresDbContext and BlobContainerClient are only
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

  public static Task NothingConfigured_Should_UseInMemory()
  {
    Resolve(postgres: null, azure: null).ShouldBeOfType<InMemoryFeedbackAttachmentBlobStore>();
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
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["PostgresDbOptions:ConnectionString"] = postgres,
        ["FeedbackAttachments:ConnectionString"] = azure,
      })
      .Build();
    ServiceCollection services = new();
    services.AddLogging();
    InMemoryFeedbackStoresModule.ConfigureServices(services, configuration);
    FeedbackAttachmentBlobModule.ConfigureServices(services, configuration);
    PostgresDbModule.ConfigureServices(services, configuration);

    using ServiceProvider provider = services.BuildServiceProvider();
    using IServiceScope scope = provider.CreateScope();
    return scope.ServiceProvider.GetRequiredService<IFeedbackAttachmentBlobStore>();
  }
}
