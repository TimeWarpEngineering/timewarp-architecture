#region Purpose
// Server-side handler for GetEntraSignInOffered: configuration scheme gate AND settings policy.
#endregion

#region Design
// The rule (scheme registered AND site policy on) is EntraSignInOffer, shared with GetCredentials,
// which sets its CanLinkMicrosoft365 flag from the same answer (task 282).
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
      return new Response(await EntraSignInOffer.IsOfferedAsync(Options.Value, SiteSettingsStore, cancellationToken).ConfigureAwait(false));
    }
  }
}
