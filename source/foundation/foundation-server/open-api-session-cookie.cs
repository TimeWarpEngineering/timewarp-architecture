#region Purpose
// Host-supplied browser session facts for the OpenAPI document: the ASP.NET scheme name and the cookie name.
#endregion

#region Design
// foundation-server cannot reference IdentitySessionDefaults (identity-host). The web host
// passes the scheme name endpoints list in AuthSchemes and the cookie name the cookie handler
// sets, the same way it passes OpenApiAgentBearer. IsDefaultScheme says an authorized endpoint
// with no AuthSchemes is authenticated by this scheme, because it is the host's DefaultScheme.
#endregion

namespace TimeWarp.Foundation;

/// <summary>
/// Browser session cookie facts the host already owns. Foundation does not store the scheme or cookie name.
/// </summary>
public sealed class OpenApiSessionCookie
{
  /// <summary>ASP.NET scheme name endpoints list, such as the host's identity-session constant.</summary>
  public string SchemeName { get; }

  /// <summary>Name of the cookie the session handler issues.</summary>
  public string CookieName { get; }

  /// <summary>
  /// True when this scheme is the host's DefaultScheme, so an authorized endpoint with no AuthSchemes uses it.
  /// </summary>
  public bool IsDefaultScheme { get; }

  /// <summary>Creates the session facts.</summary>
  public OpenApiSessionCookie(string schemeName, string cookieName, bool isDefaultScheme)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(schemeName);
    ArgumentException.ThrowIfNullOrWhiteSpace(cookieName);
    SchemeName = schemeName;
    CookieName = cookieName;
    IsDefaultScheme = isDefaultScheme;
  }
}
