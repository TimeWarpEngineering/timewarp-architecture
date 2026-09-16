#region Purpose
// Mints a Merge-scoped WebAuthn assertion challenge for add-existing-passkey.
#endregion

#region Design
// Auth first so an anonymous caller never burns a challenge. RP select next (task 104-031).
// Issue(WebAuthnCeremonyType.Merge) — not Authentication — so login complete cannot consume it.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using TimeWarp.Architecture.Abstractions;
using static TimeWarp.Architecture.Features.Identity.StartAddExistingPasskey;

public sealed partial class StartAddExistingPasskey
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IWebAuthnChallengeStore ChallengeStore;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IRequestHostAccessor RequestHostAccessor;
    private readonly IOptions<WebAuthnOptions> Options;

    public Handler(
      IWebAuthnChallengeStore challengeStore,
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IRequestHostAccessor requestHostAccessor,
      IOptions<WebAuthnOptions> options)
    {
      ChallengeStore = challengeStore;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      RequestHostAccessor = requestHostAccessor;
      Options = options;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      PrincipalId? callerId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken);
      if (callerId is null)
      {
        return IdentityProblems.Unauthenticated();
      }

      OneOf<WebAuthnRelyingParty, SharedProblemDetails> relyingPartyResult =
        WebAuthnRelyingPartySelection.Select(RequestHostAccessor.GetRequestHost(), Options.Value);
      if (relyingPartyResult.IsT1)
      {
        return relyingPartyResult.AsT1;
      }

      byte[] challenge = ChallengeStore.Issue(WebAuthnCeremonyType.Merge);
      string optionsJson = WebAuthnAuthentication.BuildOptionsJson(relyingPartyResult.AsT0, challenge);
      return new Response(optionsJson);
    }
  }
}
