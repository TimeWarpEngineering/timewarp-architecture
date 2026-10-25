#region Purpose
// ResolveEntraTenant: ask the server for a tenant id and append it to the draft list.
#endregion

#region Design
// The server resolves the id. This handler never parses an issuer. An invalid domain skips
// the HTTP call and sets AcceptedTenantsMessage. A problem is both the shell notification
// and that same message. A tenant id already on the draft is reported as already listed
// under its stored domain, and the list is left as it is. A new id is appended through
// EntraTenantIds.TryNormalize. No success toast: the new row is the confirmation.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;
using static ResolveEntraTenant;

partial class SiteSettingsState
{
  public static class ResolveEntraTenantActionSet
  {
    [CatalogAction
    (
      Description = "Look up an Entra tenant id from a customer's primary domain.",
      Permissions = [PermissionIds.SettingsWrite],
      Visibility = ActionVisibility.Agent
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(string domain)
      {
        Domain = domain;
      }

      public string Domain { get; }
    }

    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
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

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        if (!EntraTenantIds.TryNormalizeDomain(action.Domain, out string? domain) || domain is null)
        {
          string shown = action.Domain?.Trim() ?? "";
          SiteSettingsState.AcceptedTenantsMessage = shown.Length == 0
            ? "Enter a domain name."
            : $"'{shown}' is not a domain name.";
          return Task.FromResult<Query?>(null);
        }

        return Task.FromResult<Query?>(new Query { Domain = domain });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        foreach (EntraAcceptedTenant entry in SiteSettingsState.DraftAcceptedTenants)
        {
          if (entry.TenantId != response.TenantId)
          {
            continue;
          }

          SiteSettingsState.AcceptedTenantsMessage = entry.Domain is null
            ? $"Tenant {entry.TenantId:D} is already listed."
            : $"Tenant {entry.TenantId:D} is already listed under {entry.Domain}.";
          return Task.CompletedTask;
        }

        List<EntraAcceptedTenant> next =
        [
          .. SiteSettingsState.DraftAcceptedTenants,
          new EntraAcceptedTenant(response.TenantId, response.Domain),
        ];
        if (!EntraTenantIds.TryNormalize(next, out IReadOnlyList<EntraAcceptedTenant> normalized, out string? error))
        {
          SiteSettingsState.AcceptedTenantsMessage = error;
          return Task.CompletedTask;
        }

        SiteSettingsState.DraftAcceptedTenants = normalized;
        SiteSettingsState.AcceptedTenantsMessage = null;
        return Task.CompletedTask;
      }

      protected override Task HandleError(SharedProblemDetails problem, CancellationToken cancellationToken)
      {
        SiteSettingsState.AcceptedTenantsMessage = string.IsNullOrWhiteSpace(problem.Detail)
          ? problem.Title
          : $"{problem.Title}. {problem.Detail}";
        return base.HandleError(problem, cancellationToken);
      }
    }
  }
}
