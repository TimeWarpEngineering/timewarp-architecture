#region Purpose
// Verifies a passkey assertion, consumes parked Entra claims, attaches Entra to that principal.
#endregion

#region Design
// Same decode → consume Authentication challenge → lookup → verify → IsActive order as
// CompletePasskeyAuthentication. Park is consumed only after the assertion succeeds so a failed
// ceremony does not burn the choose ticket. Attach uses link semantics (409 if the handle is
// owned elsewhere). Issues identity-session on the passkey principal.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Abstractions;
using static TimeWarp.Architecture.Features.Identity.CompleteEntraBootstrapExisting;

public sealed partial class CompleteEntraBootstrapExisting
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private static readonly Action<ILogger, WebAuthnFailureReason, string, Exception?> LogVerificationFailed =
      LoggerMessage.Define<WebAuthnFailureReason, string>
      (
        LogLevel.Information,
        new EventId(1, nameof(LogVerificationFailed)),
        "Entra choose-existing passkey verification failed: {FailureReason} (rpId {RelyingPartyId})"
      );

    private readonly IPrincipalStore PrincipalStore;
    private readonly IWebAuthnChallengeStore ChallengeStore;
    private readonly IParkedEntraClaimsStore ParkedStore;
    private readonly EntraTicketProcessor Processor;
    private readonly IBrowserSessionService BrowserSessionService;
    private readonly IRequestHostAccessor RequestHostAccessor;
    private readonly IOptions<WebAuthnOptions> Options;
    private readonly IEntraChoiceTicketAccessor ChoiceTicketAccessor;
    private readonly ILogger<Handler> Logger;

    public Handler(
      IPrincipalStore principalStore,
      IWebAuthnChallengeStore challengeStore,
      IParkedEntraClaimsStore parkedStore,
      EntraTicketProcessor processor,
      IBrowserSessionService browserSessionService,
      IRequestHostAccessor requestHostAccessor,
      IOptions<WebAuthnOptions> options,
      IEntraChoiceTicketAccessor choiceTicketAccessor,
      ILogger<Handler> logger)
    {
      PrincipalStore = principalStore;
      ChallengeStore = challengeStore;
      ParkedStore = parkedStore;
      Processor = processor;
      BrowserSessionService = browserSessionService;
      RequestHostAccessor = requestHostAccessor;
      Options = options;
      ChoiceTicketAccessor = choiceTicketAccessor;
      Logger = logger;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      if (!ChoiceTicketAccessor.TryReadParkId(out string parkId)
        || !ParkedStore.TryGet(parkId, out ParkedEntraClaims? parkedPeek)
        || parkedPeek is null)
      {
        return IdentityProblems.EntraChoiceExpired();
      }

      OneOf<WebAuthnRelyingParty, SharedProblemDetails> relyingPartyResult =
        WebAuthnRelyingPartySelection.Select(RequestHostAccessor.GetRequestHost(), Options.Value);
      if (relyingPartyResult.IsT1)
      {
        return relyingPartyResult.AsT1;
      }

      WebAuthnRelyingParty relyingParty = relyingPartyResult.AsT0;
      if (!WebAuthnPayloadDecoder.TryDecode(command.CredentialId, out byte[] credentialIdBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.ClientDataJson, out byte[] clientDataJsonBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.AuthenticatorData, out byte[] authenticatorDataBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.Signature, out byte[] signatureBytes))
      {
        return IdentityProblems.MalformedPayload("CredentialId, ClientDataJson, AuthenticatorData, and Signature");
      }

      if (!WebAuthnChallengeReader.TryReadChallenge(clientDataJsonBytes, out byte[] challenge)
        || !ChallengeStore.TryConsume(WebAuthnCeremonyType.Authentication, challenge))
      {
        return IdentityProblems.ChallengeInvalid("authentication");
      }

      Credential? credential = await PrincipalStore.FindCredentialByHandleAsync(CredentialType.Passkey, credentialIdBytes, cancellationToken);
      if (credential?.IsRevoked != false)
      {
        return IdentityProblems.AuthenticationFailed();
      }

      Principal? principal = await PrincipalStore.GetPrincipalAsync(credential.PrincipalId, cancellationToken);
      if (principal is null)
      {
        return IdentityProblems.AuthenticationFailed();
      }

      WebAuthnAssertionResult verifyResult =
        WebAuthnAuthentication.Verify(relyingParty, challenge, credential.PublicMaterial, clientDataJsonBytes, authenticatorDataBytes, signatureBytes);
      if (!verifyResult.IsValid)
      {
        LogVerificationFailed(Logger, verifyResult.FailureReason, relyingParty.Id, null);
        return IdentityProblems.AuthenticationFailed();
      }

      if (!principal.IsActive)
      {
        return IdentityProblems.Quarantined();
      }

      if (!ParkedStore.TryConsume(parkId, out ParkedEntraClaims? parked) || parked is null)
      {
        return IdentityProblems.EntraChoiceExpired();
      }

      ChoiceTicketAccessor.Clear();
      OneOf<PrincipalId, SharedProblemDetails> attached =
        await Processor.AttachEntraToPrincipalAsync(parked.Claims, principal.Id, cancellationToken);
      if (attached.IsT1)
      {
        return attached.AsT1;
      }

      await BrowserSessionService.IssueAsync(principal.Id, principal.DisplayName, cancellationToken);
      return new Response(principal.Id, parked.Destination);
    }
  }
}
