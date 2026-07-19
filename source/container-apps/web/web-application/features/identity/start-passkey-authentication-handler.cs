#region Purpose
// Server-side handler for the StartPasskeyAuthentication command: mints a challenge and returns
// WebAuthn assertion options.
#endregion

#region Design
// allowCredentials is always empty (discoverable-credential-first, see
// WebAuthnAuthentication.BuildOptionsJson's Design region) — this handler never looks up a
// principal or credential to build options, so there is nothing IPrincipalStore-related to note.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.StartPasskeyAuthentication;

public sealed partial class StartPasskeyAuthentication
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IWebAuthnChallengeStore ChallengeStore;
    private readonly IOptions<WebAuthnOptions> Options;

    public Handler(IWebAuthnChallengeStore challengeStore, IOptions<WebAuthnOptions> options)
    {
      ChallengeStore = challengeStore;
      Options = options;
    }

    public Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      WebAuthnOptions webAuthnOptions = Options.Value;
      var relyingParty = new WebAuthnRelyingParty(webAuthnOptions.RpId, webAuthnOptions.RpName, webAuthnOptions.AllowedOrigins);

      byte[] challenge = ChallengeStore.Issue(WebAuthnCeremonyType.Authentication);
      string optionsJson = WebAuthnAuthentication.BuildOptionsJson(relyingParty, challenge);

      return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new Response(optionsJson));
    }
  }
}
