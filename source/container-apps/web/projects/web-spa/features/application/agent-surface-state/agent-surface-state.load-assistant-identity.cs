#region Purpose
// Loads the admin-set assistant name and its avatar for every assistant label.
#endregion

#region Design
// GetAssistantIdentity is anonymous, so this runs for signed-out visitors too: the top-bar button
// shows on every page. AuthenticationStateListener dispatches it once at startup (after the auth work),
// AssistantIdentityRefreshHandler runs it again after a successful UpdateSiteSettings (so a rename
// shows at once, whether the admin page or the agent saved) and after ResetStore. Not a
// DefaultApiHandler: a failure keeps the current name (the default until the first answer) and
// must not toast on pages that never asked for it. The name goes through AssistantNameRules again
// because every label trusts state; an invalid answer keeps the current one.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Identity;
using static GetAssistantIdentity;

partial class AgentSurfaceState
{
  public static class LoadAssistantIdentityActionSet
  {
    [CommandPaletteOptOut("Startup data load; the shell, a site-settings save and a store reset run it, a person never needs to.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler : ApiHandler<Action, Query, Response>
    {
      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher
      ) : base(store, webServerApiService, logger, publisher)
      {
      }

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Query?>(new Query());

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        if (AssistantNameRules.IsValid(response.Name))
        {
          AgentSurfaceState.AssistantName = response.Name.Trim();
          AgentSurfaceState.AssistantAvatar = string.IsNullOrWhiteSpace(response.Avatar) ? null : response.Avatar;
        }

        return Task.CompletedTask;
      }

      protected override Task HandleFileResponse(FileResponse fileResponse, CancellationToken cancellationToken) =>
        Task.CompletedTask;

      protected override Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken) =>
        Task.CompletedTask;
    }
  }
}
