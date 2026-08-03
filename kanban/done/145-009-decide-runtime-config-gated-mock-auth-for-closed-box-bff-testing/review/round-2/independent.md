# Round 2 — independent security verification (145-009)
**Date:** 2026-08-03
**Reviewer:** independent adversarial agent (orchestrator session), clean worktree

## Verdict: REFUTED — critical fail-closed gap, dynamically proven. Task reopened.

### R2-1 — CRITICAL — Status: open
web-server/program.cs:203 calls the 2-arg Web.Spa.Program.ConfigureServices overload whose
environment gate reads configuration["ASPNETCORE_ENVIRONMENT"] ?? ["DOTNET_ENVIRONMENT"] —
IConfiguration, NOT IHostEnvironment. DYNAMICALLY PROVEN: in a genuinely Production-booted
host (real env var unset), later-loaded config content alone (appsettings/CLI/any provider)
sets those keys → TryAddSpaMockAuthentication activates → MockAuthenticationStateProvider
auto-authenticates EVERY visitor as SystemAdmin/Developer/Accountant on server-rendered/
prerendered [Authorize]/<AuthorizeView> pages. Violates the spec's explicit
IHostEnvironment.IsDevelopment() requirement. API/FastEndpoints surface unaffected
(MockIdentityPrincipalHandler correctly uses DI IHostEnvironment). The registration class's
own Design comment asserts the false premise ("same gate applies") — root cause. Zero tests
exercise the composition path (all fail-closed tests call the pure predicate with strings).
FIX: thread builder.Environment.EnvironmentName into a 3-arg overload from web-server Main;
remove/fail-closed-harden the config-derived fallback; add a composition-path regression test.

### R2-2 — Medium — Status: open
TWA0021 evasions EMPIRICALLY PROVEN (probe tests, reverted): (a) non-generic
AddScoped(typeof(I), typeof(MockX)) → zero diagnostics; (b) factory delegate
AddScoped<I>(_ => new MockX()) → zero diagnostics. Analyzer inspects only generic
type-argument syntax. Also: tests/ exemption is path-substring, and the mock-type allowlist
is fixed 2 names. FIX: match typeof() arguments + object-creation of mock types in
registration lambdas; regression tests for both evasions.

### R2-3 — Low — Status: open
Epic 145 parent moved to done via pure git mv: checklist boxes unchecked, no Results —
violates parent-close conventions (and is now moot: parent reopens with this child).

### R2-4 — Info — Status: open (pre-existing, task-136-scoped)
Stale "web 5 / api 2" Design comments in template-smoke-command.cs:29 / harness.cs:21
(actual co-located counts 2/1) — the repeat-offender comment class again.

## Confirmed sound
Header inertness (single gated read); scheme-listing single-point-of-failure documented and
correct for the handler path; "Testing" exact-match acceptable; MOCK_AUTHENTICATION fully
removed; Dev UX parity; all machine gates green (build 0/0, full dev test incl. aspire-tests
7/7 with 200-with-principal AND 401-without, analyzer suite 104, smoke ×3 with fail-closed
static checks firing, audit 23/23). Static smoke checks adequate for the API surface;
composition-path coverage is the gap (R2-1).

## Round-1 audit
Effort-1 "security-focused" self-review verified the predicate and the two registration sites
but never traced web-server's actual caller and ran nothing dynamically; did not attempt
analyzer evasion. "Zero issues" is not supported.
