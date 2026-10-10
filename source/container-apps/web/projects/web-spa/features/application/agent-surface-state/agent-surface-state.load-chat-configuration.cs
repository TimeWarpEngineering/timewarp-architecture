#region Purpose
// Loads whether the server can call xAI, and whether this user may ask, for the Ask surface.
#endregion

#region Design
// Not a DefaultApiHandler: that base publishes ProblemDetailsNotification, and a signed-out 401
// must not toast. Every outcome goes through ChatReadinessProbe (task 293): the server's answer is
// Configured or NotConfigured, a 401 is Unauthenticated and anything else is Error with the status
// and detail. Before task 293 every failure became NotConfigured, which told a signed-out user to
// set a key that was already set.
// AuthenticationStateListener dispatches this at startup and on every AuthenticationState change,
// so an in-app sign-in (no page load) re-probes. It does not skip anonymous users: a mock-header
// session can be authorized on the server while the SPA principal is anonymous, so the server's
// status is the answer.
// Only a server answer carries the panel settings (task 292: RecordChats, PrivacyNotice, SupportUrl,
// CredentialLifetimeMinutes); every failure resets them to the XaiChatDefaults. SupportUrl goes
// through AskSupportLink.Normalize (relative path or http/https only) because the answer bar
// renders it into an href.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using static GetAgentChatConfiguration;

partial class AgentSurfaceState
{
  public static class LoadChatConfigurationActionSet
  {
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

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken) =>
        Apply(ChatReadinessProbe.FromResponse(response), response);

      protected override Task HandleFileResponse(FileResponse fileResponse, CancellationToken cancellationToken) =>
        Apply(ChatReadinessProbe.FromFileResponse(), response: null);

      protected override Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken) =>
        Apply(ChatReadinessProbe.FromProblem(problemDetails), response: null);

      private Task Apply(ChatProbeResult result, Response? response)
      {
        AgentSurfaceState.ChatReadiness = result.Readiness;
        AgentSurfaceState.ChatSetupCommand = result.SetupCommand;
        AgentSurfaceState.ChatModel = result.Model;
        AgentSurfaceState.ChatProblem = result.Problem;
        if (response is null)
        {
          AgentSurfaceState.RecordChats = XaiChatDefaults.RecordChats;
          AgentSurfaceState.PrivacyNotice = XaiChatDefaults.PrivacyNotice;
          AgentSurfaceState.SupportUrl = XaiChatDefaults.SupportUrl;
          AgentSurfaceState.CredentialLifetimeMinutes = XaiChatDefaults.CredentialLifetimeMinutes;
        }
        else
        {
          AgentSurfaceState.RecordChats = response.RecordChats;
          AgentSurfaceState.PrivacyNotice = response.PrivacyNotice;
          AgentSurfaceState.SupportUrl = AskSupportLink.Normalize(response.SupportUrl);
          AgentSurfaceState.CredentialLifetimeMinutes = response.CredentialLifetimeMinutes > 0
            ? response.CredentialLifetimeMinutes
            : XaiChatDefaults.CredentialLifetimeMinutes;
        }

        return Task.CompletedTask;
      }
    }
  }
}
