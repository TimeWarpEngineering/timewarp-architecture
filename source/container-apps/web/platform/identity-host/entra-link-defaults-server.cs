#region Purpose
// Named Entra OIDC scheme constants. identity-session stays DefaultScheme; Entra is never the default.
#endregion

#region Design
// RFC 219 D10: register Entra as named scheme "entra" (never DefaultScheme). Challenge/link/bootstrap
// stash mode, the link caller PrincipalId, and the local return path on
// AuthenticationProperties.Items so OnTicketReceived (and the test fake handler) can complete
// without trusting the OIDC handler's own sign-in. OpenIdConnectHandler overwrites
// Properties.RedirectUri with the protocol redirect_uri; the Items copy is the app return path.
// Literal "entra" must stay in lockstep with AuthenticationSchemeNames.Entra (contracts cannot
// reference this server type).
#endregion

namespace TimeWarp.Architecture.Configuration;

public static class EntraLinkDefaults
{
  public const string Scheme = "entra";
  public const string ModeItemKey = "entra.mode";
  public const string LinkCallerPrincipalIdItemKey = "entra.link_principal_id";
  public const string ReturnUrlItemKey = "entra.return_url";
}
