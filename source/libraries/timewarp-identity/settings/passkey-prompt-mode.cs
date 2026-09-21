#region Purpose
// Site-settings policy for the post-Entra passkey prompt: dismissible Soft vs blocking Required.
#endregion

#region Design
// RFC 219 D8 / 219-003 shipped Soft (banner, Later dismisses). Required (219-006) blocks app use
// after an Entra-issued session until a passkey is registered. Template default is Soft. None is
// not a stored value — Create rejects undefined enum members. Configuration does not own this
// field; administrators set it on the Settings page.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Post-Entra passkey prompt policy: dismissible soft banner versus blocking required registration.
/// </summary>
public enum PasskeyPromptMode
{
  /// <summary>Dismissible prompt; the user may continue without registering a passkey.</summary>
  Soft = 0,

  /// <summary>Blocks app use after an Entra-issued session until a passkey is registered.</summary>
  Required = 1
}
