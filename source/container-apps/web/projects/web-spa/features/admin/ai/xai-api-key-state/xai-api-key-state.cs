#region Purpose
// SPA cache of the site xAI API key status. The key itself is never stored here.
#endregion

#region Design
// TimeWarp.State. Fetch, set, and clear go through ActionSets. The set action carries the key
// only as far as the HTTP body. Success copies the status (suffix, who, when) and the page
// clears its input. Command palette and the agent catalog do not offer set or clear: a palette
// field would put the secret in a place this page is careful not to keep.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Ai;

using TimeWarp.Architecture.Features.AgentChats;

/// <summary>Admin → AI status. No property is the API key.</summary>
[StateAccess]
public sealed partial class XaiApiKeyState : State<XaiApiKeyState>
{
  /// <summary>True after a successful fetch, set, or clear.</summary>
  public bool HasSnapshot { get; private set; }

  /// <summary>True when the last set or clear failed. The problem is on NotificationState.</summary>
  public bool SaveFailed { get; private set; }

  /// <summary>True when the latest clear was declined. The Ask reload skips that case.</summary>
  internal bool ClearDeclined { get; set; }

  /// <summary>Host <c>XAI:ApiKey</c> is set. The page is read-only.</summary>
  public bool ManagedByHost { get; private set; }

  /// <summary>Ask has a key.</summary>
  public bool Configured { get; private set; }

  /// <summary>This deployment can store an admin key.</summary>
  public bool StorageAvailable { get; private set; }

  /// <summary>Set, replace, and clear are offered.</summary>
  public bool CanChange { get; private set; }

  /// <summary>A row is stored and Ask cannot read it.</summary>
  public bool NeedsReplacement { get; private set; }

  /// <summary>Last four characters of the stored admin key, when the host key is not the one Ask uses.</summary>
  public string? Suffix { get; private set; }

  /// <summary>When the admin key was written.</summary>
  public DateTimeOffset? SetAt { get; private set; }

  /// <summary>Who wrote the admin key.</summary>
  public Guid? SetByPrincipalId { get; private set; }

  /// <summary>How to set the host value. Shown when this page cannot store a key.</summary>
  public string SetupHint { get; private set; } = "";

  /// <inheritdoc />
  public override void Initialize()
  {
    HasSnapshot = false;
    SaveFailed = false;
    ClearDeclined = false;
    ManagedByHost = false;
    Configured = false;
    StorageAvailable = false;
    CanChange = false;
    NeedsReplacement = false;
    Suffix = null;
    SetAt = null;
    SetByPrincipalId = null;
    SetupHint = "";
  }

  internal void Apply(IXaiApiKeyStatus status)
  {
    ArgumentNullException.ThrowIfNull(status);
    HasSnapshot = true;
    SaveFailed = false;
    ManagedByHost = status.ManagedByHost;
    Configured = status.Configured;
    StorageAvailable = status.StorageAvailable;
    CanChange = status.CanChange;
    NeedsReplacement = status.NeedsReplacement;
    Suffix = status.Suffix;
    SetAt = status.SetAt;
    SetByPrincipalId = status.SetByPrincipalId;
    SetupHint = status.SetupHint;
  }
}
