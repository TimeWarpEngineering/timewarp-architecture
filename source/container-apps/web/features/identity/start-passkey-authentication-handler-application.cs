#region Purpose
// Server-side handler for the StartPasskeyAuthentication command: mints a challenge and returns
// WebAuthn assertion options.
#endregion

#region Design
// allowCredentials is always empty (discoverable-credential-first, see
// WebAuthnAuthentication.BuildOptionsJson's Design region) — this handler never looks up a
// principal or credential to build options, so there is nothing IPrincipalStore-related to note.
// RP-ID selection (task 104-031): the relying party is chosen per request from the request host via
// WebAuthnRelyingPartySelection.Select, run BEFORE ChallengeStore.Issue so a host outside the
// allowlist returns the 400 "Host not allowed" problem without issuing (and wasting) a challenge.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.StartPasskeyAuthentication;

public sealed partial class StartPasskeyAuthentication
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
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

      byte[] challenge = ChallengeStore.Issue(WebAuthnCeremonyType.Authentication);
      string optionsJson = WebAuthnAuthentication.BuildOptionsJson(relyingParty, challenge);

      return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new Response(optionsJson));
    }
  }
}
