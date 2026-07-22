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
// RP-ID selection (task 104-031): the relying party is chosen per request from the request host via
// WebAuthnRelyingPartySelection.Select, run BEFORE ChallengeStore.Issue so a host outside the
// allowlist returns the 400 "Host not allowed" problem without issuing (and wasting) a challenge.
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
    private readonly IRequestHostAccessor RequestHostAccessor;
    private readonly IOptions<WebAuthnOptions> Options;

    public Handler(IWebAuthnChallengeStore challengeStore, IRequestHostAccessor requestHostAccessor, IOptions<WebAuthnOptions> options)
    {
      ChallengeStore = challengeStore;
      RequestHostAccessor = requestHostAccessor;
      Options = options;
    }

    public Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      // Select the RP ID FIRST — a disallowed host must never burn a challenge (task 104-031).
      OneOf<WebAuthnRelyingParty, SharedProblemDetails> relyingPartyResult =
        WebAuthnRelyingPartySelection.Select(RequestHostAccessor.GetRequestHost(), Options.Value);
      if (relyingPartyResult.IsT1)
      {
        return Task.FromResult<OneOf<Response, SharedProblemDetails>>(relyingPartyResult.AsT1);
      }

      WebAuthnRelyingParty relyingParty = relyingPartyResult.AsT0;

      byte[] challenge = ChallengeStore.Issue(WebAuthnCeremonyType.Registration);

      // Opaque per-ceremony user.id — never persisted; account resolution is credential-handle-based.
      byte[] userHandle = RandomNumberGenerator.GetBytes(32);

      string optionsJson = WebAuthnRegistration.BuildOptionsJson(relyingParty, challenge, userHandle, PlaceholderUserName, PlaceholderUserName);

      return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new Response(optionsJson));
    }
  }
}
