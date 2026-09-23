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
// StatusMessage / CeremonyError are user-facing strings for create/revoke UX; API transport failures
// still go through DefaultApiHandler → ToastNotificationState (shared pipeline).
// RFC 219 D8: ShouldShowPasskeySoftPrompt is the Type-list predicate (Entra without Passkey),
// not a TrustTier. PasskeySoftPromptDismissed is session UX only — never a route gate.
// Task 248-001: PendingNicknameCredentialId / PendingNicknameDefault is the "name your new
// passkey" prompt — set by AddPasskey's success (or SetPendingNickname after a Passkeys-page
// register ceremony) with the provider name as the prefill; CredentialList auto-opens its inline
// rename editor for that row and AddPasskeyPrompt shows a small form when it started the ceremony;
// RenameCredential success and ClearPendingNickname (skip / cancel) both clear it. Exactly ONE
// surface owns a pending nickname (review M1 of 248-001): PendingNicknameOwnedByPrompt is set by
// ClaimPendingNicknameForPrompt when AddPasskeyPrompt started the ceremony, and lists bind
// PendingListRenameCredentialId (null while the prompt owns it) so Settings/Passkeys never open a
// second editor for the same credential. Rename outcomes go to the shell notification region
// (ToastNotificationState), not a page-local bar.
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

  /// <summary>Credential awaiting a user nickname (just added); null when nothing is pending.</summary>
  public Guid? PendingNicknameCredentialId { get; private set; }

  /// <summary>Prefill for the pending nickname prompt — the provider name when known.</summary>
  public string? PendingNicknameDefault { get; private set; }

  /// <summary>True when AddPasskeyPrompt's own form owns the pending nickname; lists then stay closed.</summary>
  public bool PendingNicknameOwnedByPrompt { get; private set; }

  /// <summary>Pending credential id for CredentialList auto-open — null while the prompt owns it.</summary>
  public Guid? PendingListRenameCredentialId =>
    PendingNicknameOwnedByPrompt ? null : PendingNicknameCredentialId;

  public string? StatusMessage { get; private set; }

  public string? CeremonyError { get; private set; }

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
    PendingNicknameCredentialId = null;
    PendingNicknameDefault = null;
    PendingNicknameOwnedByPrompt = false;
    StatusMessage = null;
    CeremonyError = null;
    PasskeySoftPromptDismissed = false;
  }
}
