#region Purpose
// Relying Party configuration for the WebAuthn passkey ceremonies (task 104-003).
#endregion

#region Design
// Lives in web-application, not web-server, despite being bound from configuration at the host
// (web-server's Program.ConfigureSettings calls AddFluentValidatedOptions<WebAuthnOptions,
// WebAuthnOptionsValidator> there) — the identity handlers that consume IOptions&lt;WebAuthnOptions&gt;
// live in web-application, which does NOT reference web-server (web-server -&gt; web-infrastructure
// -&gt; web-application is the one-way dependency chain), so the type has to live where both the
// binder (web-server, which references web-application transitively) and the consumers
// (web-application's own handlers) can see it. SampleOptions stays in web-server precisely because
// nothing outside web-server ever consumes it — this is the one place that convention does not fit.
// RpId defaults to "localhost" (the shared host component of both the dev origin,
// https://localhost:63611, and the fixed integration-test origin, https://localhost:7000) so the
// template works out of the box. AllowedOrigins defaults to empty, which WebAuthnRelyingParty
// treats as "accept any https origin whose host equals RpId" (see its Design region) — this
// deliberately covers both fixed local origins above without hand-listing ports; production
// configuration should set AllowedOrigins explicitly.
// Configuration section name is "WebAuthnOptions" (matches this type's name), not "WebAuthn":
// AddFluentValidatedOptions binds `configuration.GetSection(key)` where key defaults to
// typeof(TOptions).Name absent a [ConfigurationKey] attribute (see TimeWarp.OptionsValidation).
// A round-1 review caught appsettings.json shipping a "WebAuthn" section that silently never bound
// (masked because the shipped values equalled these C# defaults) — see
// WebServerIntegrationTests's WebAuthnOptions_Binds_From_Configuration for the regression pin.
#endregion

namespace TimeWarp.Architecture.Configuration;

public class WebAuthnOptions
{
  public string RpId { get; set; } = "localhost";
  public string RpName { get; set; } = "TimeWarp Architecture";
  public List<string> AllowedOrigins { get; set; } = [];
}
