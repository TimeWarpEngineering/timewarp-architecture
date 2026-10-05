#region Purpose
// Server-side credential rules shared by the handlers that enforce them and the reads that offer them.
#endregion

#region Design
// One copy of each rule (task 275): RevokeCredential.Handler enforces CanRevoke (409 LastCredential
// otherwise) and EntraTicketProcessor enforces HoldsMicrosoft365 (409 Microsoft365AlreadyLinked).
// The hypermedia lab reads call the same predicates to decide which actions to offer, so an offer
// and the enforcement cannot drift. Inputs are counts and types rather than Credential instances,
// so a read that only holds GetCredentials summaries applies the rule without reloading entities.
// The SPA still carries its own mirrors (CredentialsState.CanUnlink / CanLinkMicrosoft365); removing
// them is the adoption follow-up of task 275, not this file's concern.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public static class CredentialRules
{
  /// <summary>True when revoking one credential still leaves another active one (no self-lockout).</summary>
  public static bool CanRevoke(int activeCredentialCount) =>
    activeCredentialCount > 1;

  /// <summary>True when the principal's active credentials already include a Microsoft 365 (Entra) account.</summary>
  public static bool HoldsMicrosoft365(IEnumerable<CredentialType> activeCredentialTypes) =>
    activeCredentialTypes.Contains(CredentialType.EntraAccount);

  /// <summary>True when Microsoft 365 sign-in is offered and the principal has not linked an account yet.</summary>
  public static bool CanLinkMicrosoft365(bool microsoft365Offered, IEnumerable<CredentialType> activeCredentialTypes) =>
    microsoft365Offered && !HoldsMicrosoft365(activeCredentialTypes);
}
