#region Purpose
// Server-side handler for the CompleteAgentKeyRegistration command: verifies the agent's proof of
// possession and mints a Principal + Credential on success.
#endregion

#region Design
// Ceremony preamble (decode → consume → TryParse → verify → handle-exists) lives in
// AgentKeyRegistrationCeremony; ordering / replay-safety rationale is owned there. This handler's
// post-verify path: Principal.Create(Agent) → AddPrincipalAsync → Credential.Create →
// AddCredentialAsync (try/catch race). No sponsor, no cookie: agents need no linked human and are
// never issued a browser session — only a scoped bearer token, minted separately by
// CompleteAgentTokenIssuance.
// FindCredentialByHandleAsync (in the ceremony) runs BEFORE Principal.Create so sequential
// duplicate-handle rejection never leaves an orphan Principal. AddCredentialAsync is still wrapped
// in try/catch(InvalidOperationException) for the residual concurrent-race window (two ceremonies
// registering the SAME key) — the store enforces handle uniqueness atomically; the orphan Principal
// from the losing race is NOT compensated (IPrincipalStore has no delete method yet; see
// CompletePasskeyRegistration.Handler Design for the full residual rationale).
// Concurrency note (104-028): zero Update* calls. AddCredentialAsync's first-credential rule
// auto-promotes the STORED principal Provisional -> Keyed kind-agnostically; this handler's in-hand
// `principal` local is deliberately left stale afterward.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Buffers.Text;
using static TimeWarp.Architecture.Features.Identity.CompleteAgentKeyRegistration;

public sealed partial class CompleteAgentKeyRegistration
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IAgentKeyChallengeStore ChallengeStore;

    public Handler(IPrincipalStore principalStore, IAgentKeyChallengeStore challengeStore)
    {
      PrincipalStore = principalStore;
      ChallengeStore = challengeStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
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

      var principal = Principal.Create(PrincipalKind.Agent);
      await PrincipalStore.AddPrincipalAsync(principal, cancellationToken);

      var credential = Credential.Create(principal.Id, CredentialType.AgentKey, materials.KeyId, materials.PublicKeyBytes, command.Label);
      try
      {
        await PrincipalStore.AddCredentialAsync(credential, cancellationToken);
      }
      catch (InvalidOperationException)
      {
        return IdentityProblems.CredentialAlreadyRegistered("agent key");
      }

      return new Response(principal.Id, Base64Url.EncodeToString(materials.KeyId));
    }
  }
}
