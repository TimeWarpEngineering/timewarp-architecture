#region Purpose
// SPA cache of the site-settings singleton for the Settings Authentication section and Required passkey gate.
#endregion

#region Design
// TimeWarp.State: HTTP goes through ActionSets. Null snapshot until first fetch. Version is the
// concurrency token round-tripped on Update. PasskeyPromptMode drives AddPasskeyPrompt Later vs
// block. Slice Features.Settings; SettingsPage (Applications) and AuthenticationStateListener
// (Identity) take CrossSliceReference. Task 219-006.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

[StateAccess]
public sealed partial class SiteSettingsState : State<SiteSettingsState>
{
  public bool? EntraSignInEnabled { get; private set; }
  public bool? EntraAllowBootstrap { get; private set; }
  public IReadOnlyList<string> EntraTrustedTenants { get; private set; } = [];
  public PasskeyPromptMode PasskeyPromptMode { get; private set; } = PasskeyPromptMode.Soft;
  public long Version { get; private set; }
  public string? SaveError { get; private set; }

  public bool HasSnapshot => EntraSignInEnabled is not null;

  public override void Initialize()
  {
    EntraSignInEnabled = null;
    EntraAllowBootstrap = null;
    EntraTrustedTenants = [];
    PasskeyPromptMode = PasskeyPromptMode.Soft;
    Version = 0;
    SaveError = null;
  }
}
