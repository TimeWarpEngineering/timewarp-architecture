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
        Apply(ChatReadinessProbe.FromResponse(response));

      protected override Task HandleFileResponse(FileResponse fileResponse, CancellationToken cancellationToken) =>
        Apply(ChatReadinessProbe.FromFileResponse());

      protected override Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken) =>
        Apply(ChatReadinessProbe.FromProblem(problemDetails));

      private Task Apply(ChatProbeResult result)
      {
        AgentSurfaceState.ChatReadiness = result.Readiness;
        AgentSurfaceState.ChatSetupCommand = result.SetupCommand;
        AgentSurfaceState.ChatModel = result.Model;
        AgentSurfaceState.ChatProblem = result.Problem;
        return Task.CompletedTask;
      }
    }
  }
}
