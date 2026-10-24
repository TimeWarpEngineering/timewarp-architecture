#region Purpose
// The three sign-in surfaces a generated app can show on the login card.
#endregion

#region Design
// Passkey is the template default: passkey first, Microsoft 365 only when the site offers it.
// Microsoft365Only and Staff hide passkey sign-in and public account creation. Staff adds the
// staff-only line. Copy is generic; no product or client name. Numeric values are stable for
// configuration binding.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>Which actions the login card offers.</summary>
public enum SignInMode
{
  /// <summary>Passkey is the primary action. Microsoft 365 appears when the site offers it.</summary>
  Passkey = 0,

  /// <summary>The only action is Continue with Microsoft 365.</summary>
  Microsoft365Only = 1,

  /// <summary>Continue with Microsoft 365, plus a staff-only line.</summary>
  Staff = 2
}
