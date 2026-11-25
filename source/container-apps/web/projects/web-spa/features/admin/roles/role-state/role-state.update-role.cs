#region Purpose
// UpdateRole action set: sends the UpdateRole command and refreshes the stored role response.
#endregion

#region Design
// Action ctor takes UpdateRole.Command so the dispatch method is RoleState.UpdateRole(Command).
// RoleEditForm binds IRoleDetails (the Command) and submits via that method.
// UpdateRole.Response is empty, so success builds a new GetRole.Response from the command.
// Submitted is the command GetRequest built and sent. The form's command stays editable
// while the request is in flight, so HandleSuccess reads the sent copy.
// When the stored snapshot is that role, its permission and agent-scope lists are kept.
// A different id starts those lists empty because this response does not carry them.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static UpdateRole;

partial class RoleState
{
  public static class UpdateRoleActionSet
  {
    [CatalogAction
    (
      Description = "Replace a role's name and description.",
      Permissions = [PermissionIds.AdminRolesManage],
      Visibility = ActionVisibility.Agent
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Command Command { get; }

      public Action(Command command)
      {
        Command = command;
      }
    }

    internal class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher
    ) : DefaultApiHandler<Action, Command, Response>(store, webServerApiService, logger, publisher)
    {
      private Command? Submitted { get; set; }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        Command submitted = new()
        {
          RoleId = action.Command.RoleId,
          Name = action.Command.Name,
          Description = action.Command.Description
        };
        Submitted = submitted;
        return Task.FromResult<Command?>(submitted);
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = response;
        _ = cancellationToken;
        if (Submitted is not Command submitted)
        {
          return Task.CompletedTask;
        }

        GetRole.Response? previous = RoleState.RoleSnapshot;
        List<string> permissions = [];
        List<string> agentScopes = [];
        if (previous is not null && previous.RoleId == submitted.RoleId)
        {
          permissions = [.. previous.Permissions];
          agentScopes = [.. previous.AgentScopes];
        }

        RoleState.RoleSnapshot = new GetRole.Response
        (
          roleId: submitted.RoleId,
          name: submitted.Name,
          description: submitted.Description,
          permissions: permissions,
          agentScopes: agentScopes
        );
        return Task.CompletedTask;
      }
    }
  }
}
