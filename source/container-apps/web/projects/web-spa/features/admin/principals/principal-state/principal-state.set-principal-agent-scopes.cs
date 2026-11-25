#region Purpose
// SetPrincipalAgentScopes action: persists the agent-scope list the page built.
#endregion

#region Design
// The page owns the checkbox working copy and passes Scopes on the command. Success does
// not notify the session — agent scopes are not human permission claims. The page reopens
// the principal only after LastSetPrincipalAgentScopesSucceeded so a failed write keeps
// the page's set.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using static SetPrincipalAgentScopes;

partial class PrincipalState
{
  public static class SetPrincipalAgentScopesActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Command Command { get; }

      public Action(Command command)
      {
        Command = command;
      }
    }

    internal class Handler : DefaultApiHandler<Action, Command, Response>
    {
      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher)
        : base(store, webServerApiService, logger, publisher)
      {
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        PrincipalState.LastSetPrincipalAgentScopesSucceeded = false;
        return Task.FromResult<Command?>(new Command
        {
          PrincipalId = action.Command.PrincipalId,
          Scopes = [.. action.Command.Scopes]
        });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = response;
        _ = cancellationToken;
        PrincipalState.LastSetPrincipalAgentScopesSucceeded = true;
        return Task.CompletedTask;
      }
    }
  }
}
