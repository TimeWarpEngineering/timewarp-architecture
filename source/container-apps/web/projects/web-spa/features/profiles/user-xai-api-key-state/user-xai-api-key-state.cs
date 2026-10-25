#region Purpose
// SPA cache of the signed-in user's xAI API key status. The key itself is never stored here.
#endregion

#region Design
// TimeWarp.State. Fetch, set, and clear go through ActionSets. The set action carries the key
// only as far as the HTTP body. Success copies the status (visible, suffix, host override) and
// the page clears its input. Visible starts false so Profile does not flash the section when
// the site mode is Off. Command palette and the agent catalog do not offer set or clear.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using TimeWarp.Architecture.Features.AgentChats;

/// <summary>Profile status for the current user's xAI API key. No property is the key.</summary>
[StateAccess]
public sealed partial class UserXaiApiKeyState : State<UserXaiApiKeyState>
{
  /// <summary>True after a successful fetch, set, or clear.</summary>
  public bool HasSnapshot { get; private set; }

  /// <summary>True when the last set or clear failed. The problem is on NotificationState.</summary>
  public bool SaveFailed { get; private set; }

  /// <summary>True when the latest clear was declined. The Ask reload skips that case.</summary>
  internal bool ClearDeclined { get; private set; }

  /// <summary>False when the site mode is Off. Profile renders no section.</summary>
  public bool Visible { get; private set; }

  /// <summary>True when this user's key is stored and can be read.</summary>
  public bool Configured { get; private set; }

  /// <summary>True when a row is stored and cannot be read.</summary>
  public bool NeedsReplacement { get; private set; }

  /// <summary>True when this user may set, replace, or clear a key.</summary>
  public bool CanChange { get; private set; }

  /// <summary>True when this host can store a secret that survives a restart.</summary>
  public bool StorageAvailable { get; private set; }

  /// <summary>True when the host key is set, so Ask will not use this user's key.</summary>
  public bool SupersededByHost { get; private set; }

  /// <summary>Last four characters of this user's key.</summary>
  public string? Suffix { get; private set; }

  /// <inheritdoc />
  public override void Initialize()
  {
    HasSnapshot = false;
    SaveFailed = false;
    ClearDeclined = false;
    Visible = false;
    Configured = false;
    NeedsReplacement = false;
    CanChange = false;
    StorageAvailable = false;
    SupersededByHost = false;
    Suffix = null;
  }

  internal void Apply(IUserXaiApiKeyStatus status)
  {
    ArgumentNullException.ThrowIfNull(status);
    HasSnapshot = true;
    SaveFailed = false;
    Visible = status.Visible;
    Configured = status.Configured;
    NeedsReplacement = status.NeedsReplacement;
    CanChange = status.CanChange;
    StorageAvailable = status.StorageAvailable;
    SupersededByHost = status.SupersededByHost;
    Suffix = status.Suffix;
  }
}
