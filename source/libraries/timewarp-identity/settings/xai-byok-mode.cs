#region Purpose
// Site-settings switch for whose xAI API key Ask may use below the host key.
#endregion

#region Design
// Allow is the zero value so a JSON body that omits the field stays Allow, the template default.
// Required uses the signed-in user's key and does not fall back to the site key. Off uses the
// site key only and the Profile section stays hidden. The host XAI:ApiKey wins in every mode;
// this enum does not represent that tier. Undefined values are rejected, same as PasskeyPromptMode.
#endregion

namespace TimeWarp.Identity;

/// <summary>Whose xAI API key Ask uses when the host has not set one.</summary>
public enum XaiByokMode
{
  /// <summary>The user's key, then the site key.</summary>
  Allow = 0,

  /// <summary>The user's key only. A site key is not a fallback.</summary>
  Required = 1,

  /// <summary>The site key only. Profile does not offer a personal key.</summary>
  Off = 2
}
