#region Purpose
// Server-side handler for the StartPasskeyRegistration command: mints a challenge and returns
// WebAuthn creation options named for the account the passkey will belong to.
#endregion

#region Design
// user.id is per-ceremony and never persisted — see CompletePasskeyRegistration's Design region for
// why account resolution is credential-handle-based instead. No email/username collection.
// user.name = user.displayName = PasskeyAccountName ("TimeWarp account · <account fingerprint>",
// task 253) — the authenticator stores it at creation and never lets it change, so it must name
// the right account up front:
//   - New account (ForCurrentAccount=false): the PrincipalId CompletePasskeyRegistration will mint
//     is pre-allocated HERE and kept with the challenge (IWebAuthnChallengeStore.Issue(type,
//     pendingPrincipalId)) — server-side only, never in the options JSON or any response, so a
//     client cannot supply or swap it. A challenge that is never completed takes the id with it on
//     expiry/eviction; nothing persistent is allocated. The id is allocated even when a session
//     cookie is present — this path always means "new account".
//   - Current account (ForCurrentAccount=true, Settings → AddPasskey): the name is the signed-in
//     caller's (IBrowserSessionService — the endpoint is anonymous, so the cookie is read
//     explicitly, same as GetCurrentSession). No pending id is recorded, so Complete refuses this
//     challenge: a new account must never carry another account's name. No session → 401 before
//     any challenge is issued.
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
    private readonly IWebAuthnChallengeStore ChallengeStore;
    private readonly IBrowserSessionService BrowserSessionService;
    private readonly IRequestHostAccessor RequestHostAccessor;
    private readonly IOptions<WebAuthnOptions> Options;

    public Handler
    (
      IWebAuthnChallengeStore challengeStore,
      IBrowserSessionService browserSessionService,
      IRequestHostAccessor requestHostAccessor,
      IOptions<WebAuthnOptions> options
    )
    {
      ChallengeStore = challengeStore;
      BrowserSessionService = browserSessionService;
      RequestHostAccessor = requestHostAccessor;
      Options = options;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      PrincipalId? callerId = null;
      if (command.ForCurrentAccount)
      {
        callerId = await BrowserSessionService.GetCurrentPrincipalIdAsync(cancellationToken);
        if (callerId is null)
        {
          return IdentityProblems.Unauthenticated();
        }
      }

      // Select the RP ID before issuing — a disallowed host must never burn a challenge (task 104-031).
      OneOf<WebAuthnRelyingParty, SharedProblemDetails> relyingPartyResult =
        WebAuthnRelyingPartySelection.Select(RequestHostAccessor.GetRequestHost(), Options.Value);
      if (relyingPartyResult.IsT1)
      {
        return relyingPartyResult.AsT1;
      }

      WebAuthnRelyingParty relyingParty = relyingPartyResult.AsT0;

      // New account: pre-allocate the id Complete will mint, kept only with the challenge.
      PrincipalId? pendingPrincipalId = callerId is null ? PrincipalId.New() : null;
      PrincipalId namedPrincipalId = callerId ?? pendingPrincipalId!.Value;

      byte[] challenge = ChallengeStore.Issue(WebAuthnCeremonyType.Registration, pendingPrincipalId);

      // Opaque per-ceremony user.id — never persisted; account resolution is credential-handle-based.
      byte[] userHandle = RandomNumberGenerator.GetBytes(32);

      string accountName = PasskeyAccountName.For(namedPrincipalId);
      string optionsJson = WebAuthnRegistration.BuildOptionsJson(relyingParty, challenge, userHandle, accountName, accountName);

      return new Response(optionsJson);
    }
  }
}
