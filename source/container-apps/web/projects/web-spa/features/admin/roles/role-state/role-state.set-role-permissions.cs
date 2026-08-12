#region Purpose
// SetRolePermissions action: persists draft permission multi-select for one role.
#endregion

#region Design
// Task 182-004: mirrors PrincipalState.SetPrincipalRoles — draft toggles are pure state;
// Save posts SetRolePermissions then re-fetches GetRoles so drafts re-seed from stored grants
// (and protected-core / validation errors surface via DefaultApiHandler problem handling).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static SetRolePermissions;

partial class RoleState
{
  public static class SetPermissionSelectedActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Guid RoleId { get; }
      public string PermissionId { get; }
      public bool Selected { get; }

      public Action(Guid roleId, string permissionId, bool selected)
      {
        RoleId = roleId;
        PermissionId = permissionId;
        Selected = selected;
      }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        if (!RoleState.DraftPermissionIds.TryGetValue(action.RoleId, out HashSet<string>? set))
        {
          set = new HashSet<string>(StringComparer.Ordinal);
          RoleState.DraftPermissionIds[action.RoleId] = set;
        }

        if (action.Selected)
        {
          set.Add(action.PermissionId);
        }
        else
        {
          set.Remove(action.PermissionId);
        }

        return Task.CompletedTask;
      }
    }
  }

  public static class SetRolePermissionsActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Guid RoleId { get; }

      public Action(Guid roleId)
      {
        RoleId = roleId;
      }
    }

    internal class Handler : DefaultApiHandler<Action, Command, Response>
    {
      private readonly ISender MediatorSender;

      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ISender sender,
        ILogger<Handler> logger)
        : base(store, webServerApiService, sender, logger)
      {
        MediatorSender = sender;
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        IReadOnlyCollection<string> draft = RoleState.GetDraftPermissionIds(action.RoleId);
        return Task.FromResult<Command?>(new Command
        {
          UserId = Guid.NewGuid(),
          RoleId = action.RoleId,
          PermissionIds = [.. draft]
        });
      }

      protected override async Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        // Re-list so drafts match stored grants (and any server-side normalization).
        await MediatorSender.Send(new FetchRolesActionSet.Action(), cancellationToken);
      }
    }
  }
}
