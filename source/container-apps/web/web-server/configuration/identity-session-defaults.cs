#region Purpose
// Constants for the browser session cookie scheme used by the passkey identity feature.
#endregion

#region Design
// Scheme is a NAMED authentication scheme, deliberately not registered as the default: the
// container already carries a dormant Microsoft Entra (Azure AD B2C) registration
// (AddMicrosoftIdentityWebAppAuthentication in Program.ConfigureAuthentication) that owns whatever
// the default scheme currently is. Adding this as an additional named scheme via a second
// AddAuthentication() call lets both coexist — CookieBrowserSessionService always signs in/reads by
// this explicit scheme name, never relying on "the default."
#endregion

namespace TimeWarp.Architecture.Configuration;

public static class IdentitySessionDefaults
{
  public const string Scheme = "identity-session";
  public const string CookieName = ".timewarp.identity.session";
  public const string PrincipalIdClaimType = "timewarp:principal_id";
}
