#region Purpose
// Port for reading the current request's User-Agent header so identity registration handlers can record
// the browser/OS family a credential was registered from without web-application depending on ASP.NET Core.
#endregion

#region Design
// Task 248-001, same layering pattern as IRequestHostAccessor: web-application declares the port,
// web-server implements it over IHttpContextAccessor (HttpRequestUserAgentAccessor). Returns the raw
// header value (or null when there is no request / no header); the CALLER (RegistrationContext) is
// what reduces it to family names — the raw string never reaches a Credential row. Synchronous for
// the same reason as IRequestHostAccessor: a header read is a pure property access.
// On the InteractiveServer loopback hop the circuit's User-Agent is copied onto the internal request
// by IdentitySessionCookieForwardingHandler, so server-rendered ceremonies still see the browser.
#endregion

namespace TimeWarp.Architecture.Abstractions;

public interface IRequestUserAgentAccessor
{
  string? GetUserAgent();
}
