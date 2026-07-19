#region Purpose
// Endpoint-centric contract for starting a WebAuthn passkey authentication ceremony.
#endregion

#region Design
// Empty body, same shape as StartPasskeyRegistration: the returned options carry an empty
// allowCredentials list (discoverable-credential-first — the platform's passkey UI lets the user
// pick, rather than the server pre-listing candidates, which would require knowing identity before
// authentication starts).
// No GetMockResponseFactory — see StartPasskeyRegistration's Design region.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static partial class StartPasskeyAuthentication
{
  [ApiRoute("api/identity/passkey/authenticate/options", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>;

  public sealed class Response
  {
    public string OptionsJson { get; }

    public Response(string optionsJson)
    {
      OptionsJson = Guard.Against.NullOrEmpty(optionsJson);
    }
  }
}
