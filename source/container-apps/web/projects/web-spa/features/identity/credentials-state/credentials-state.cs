#region Purpose
// SPA state for the signed-in principal's credentials (passkeys + Entra + agent keys) — Settings list and D8 soft prompt.
#endregion

#region Design
// TimeWarp.State rule: every SPA → backend HTTP call goes through an ActionSet (COPIC / ProfileState).
// Credentials are product data from GetCredentials / AddPasskey / AddExistingPasskey /
// RevokeCredential — never page-local List<> fields. Null Credentials = no snapshot; empty list
// = loaded with zero passkeys.
// In-flight fetch is [TrackAction] on FetchCredentials — Settings uses IsAnyActive, not null.
// ActivePasskeys is the Settings filter (passkey + IsActive); ActiveEntraAccounts is the Microsoft 365
// filter. Full list stays available for follow-ups.
// Task 229: CanLinkMicrosoft365 is Offered && no active EntraAccount (one linked account per
// principal). CanUnlink is ActiveCredentialCount > 1 so Unlink cannot lock the user out; the
// server LastCredential 409 is the backstop.
// Outcomes (created / merged / removed / ceremony failed) are reported to the shell's single
// notification region: handlers publish OutcomeNotification / ProblemDetailsNotification and
// NotificationState paints them (task 247). CeremonyFailed is the only page-facing flag — it
// lets callers skip FetchCredentials after a failed ceremony so Fetch cannot mask the failure.
// API transport failures still go through DefaultApiHandler → NotificationState.
// RFC 219 D8: ShouldShowPasskeySoftPrompt is the Type-list predicate (Entra without Passkey),
// not a TrustTier. PasskeySoftPromptDismissed is session UX only — never a route gate.
// Task 169 + 219-003.
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

  /// <summary>Active EntraAccount credentials — Settings Microsoft 365 section.</summary>
  public IReadOnlyList<CredentialSummary> ActiveEntraAccounts =>
    CredentialsList is null
      ? []
      : [.. CredentialsList
          .Where(c => c.Type == CredentialType.EntraAccount && c.IsActive)
          .OrderByDescending(c => c.CreatedAt)];

  /// <summary>Active credentials of every type — Unlink/Delete last-credential guard.</summary>
  public int ActiveCredentialCount =>
    CredentialsList?.Count(c => c.IsActive) ?? 0;

  /// <summary>True when Microsoft 365 is offered and no active EntraAccount is linked.</summary>
  public static bool CanLinkMicrosoft365(bool offered, int activeEntraAccountCount) =>
    offered && activeEntraAccountCount == 0;

  /// <summary>True when revoking this credential would leave at least one other active credential.</summary>
  public static bool CanUnlink(int activeCredentialCount) =>
    activeCredentialCount > 1;

  public Guid? LastAddedCredentialId { get; private set; }

  /// <summary>True when the last add/merge ceremony failed; the failure itself is on NotificationState.</summary>
  public bool CeremonyFailed { get; private set; }

  /// <summary>True after the user dismisses the Entra add-passkey banner this SPA session.</summary>
  public bool PasskeySoftPromptDismissed { get; private set; }

  /// <summary>
  /// True when GetCredentials shows an active EntraAccount and no active Passkey, and the
  /// banner has not been dismissed. Never used as a route or session gate.
  /// </summary>
  public bool ShouldShowPasskeySoftPrompt =>
    PasskeySoftPrompt.ShouldShow(Credentials, PasskeySoftPromptDismissed);

  public override void Initialize()
  {
    CredentialsList = null;
    LastAddedCredentialId = null;
    CeremonyFailed = false;
    PasskeySoftPromptDismissed = false;
  }
}
