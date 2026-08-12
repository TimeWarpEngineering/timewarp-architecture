#region Purpose
// SPA state for the signed-in principal's credentials (passkeys + agent keys) — Settings list UI.
#endregion

#region Design
// TimeWarp.State rule: every SPA → backend HTTP call goes through an ActionSet (COPIC / ProfileState).
// Credentials are product data from GetCredentials / AddPasskey / RevokeCredential — never page-local
// List<> fields. Null Passkeys = not loaded yet (loading UI); empty list = loaded with zero passkeys.
// ActivePasskeys is the Settings filter (passkey + IsActive); full list stays available for follow-ups.
// StatusMessage / CeremonyError are user-facing strings for create/revoke UX; API transport failures
// still go through DefaultApiHandler → ToastNotificationState (shared pipeline).
// Task 169.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using TimeWarp.Identity;
using static GetCredentials;

[StateAccess]
public sealed partial class CredentialsState : State<CredentialsState>
{
  private List<CredentialSummary>? CredentialsList { get; set; }

  /// <summary>Null until first successful fetch; empty means loaded with zero credentials.</summary>
  public IReadOnlyList<CredentialSummary>? Credentials => CredentialsList?.AsReadOnly();

  /// <summary>Active passkeys only — Settings default view.</summary>
  public IReadOnlyList<CredentialSummary> ActivePasskeys =>
    CredentialsList is null
      ? []
      : [.. CredentialsList
          .Where(c => c.Type == CredentialType.Passkey && c.IsActive)
          .OrderByDescending(c => c.CreatedAt)];

  public Guid? LastAddedCredentialId { get; private set; }

  public string? StatusMessage { get; private set; }

  public string? CeremonyError { get; private set; }

  public override void Initialize()
  {
    CredentialsList = null;
    LastAddedCredentialId = null;
    StatusMessage = null;
    CeremonyError = null;
  }
}
