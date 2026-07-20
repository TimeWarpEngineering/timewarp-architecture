#region Purpose
// Agent access-token lifetime configuration for the agent-key token-issuance ceremony (task 104-004).
#endregion

#region Design
// Lives in web-application (not web-server) for the SAME reason WebAuthnOptions does (104-003
// round-1 finding M1's sibling deviation, made from day one here rather than discovered by a
// review): the consuming handler (CompleteAgentTokenIssuance.Handler) lives in web-application,
// which does NOT reference web-server (web-server -> web-infrastructure -> web-application is the
// one-way dependency chain), so this type must live where both the binder (web-server, which
// references web-application transitively) and the consumer (the handler) can see it.
// Configuration section name is "AgentTokenOptions" (matches this type's name) — AddFluentValidatedOptions
// binds configuration.GetSection(key) where key defaults to typeof(TOptions).Name absent a
// [ConfigurationKey] attribute (see TimeWarp.OptionsValidation). 104-003's round-1 review (finding
// M1) caught WebAuthnOptions shipping with an appsettings section named "WebAuthn" — a mismatch that
// silently never bound, masked because the shipped values happened to equal the C# defaults. This
// type's appsettings.json section is "AgentTokenOptions" exactly, verified by a binding regression
// test (WebServerIntegrationTests's AgentTokenOptions_Binds_From_Configuration) asserting a
// non-default configured value round-trips through IOptions<AgentTokenOptions>, so a
// section-name regression cannot recur silently.
// TokenLifetimeMinutes default 15 (task 104-004 §2): short-lived, no refresh tokens — refresh IS the
// token ceremony (see IAgentTokenStore's Design region).
#endregion

namespace TimeWarp.Architecture.Configuration;

public class AgentTokenOptions
{
  public int TokenLifetimeMinutes { get; set; } = 15;
}
