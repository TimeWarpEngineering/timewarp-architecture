#region Purpose
// FetchProfileDataActionSet: loads the current user's profile via the GetProfile API.
#endregion

#region Design
// Built on DefaultApiHandler so validation, auth token handling, and problem-details
// error reporting follow the shared API-call path; only HandleSuccess touches state,
// leaving failures to the common error pipeline.
// [TrackAction] exposes in-flight status so the UI can render a loading indicator while
// the fetch is pending.
// Task 489-002: the success path stores the response object. It does not copy fields onto the state.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using static GetProfile;

partial class ProfileState
{
  public static class FetchProfileDataActionSet
  {
    [TrackAction]
    [CommandPaletteOptOut("Page/startup data load; the owning page or shell runs it, a person never needs to.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
    {
      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher,
        IValidator<Query>? validator = null,
        AuthenticationStateProvider? authenticationStateProvider = null
      ) : base(store, webServerApiService, logger, publisher, validator, authenticationStateProvider) { }

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<Query?>(new Query());
      }
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        ProfileState.Profile = response;
        return Task.CompletedTask;
      }
    }
  }
}
