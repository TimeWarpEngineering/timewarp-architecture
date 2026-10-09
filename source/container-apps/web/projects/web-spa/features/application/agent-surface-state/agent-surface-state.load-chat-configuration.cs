#region Purpose
// Loads whether the server can call xAI. A failure still finishes the probe so Ask can render.
#endregion

#region Design
// Not a DefaultApiHandler: that base publishes ProblemDetailsNotification, and a 401 or a
// missing key must not toast. HandleError records NotConfigured and the constant setup command.
// The shell dispatches this once. It does not check AuthenticationStateProvider first — an
// anonymous skip would leave the probe Unknown forever.
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

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        AgentSurfaceState.ChatReadiness = response.Configured
          ? CatalogAgentReadiness.Configured
          : CatalogAgentReadiness.NotConfigured;
        AgentSurfaceState.ChatSetupCommand = string.IsNullOrWhiteSpace(response.SetupCommand)
          ? XaiChatDefaults.SetupCommand
          : response.SetupCommand;
        AgentSurfaceState.ChatModel = response.Model;
        AgentSurfaceState.ChatProbeCompleted = true;
        AgentSurfaceState.RecordChats = response.RecordChats;
        AgentSurfaceState.PrivacyNotice = response.PrivacyNotice;
        AgentSurfaceState.SupportUrl = string.IsNullOrWhiteSpace(response.SupportUrl)
          ? XaiChatDefaults.SupportUrl
          : response.SupportUrl;
        AgentSurfaceState.CredentialLifetimeMinutes = response.CredentialLifetimeMinutes > 0
          ? response.CredentialLifetimeMinutes
          : XaiChatDefaults.CredentialLifetimeMinutes;
        return Task.CompletedTask;
      }

      protected override Task HandleFileResponse(FileResponse fileResponse, CancellationToken cancellationToken)
      {
        return MarkNotConfigured();
      }

      protected override Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
      {
        return MarkNotConfigured();
      }

      private Task MarkNotConfigured()
      {
        AgentSurfaceState.ChatReadiness = CatalogAgentReadiness.NotConfigured;
        AgentSurfaceState.ChatSetupCommand = XaiChatDefaults.SetupCommand;
        AgentSurfaceState.ChatModel = null;
        AgentSurfaceState.ChatProbeCompleted = true;
        AgentSurfaceState.RecordChats = false;
        AgentSurfaceState.PrivacyNotice = XaiChatDefaults.PrivacyNotice;
        AgentSurfaceState.SupportUrl = XaiChatDefaults.SupportUrl;
        AgentSurfaceState.CredentialLifetimeMinutes = XaiChatDefaults.CredentialLifetimeMinutes;
        return Task.CompletedTask;
      }
    }
  }
}
