#region Purpose
// Verifies a merge-scoped passkey assertion and merges the credential's principal into the caller.
#endregion

#region Design
// Auth first, RP select, decode, consume Merge challenge, lookup by handle, Verify, then:
//   credential on caller → 409 Already on this account
//   source merged/quarantined → 403
//   else MergePrincipalAsync(source, caller) and audit both ids + credential id.
// Quarantine/merged check is post-Verify (same oracle posture as CompletePasskeyAuthentication).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Abstractions;
using static TimeWarp.Architecture.Features.Identity.CompleteAddExistingPasskey;

public sealed partial class CompleteAddExistingPasskey
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private static readonly Action<ILogger, WebAuthnFailureReason, string, Exception?> LogVerificationFailed =
      LoggerMessage.Define<WebAuthnFailureReason, string>
      (
        LogLevel.Information,
        new EventId(1, nameof(LogVerificationFailed)),
        "Add-existing-passkey verification failed: {FailureReason} (rpId {RelyingPartyId})"
      );

    private static readonly Action<ILogger, Guid, Guid, Guid, Exception?> LogMerged =
      LoggerMessage.Define<Guid, Guid, Guid>
      (
        LogLevel.Information,
        new EventId(2, nameof(LogMerged)),
        "Merged principal {SourcePrincipalId} into {TargetPrincipalId} via credential {CredentialId}"
      );

    private readonly IPrincipalStore PrincipalStore;
    private readonly IWebAuthnChallengeStore ChallengeStore;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IRequestHostAccessor RequestHostAccessor;
    private readonly IOptions<WebAuthnOptions> Options;
    private readonly ILogger<Handler> Logger;

    public Handler(
      IPrincipalStore principalStore,
      IWebAuthnChallengeStore challengeStore,
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IRequestHostAccessor requestHostAccessor,
      IOptions<WebAuthnOptions> options,
      ILogger<Handler> logger)
    {
      PrincipalStore = principalStore;
      ChallengeStore = challengeStore;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      RequestHostAccessor = requestHostAccessor;
      Options = options;
      Logger = logger;
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

      WebAuthnRelyingParty relyingParty = relyingPartyResult.AsT0;
      if (!WebAuthnPayloadDecoder.TryDecode(command.CredentialId, out byte[] credentialIdBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.ClientDataJson, out byte[] clientDataJsonBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.AuthenticatorData, out byte[] authenticatorDataBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.Signature, out byte[] signatureBytes))
      {
        return IdentityProblems.MalformedPayload("CredentialId, ClientDataJson, AuthenticatorData, and Signature");
      }

      if (!WebAuthnChallengeReader.TryReadChallenge(clientDataJsonBytes, out byte[] challenge)
        || !ChallengeStore.TryConsume(WebAuthnCeremonyType.Merge, challenge))
      {
        return IdentityProblems.ChallengeInvalid("merge");
      }

      Credential? credential = await PrincipalStore.FindCredentialByHandleAsync(CredentialType.Passkey, credentialIdBytes, cancellationToken);
      if (credential?.IsRevoked != false)
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

      if (credential.PrincipalId == callerId.Value)
      {
        return IdentityProblems.AlreadyOnThisAccount("passkey");
      }

      Principal? source = await PrincipalStore.GetPrincipalAsync(credential.PrincipalId, cancellationToken);
      if (source is null)
      {
        return IdentityProblems.AuthenticationFailed();
      }

      if (!source.IsActive || source.MergedIntoPrincipalId is not null)
      {
        return IdentityProblems.AccountNotMergeable();
      }

      IReadOnlyList<Credential> sourceActive =
        await PrincipalStore.ListCredentialsAsync(source.Id, includeRevoked: false, cancellationToken);
      int moved = sourceActive.Count;
      try
      {
        await PrincipalStore.MergePrincipalAsync(source.Id, callerId.Value, cancellationToken);
      }
      catch (InvalidOperationException)
      {
        return IdentityProblems.AccountNotMergeable();
      }
      catch (ConcurrencyConflictException)
      {
        return IdentityProblems.TooMuchContention();
      }

      LogMerged(Logger, source.Id.Value, callerId.Value.Value, credential.Id.Value, null);
      return new Response(moved, source.Id);
    }
  }
}
