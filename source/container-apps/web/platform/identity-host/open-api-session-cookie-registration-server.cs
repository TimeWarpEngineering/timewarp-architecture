#region Purpose
// Builds the web host's OpenAPI session-cookie facts from IdentitySessionDefaults.
#endregion

#region Design
// The scheme name is IdentitySessionDefaults.Scheme, the value contracts list as
// AuthenticationSchemeNames.IdentitySession, and the cookie name is the one AddCookie sets.
// identity-session is the host's DefaultScheme (RFC 219 D10), so an authorized endpoint with
// no AuthSchemes is documented as requiring the session.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using TimeWarp.Architecture.Configuration;
using TimeWarp.Foundation;

/// <summary>Web host registration for <see cref="OpenApiSessionCookie"/>.</summary>
internal static class OpenApiSessionCookieRegistration
{
  internal static OpenApiSessionCookie Create()
  {
    return new OpenApiSessionCookie
    (
      IdentitySessionDefaults.Scheme,
      IdentitySessionDefaults.CookieName,
      isDefaultScheme: true
    );
  }
}
