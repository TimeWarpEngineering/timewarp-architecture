#region Purpose
// Server-side handler for the StartPasskeyRegistration command: mints a challenge and returns
// WebAuthn creation options.
#endregion

#region Design
// user.id/name/displayName are per-ceremony and never persisted — see
// CompletePasskeyRegistration's Design region for why account resolution is credential-handle-based
// instead. No email/username collection, per the task requirement; "TimeWarp user" is a fixed
// placeholder the authenticator's own UI may show.
// Concurrency note: this handler makes zero IPrincipalStore calls (nothing to look up or persist
// yet — the ceremony has not produced a credential), so 104-028's Update*/ConcurrencyConflictException
// contract is simply not exercised here.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Security.Cryptography;
using static TimeWarp.Architecture.Features.Identity.StartPasskeyRegistration;

public sealed partial class StartPasskeyRegistration
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private const string PlaceholderUserName = "TimeWarp user";

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

      byte[] challenge = ChallengeStore.Issue(WebAuthnCeremonyType.Registration);

      // Opaque per-ceremony user.id — never persisted; account resolution is credential-handle-based.
      byte[] userHandle = RandomNumberGenerator.GetBytes(32);

      string optionsJson = WebAuthnRegistration.BuildOptionsJson(relyingParty, challenge, userHandle, PlaceholderUserName, PlaceholderUserName);

      return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new Response(optionsJson));
    }
  }
}
