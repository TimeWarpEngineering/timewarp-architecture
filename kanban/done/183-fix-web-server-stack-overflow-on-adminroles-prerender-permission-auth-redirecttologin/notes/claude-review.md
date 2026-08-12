# Claude review — task 183 fix (e55bcabf)

**Reviewer:** Claude
**Date:** 2026-08-12
**Scope:** the five files in `e55bcabf` (hosted auth-state provider, program.cs re-registration, unsealed SPA provider, registration Design note, RedirectToLogin SSR guard)

## Verdict: Accept

The fix is correct, minimal, and uses the right Blazor patterns for each half of the cascade.

### What's right

- **`HostedIdentitySessionAuthenticationStateProvider`** — preferring `HttpContext.User` during
  hosted prerender is the standard pattern for a hosted Blazor Web App whose SPA provider is
  HTTP-based. Inheriting from the SPA provider (rather than replacing it) keeps the passkey
  ceremony's `NotifySessionChanged` cast working, and the fallback to `base` covers no-HttpContext
  edge cases. `PermissionRequirementHandler` evaluates via `principal_id` + AuthenticationType, so
  the cookie principal (post `PrincipalRoleClaimsTransformation`) is sufficient — no permission
  claims needed on it.
- **program.cs guard** — re-registering after `Web.Spa.Program.ConfigureServices` with the
  `ImplementationType == IdentitySessionAuthenticationStateProvider` check is the right
  last-registration-wins move, and correctly skips mock/Entra modes (under mock the SPA registers
  `MockAuthenticationStateProvider`, so the hosted override must not apply).
- **`RedirectToLogin`** — moving `NavigateTo` out of `OnInitialized` into
  `OnAfterRender(firstRender)` is the canonical SSR-safe shape: `OnAfterRender` never runs during
  static SSR, and the `RendererInfo.IsInteractive` check is belt-and-braces.
  `forceLoad: true` avoids enhanced-nav re-entry, and the static `<a>` fallback gives the
  prerendered frame a usable affordance instead of dead air.

### Observation (non-blocking, worth a follow-up eye)

The app runs `InteractiveAuto` (BlazorSettings default + appsettings), which has a
**server-circuit phase** before WASM takes over. `IHttpContextAccessor` inside a circuit is
officially documented as unreliable (the context may be null or not flow across async hops).
If it comes back null in a circuit, the provider falls back to the base session HTTP call —
which from the server does not carry the browser cookie → anonymous → interactive
`RedirectToLogin` (full reload). That's no longer a crash, but it could bounce a signed-in
user to Login during the server-interactive window if the accessor misbehaves. In practice the
websocket request's context (which did carry the cookie) is usually what the accessor returns,
so this is theoretical until observed. The durable fix, if it ever bites, is .NET 8+'s
`AddAuthenticationStateSerialization` / `PersistentAuthenticationStateProvider` pattern
(serialize prerender auth state into the circuit/WASM) — file a follow-up task then, don't
preempt it now. WASM interactive is unaffected: the browser's fetch carries the cookie, which is
why the SPA provider always worked client-side.

### Bonus resolved residual

`tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs`
(task 154) documents a Design residual: *"full authenticated Blazor HTML SSR of /Settings
stack-overflows in this in-proc host … Page 200 is left to live smoke."* That residual **was
this bug**. The 183 fix makes authenticated HTML SSR testable in-proc, so I'm adding the
previously-impossible regression tests there (authenticated Member `/Settings` → 200;
Administrator `/Admin/Roles` → 200, body is the real page, not the sign-in fallback) and
updating that Design region. Pre-fix these tests kill the test process (stack overflow), which
makes them a true tripwire for this class of bug.

### Optional stretch from the brief

Not taken: cookie-forwarding handler / claim enrichment. The hosted provider already covers
prerender correctly, and the loopback-with-cookie path would only matter for the circuit edge
case above — better solved by auth-state serialization if ever needed.
