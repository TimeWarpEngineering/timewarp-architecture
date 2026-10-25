#region Purpose
// Names the browser sign-in methods a generated app can enable.
#endregion

#region Design
// One value per method. Google and X are future values, not combinations. Configuration binds
// the names, so an unknown name fails at start. Numeric values stay stable. Button order on
// the login card is this declaration order, not the order of the configuration list.
// Agent keys and agent tokens are not browser sign-in and are not values here.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

/// <summary>A browser sign-in method the server can enable.</summary>
public enum SignInMethod
{
  /// <summary>WebAuthn passkey sign-in and passkey account creation.</summary>
  Passkey = 0,

  /// <summary>Microsoft 365 (Entra) sign-in.</summary>
  Microsoft365 = 1
}
