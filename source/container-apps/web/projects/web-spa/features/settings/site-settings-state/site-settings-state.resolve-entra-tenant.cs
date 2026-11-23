#region Purpose
// ResolveEntraTenant: ask the server for a tenant id and record it for the page to apply.
#endregion

#region Design
// The server resolves the id. This handler never parses an issuer and never edits the
// authentication command. An invalid domain skips the HTTP call and sets
// AcceptedTenantsMessage. A problem is both the shell notification and that same message.
// Success stores ResolvedTenant and clears the message. TenantLookupCount advances on every
// completed attempt, including a skipped call, so the page can tell a new result from the
// previous one. The page merges the tenant into Command.EntraAcceptedTenants. No success
// toast: the new row is the confirmation.
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
          RecordLookup
          (
            tenant: null,
            message: shown.Length == 0
              ? "Enter a domain name."
              : $"'{shown}' is not a domain name."
          );
          return Task.FromResult<Query?>(null);
        }

        return Task.FromResult<Query?>(new Query { Domain = domain });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        RecordLookup(new EntraAcceptedTenant(response.TenantId, response.Domain), message: null);
        return Task.CompletedTask;
      }

      protected override Task HandleError(SharedProblemDetails problem, CancellationToken cancellationToken)
      {
        string? message = string.IsNullOrWhiteSpace(problem.Detail)
          ? problem.Title
          : $"{problem.Title}. {problem.Detail}";
        RecordLookup(tenant: null, message);
        return base.HandleError(problem, cancellationToken);
      }

      private void RecordLookup(EntraAcceptedTenant? tenant, string? message)
      {
        SiteSettingsState.ResolvedTenant = tenant;
        SiteSettingsState.AcceptedTenantsMessage = message;
        SiteSettingsState.TenantLookupCount++;
      }
    }
  }
}
