#region Purpose
// SetPrincipalRoles action: persists draft role multi-select for one principal.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using static SetPrincipalRoles;

partial class PrincipalState
{
  public static class ToggleRoleActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Guid PrincipalId { get; }
      public Guid RoleId { get; }

      public Action(Guid principalId, Guid roleId)
      {
        PrincipalId = principalId;
        RoleId = roleId;
      }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        if (!PrincipalState.DraftRoleIds.TryGetValue(action.PrincipalId, out HashSet<Guid>? set))
        {
          set = [];
          PrincipalState.DraftRoleIds[action.PrincipalId] = set;
        }

        if (!set.Add(action.RoleId))
        {
          set.Remove(action.RoleId);
        }

        return Task.CompletedTask;
      }
    }
  }

  public static class SetPrincipalRolesActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Guid PrincipalId { get; }

      public Action(Guid principalId)
      {
        PrincipalId = principalId;
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
      private Guid ActivePrincipalId;

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        ActivePrincipalId = action.PrincipalId;
        IReadOnlyCollection<Guid> draft = PrincipalState.GetDraftRoleIds(action.PrincipalId);
        return Task.FromResult<Command?>(new Command
        {
          UserId = Guid.NewGuid(),
          PrincipalId = action.PrincipalId,
          RoleIds = [.. draft]
        });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        PrincipalState.DraftRoleIds[ActivePrincipalId] = response.RoleIds.ToHashSet();
        if (PrincipalState.PrincipalsList is not null)
        {
          int index = PrincipalState.PrincipalsList.FindIndex(
            item => item.PrincipalId.Value == ActivePrincipalId);
          if (index >= 0)
          {
            ListPrincipals.PrincipalSummaryDto existing = PrincipalState.PrincipalsList[index];
            PrincipalState.PrincipalsList[index] = new ListPrincipals.PrincipalSummaryDto(
              existing.PrincipalId,
              existing.Kind,
              existing.TrustTier,
              existing.IsActive,
              existing.IsQuarantined,
              [.. response.RoleIds]);
          }
        }

        return Task.CompletedTask;
      }
    }
  }
}
