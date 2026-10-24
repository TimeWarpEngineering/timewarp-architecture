#region Purpose
// Configuration section that selects the login card: passkey, Microsoft 365 only, or staff.
#endregion

#region Design
// Section name "SignInOptions" matches the type name: AddFluentValidatedOptions binds by type name.
// The default mode is Passkey, so a host with no section paints the same login card.
// The server configuration drives prerender. The WebAssembly host binds the same section from
// its own appsettings so hydration does not swap the card. Staff and Microsoft365Only share the
// work-account lead. Only Staff shows the staff-only line. Copy is generic; no client name.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>Login-card mode for one generated app.</summary>
public sealed class SignInOptions
{
  public const string SectionName = "SignInOptions";

  public const string WorkAccountLead = "Use your Microsoft 365 work account.";

  public const string StaffNote = "Staff only.";

  public const string Microsoft365UnavailableMessage = "Microsoft 365 sign-in is not available.";

  public SignInMode Mode { get; set; } = SignInMode.Passkey;

  /// <summary>Passkey button, account creation, and the add-passkey prompt.</summary>
  public bool OffersPasskey => Mode == SignInMode.Passkey;

  /// <summary>Lead line used when the card is a Microsoft 365 work account.</summary>
  public bool ShowsWorkAccountLead => Mode is SignInMode.Microsoft365Only or SignInMode.Staff;

  /// <summary>Staff-only line under the Microsoft 365 button.</summary>
  public bool ShowsStaffNote => Mode == SignInMode.Staff;

  /// <summary>
  /// Microsoft 365 is the only path, and the site is not offering it, so the card must not
  /// fall back to a passkey.
  /// </summary>
  public bool ShowMicrosoft365Unavailable(bool offered) => ShowsWorkAccountLead && !offered;
}
