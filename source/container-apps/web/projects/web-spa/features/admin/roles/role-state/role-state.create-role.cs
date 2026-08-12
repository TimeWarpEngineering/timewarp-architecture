#region Purpose
// CreateRole action set: sends the CreateRole command to the Web API and records the result.
#endregion

#region Design
// Action ctor takes CreateRole.Command so the generator emits RoleState.CreateRole(Command).
// RoleForm binds IRoleDetails (the Command) and submits via that method — COPIC
// EducationHistorySearchForm, not Mediator.Send of a page-owned Action.
// UserId is IAuthApiRequest mock-mode only; stamp it here so the form never sees it
// (same as FetchRoles GetRequest).
// LastCreatedRoleId is recorded on success so a page can confirm the create round-trip.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static CreateRole;

partial class RoleState
{
  internal static class CreateRoleActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
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
      ISender sender,
      ILogger<Handler> logger
    ) : DefaultApiHandler<Action, Command, Response>(store, webServerApiService, sender, logger)
    {
      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Command?>(new Command
        {
          UserId = Guid.NewGuid(),
          Name = action.Command.Name,
          Description = action.Command.Description
        });

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        RoleState.LastCreatedRoleId = response.RoleId;
        return Task.CompletedTask;
      }
    }
  }
}
