# Round 1 — general (security-focused)
**Date:** 2026-08-03
**Scope reviewed:** runtime-config-gated mock auth (145-009) product + tests + smoke surfaces

## Summary

Implements fail-closed mock authentication:

| Surface | Mechanism |
|---------|-----------|
| SPA | `MockAuthenticationRegistration.TryAddSpaMockAuthentication` — only when Development/Testing **and** `Authentication:UseMock` |
| Web.Server | `MockIdentityPrincipalHandler` scheme always registered; returns `NoResult` unless gate + `X-TimeWarp-Mock-Principal-Id`; listed on `identity-session-authenticated` policy |
| AppHost | Sets `Authentication__UseMock=true` only when AppHost env is Development/Testing |
| Enforcement | TWA0021 bans product DI registration of mock types outside `MockAuthenticationRegistration` (tests/ exempt for in-proc harness) |
| Smoke | Production appsettings must not set UseMock true; registration type present; no DefineConstants MOCK_AUTHENTICATION |

**Security assessment:** Production fail-closed holds: environment gate is hard-coded allow-list (Development/Testing only); config flag alone is insufficient. Mock handler cannot succeed outside that gate. Header is not a bearer secret — it is inert when mock is off. Residual risk: any process that is both Development and UseMock-enabled will honor the header (intentional for closed-box/local); do not enable UseMock on shared Development hosts exposed beyond trust boundary.

## Issues

No issues found that block ship. Optional follow-ups (not open):

- Document that YARP must forward `X-TimeWarp-Mock-Principal-Id` (default forward works).
- Consider not listing mock scheme on AuthenticatedPolicy when mock is compile-time unused — current NoResult path is safe.
