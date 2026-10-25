#region Purpose
// Remove one accepted tenant from the unsaved draft list.
#endregion

#region Design
// Keyed by tenant id. The saved snapshot is unchanged until UpdateSiteSettings.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

partial class SiteSettingsState
{
  public static class RemoveAcceptedTenantActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(Guid tenantId)
      {
        TenantId = tenantId;
      }

      public Guid TenantId { get; }
    }

    internal sealed class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store)
      {
      }

      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        SiteSettingsState.DraftAcceptedTenants = SiteSettingsState.DraftAcceptedTenants
          .Where(tenant => tenant.TenantId != action.TenantId)
          .ToArray();
        SiteSettingsState.AcceptedTenantsMessage = null;
        return ValueTask.CompletedTask;
      }
    }
  }
}
