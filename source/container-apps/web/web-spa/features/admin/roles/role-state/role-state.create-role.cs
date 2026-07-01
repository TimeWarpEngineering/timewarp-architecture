namespace TimeWarp.Architecture.Features.Admin.Roles;

using static CreateRole;

partial class RoleState
{
  internal static class CreateRoleActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      // The form binds directly to this Command (via IRoleDetails) and dispatches this same
      // instance on submit — so the user's edits reach the handler.
      public Command Command { get; } = new();
    }

    internal class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ISender sender,
      ILogger<Handler> logger
    ) : DefaultApiHandler<Action, Command, Response>(store, webServerApiService, sender, logger)
    {
      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Command?>(action.Command);

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        RoleState.LastCreatedRoleId = response.RoleId;
        return Task.CompletedTask;
      }
    }
  }
}
