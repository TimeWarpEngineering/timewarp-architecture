#region Purpose
// Registers the zero-infra IFeedbackStore default.
#endregion

#region Design
// Called from Web.Server Program next to the profile store module. PostgresDbModule replaces
// IFeedbackStore and IFeedbackAttachmentStore with the scoped EF stores when a connection
// string is present. Blob bytes are registered by FeedbackAttachmentBlobModule.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using TimeWarp.Architecture.Features.Feedback.Application;

public sealed class InMemoryFeedbackStoresModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    _ = configuration;
    serviceCollection.AddSingleton<IFeedbackStore, InMemoryFeedbackStore>();
    serviceCollection.AddSingleton<IFeedbackAttachmentStore, InMemoryFeedbackAttachmentStore>();
  }
}
