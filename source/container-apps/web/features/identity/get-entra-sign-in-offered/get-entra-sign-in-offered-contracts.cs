#region Purpose
// Public boolean: whether the login page should offer Continue with Microsoft 365.
#endregion

#region Design
// Anonymous on purpose — the login page has no session. Response is Offered only; do not
// leak TenantId, AllowBootstrap, or PasskeyPromptMode. Offered is scheme registered
// (configuration Enabled) AND site settings EntraSignInEnabled so the button never 404s and
// never shows when administrators disabled sign-in at runtime.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("Login page needs a public read of whether Entra sign-in is offered; the payload is a single boolean.")]
public static partial class GetEntraSignInOffered
{
  [ApiRoute("api/identity/entra/offered", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    public bool Offered { get; }

    public Response(bool offered)
    {
      Offered = offered;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    _ => new Response(offered: false);
}
