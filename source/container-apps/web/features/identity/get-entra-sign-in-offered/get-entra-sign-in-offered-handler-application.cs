#region Purpose
// Server-side handler for GetEntraSignInOffered: configuration scheme gate AND settings policy.
#endregion

#region Design
// options.Enabled is the scheme-registration gate; settings.EntraSignInEnabled is the runtime
// offer. Both must be true or the login button would 404 (no scheme) or 403 (policy). The
// registered store seeds on first read (SeedOnReadSiteSettingsStore, task 254), so an emptied
// table is re-seeded from configuration here; a null read (table not migrated) is not offered.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Options;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Identity.GetEntraSignInOffered;

public sealed class GetEntraSignInOffered
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IOptions<EntraAuthenticationOptions> Options;
    private readonly ISiteSettingsStore SiteSettingsStore;

    public Handler(
      IOptions<EntraAuthenticationOptions> options,
      ISiteSettingsStore siteSettingsStore)
    {
      Options = options;
      SiteSettingsStore = siteSettingsStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
      CancellationToken cancellationToken)
    {
      _ = request;
      SiteSettings? settings = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
      bool offered = Options.Value.Enabled && settings is { EntraSignInEnabled: true };
      return new Response(offered);
    }
  }
}
