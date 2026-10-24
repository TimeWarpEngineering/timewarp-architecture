#region Purpose
// Server-side handler for GetEntraSignInOffered: the methods the browser should render.
#endregion

#region Design
// Passkey is included when it is listed. Microsoft365 uses EntraSignInOffer, the same rule
// GetCredentials uses for CanLinkMicrosoft365 (task 282). The list is enum order: Passkey, then
// Microsoft365.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Options;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Identity.GetEntraSignInOffered;

public sealed class GetEntraSignInOffered
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IOptions<SignInOptions> SignInOptions;
    private readonly ISiteSettingsStore SiteSettingsStore;

    public Handler(
      IOptions<SignInOptions> signInOptions,
      ISiteSettingsStore siteSettingsStore)
    {
      SignInOptions = signInOptions;
      SiteSettingsStore = siteSettingsStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
      CancellationToken cancellationToken)
    {
      _ = request;
      List<SignInMethod> methods = [];
      if (SignInOptions.Value.Lists(SignInMethod.Passkey))
      {
        methods.Add(SignInMethod.Passkey);
      }

      if (await EntraSignInOffer.IsOfferedAsync(SignInOptions.Value, SiteSettingsStore, cancellationToken).ConfigureAwait(false))
      {
        methods.Add(SignInMethod.Microsoft365);
      }

      return new Response(methods);
    }
  }
}
