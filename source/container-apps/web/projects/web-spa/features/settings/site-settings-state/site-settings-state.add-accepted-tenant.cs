#region Purpose
// Add a known Entra tenant id to the draft list, with no domain.
#endregion

#region Design
// A pasted GUID has no domain. Normalization is the same EntraTenantIds.TryNormalize the
// save command uses, so an empty GUID or a duplicate is a message and the list stays.
// Not a catalog action: the domain lookup is the agent-facing edit.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

partial class SiteSettingsState
{
  public static class AddAcceptedTenantActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(string tenantIdText)
      {
        TenantIdText = tenantIdText;
      }

      public string TenantIdText { get; }
    }

    internal sealed class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store)
      {
      }

      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        string shown = action.TenantIdText?.Trim() ?? "";
        if (!Guid.TryParse(shown, out Guid tenantId))
        {
          SiteSettingsState.AcceptedTenantsMessage = $"'{shown}' is not a tenant id.";
          return ValueTask.CompletedTask;
        }

        List<EntraAcceptedTenant> next =
        [
          .. SiteSettingsState.DraftAcceptedTenants,
          EntraAcceptedTenant.IdOnly(tenantId),
        ];
        if (!EntraTenantIds.TryNormalize(next, out IReadOnlyList<EntraAcceptedTenant> normalized, out string? error))
        {
          SiteSettingsState.AcceptedTenantsMessage = error;
          return ValueTask.CompletedTask;
        }

        SiteSettingsState.DraftAcceptedTenants = normalized;
        SiteSettingsState.AcceptedTenantsMessage = null;
        return ValueTask.CompletedTask;
      }
    }
  }
}
