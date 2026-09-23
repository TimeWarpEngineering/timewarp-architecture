#region Purpose
// Server-side handler for the RenameCredential command: sets the nickname on one of the CALLER's own
// credentials, under the same bounded optimistic-concurrency retry loop as RevokeCredential.
#endregion

#region Design
// IDOR rule (load-bearing, security — identical to RevokeCredential.Handler): the caller's principal
// id comes ONLY from ICurrentPrincipalAccessor; the target is looked up by CredentialId from the
// route; ownership (credential.PrincipalId == callerId) is checked BEFORE any mutation; unknown id
// and someone-else's id return the EXACT SAME 404 so this endpoint is not an enumeration oracle.
// Retry loop: Get → ownership check → Rename() the in-hand snapshot → Update*; a
// ConcurrencyConflictException means a concurrent writer (revoke, merge, another rename) advanced
// the stored Version — re-Get and retry up to MaxAttempts, then 409 TooMuchContention. Read
// IPrincipalStore's Design region for the snapshot-on-get contract this relies on.
// Revoked credentials CAN be renamed (see the contract's Design region) — no IsRevoked branch.
// The nickname arrives pre-validated (1..64 after trim) by the mediator's FluentValidationBehavior;
// Credential.Rename re-checks and would throw ArgumentException on a bypass — that is a programming
// error, not a user-facing 400, so it is deliberately not caught here.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.RenameCredential;

public sealed partial class RenameCredential
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private const int MaxAttempts = 3;

    private readonly IPrincipalStore PrincipalStore;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;

    public Handler(IPrincipalStore principalStore, ICurrentPrincipalAccessor currentPrincipalAccessor)
    {
      PrincipalStore = principalStore;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      PrincipalId? callerId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken);
      if (callerId is null)
      {
        return IdentityProblems.Unauthenticated();
      }

      var credentialId = CredentialId.From(command.CredentialId);

      for (int attempt = 0; attempt < MaxAttempts; attempt++)
      {
        Credential? credential = await PrincipalStore.GetCredentialAsync(credentialId, cancellationToken);
        if (credential is null || credential.PrincipalId != callerId.Value)
        {
          // Unknown id and "belongs to someone else" are indistinguishable on the wire (Design region).
          return IdentityProblems.NotFound();
        }

        credential.Rename(command.Nickname);

        try
        {
          await PrincipalStore.UpdateCredentialAsync(credential, cancellationToken);
          return new Response();
        }
        catch (ConcurrencyConflictException)
        {
          // Stale snapshot — re-Get and retry (Design region).
        }
      }

      return IdentityProblems.TooMuchContention();
    }
  }
}
