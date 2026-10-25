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
using TimeWarp.Architecture.Features.AgentChats.Application;
using TimeWarp.Foundation.Features;
using static TimeWarp.Architecture.Features.Identity.CompleteAddExistingPasskey;

public sealed partial class CompleteAddExistingPasskey
{
  [CrossSliceReference(
    typeof(IUserXaiApiKey),
    "Retiring a principal by merge deletes that user's xAI API key row.")]
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
    private readonly IUserXaiApiKey? UserXaiApiKeys;

    public Handler(
      IPrincipalStore principalStore,
      IWebAuthnChallengeStore challengeStore,
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IRequestHostAccessor requestHostAccessor,
      IOptions<WebAuthnOptions> options,
      ILogger<Handler> logger,
      IUserXaiApiKey? userXaiApiKeys = null)
    {
      PrincipalStore = principalStore;
      ChallengeStore = challengeStore;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      RequestHostAccessor = requestHostAccessor;
      Options = options;
      Logger = logger;
      UserXaiApiKeys = userXaiApiKeys;
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

      await DeleteUserKeyAsync(source.Id.Value, cancellationToken).ConfigureAwait(false);
      LogMerged(Logger, source.Id.Value, callerId.Value.Value, credential.Id.Value, null);
      return new Response(moved, source.Id);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Design",
      "CA1031:Do not catch general exception types",
      Justification = "The merge already committed. A key-delete failure must not fail the retired principal.")]
    private async Task DeleteUserKeyAsync(Guid userId, CancellationToken cancellationToken)
    {
      if (UserXaiApiKeys is null)
      {
        return;
      }

      try
      {
        await UserXaiApiKeys.DeleteForPrincipalAsync(userId, cancellationToken).ConfigureAwait(false);
      }
      catch (Exception exception) when (exception is not OperationCanceledException)
      {
        _ = exception;
      }
    }
  }
}
