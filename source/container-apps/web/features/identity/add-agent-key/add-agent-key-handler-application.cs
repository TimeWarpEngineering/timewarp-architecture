#region Purpose
// Server-side handler for the AddAgentKey command: verifies the agent's proof of possession and
// attaches a new Credential to the CALLER's EXISTING principal (never mints a new one) — also how
// agent key ROTATION works, paired with a subsequent RevokeCredential of the old key.
#endregion

#region Design
// Auth first (ICurrentPrincipalAccessor) before AgentKeyRegistrationCeremony — an unauthenticated
// caller must never burn a challenge. Ceremony preamble (decode → consume → TryParse → verify →
// handle-exists) lives in AgentKeyRegistrationCeremony; ordering rationale is owned there.
// Differs from CompleteAgentKeyRegistration: principal id comes from the authenticated caller
// (never Principal.Create), and there is no session/token issuance side effect — adding a key does
// not itself mint a bearer token; the caller requests one separately via CompleteAgentTokenIssuance
// using the new KeyId this response returns.
// No orphan-Principal residual: this handler never calls AddPrincipalAsync, so a lost same-handle
// race has nothing left over to compensate.
// No PrincipalKind vs CredentialType affinity check (Wave-1, deliberate): nothing in the domain model
// ties Kind to CredentialType; a Human principal COULD end up with an AgentKey via this endpoint.
// If a product need for kind/credential affinity emerges, it belongs on Principal/Credential, not
// bolted onto this handler. Zero Update* calls (Add* only).
// Round-1 M5: same AgentKeyCeremonyType.Registration as Complete/Start (see ceremony Design).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Buffers.Text;
using static TimeWarp.Architecture.Features.Identity.AddAgentKey;

public sealed partial class AddAgentKey
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IAgentKeyChallengeStore ChallengeStore;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;

    public Handler
    (
      IPrincipalStore principalStore,
      IAgentKeyChallengeStore challengeStore,
      ICurrentPrincipalAccessor currentPrincipalAccessor
    )
    {
      PrincipalStore = principalStore;
      ChallengeStore = challengeStore;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      PrincipalId? callerId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken);
      if (callerId is null)
      {
        return IdentityProblems.Unauthenticated();
      }

      OneOf<AgentKeyRegistrationCeremony.Materials, SharedProblemDetails> ceremonyResult =
        await AgentKeyRegistrationCeremony.TryCompleteAsync(
          command.PublicKey,
          command.Challenge,
          command.Signature,
          ChallengeStore,
          PrincipalStore,
          cancellationToken);
      if (ceremonyResult.IsT1)
      {
        return ceremonyResult.AsT1;
      }

      AgentKeyRegistrationCeremony.Materials materials = ceremonyResult.AsT0;

      var credential = Credential.Create(callerId.Value, CredentialType.AgentKey, materials.KeyId, materials.PublicKeyBytes, command.Label);
      try
      {
        await PrincipalStore.AddCredentialAsync(credential, cancellationToken);
      }
      catch (InvalidOperationException)
      {
        return IdentityProblems.CredentialAlreadyRegistered("agent key");
      }

      return new Response(credential.Id, Base64Url.EncodeToString(materials.KeyId));
    }
  }
}
