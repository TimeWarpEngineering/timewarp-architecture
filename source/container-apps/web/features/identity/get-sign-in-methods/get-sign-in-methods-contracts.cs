#region Purpose
// Public list of sign-in methods the browser should render right now.
#endregion

#region Design
// Anonymous on purpose — the login page has no session. Response is the offered method list
// in enum order. Do not leak TenantId, AllowBootstrap, or PasskeyPromptMode. Passkey is offered
// when it is listed. Microsoft365 is offered when it is listed AND site settings
// EntraSignInEnabled is on, so the button never 404s and never shows when administrators
// disabled sign-in at runtime.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("Login page needs a public read of the offered sign-in methods; the payload is that list.")]
public static partial class GetSignInMethods
{
  [ApiRoute("api/identity/sign-in-methods", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    public IReadOnlyList<SignInMethod> Methods { get; }

    public Response(IReadOnlyList<SignInMethod> methods)
    {
      Methods = methods;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    _ => new Response([SignInMethod.Passkey]);
}
