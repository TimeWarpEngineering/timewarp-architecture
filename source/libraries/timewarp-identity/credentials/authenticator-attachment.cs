#region Purpose
// Authenticator attachment captured once at passkey registration: platform (built into the device), cross-platform (roaming / phone / security key), or unknown.
#endregion

#region Design
// Mirrors WebAuthn's AuthenticatorAttachment enumeration ("platform" / "cross-platform") plus Unknown
// for agent keys, Entra accounts, and browsers that expose neither PublicKeyCredential
// .authenticatorAttachment nor response transports. Stored as an int column (task 248-001); Unknown = 0
// so an unmapped legacy row reads as Unknown, never as a guess.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Where the registering authenticator lives relative to the client device.
/// </summary>
public enum AuthenticatorAttachment
{
  /// <summary>Not captured (agent keys, Entra accounts, or a browser that reported nothing).</summary>
  Unknown = 0,

  /// <summary>Built into the registering device (Windows Hello, Touch ID, Android screen lock).</summary>
  Platform = 1,

  /// <summary>Roaming authenticator — phone over hybrid/QR, security key over USB/NFC/BLE.</summary>
  CrossPlatform = 2
}
