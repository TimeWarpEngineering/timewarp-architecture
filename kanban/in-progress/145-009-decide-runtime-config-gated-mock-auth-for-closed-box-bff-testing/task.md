# Runtime-config-gated mock auth for closed-box BFF testing

## Description

PROMOTED (Steve, 2026-08-02) — decision resolved: the template SHOULD support closed-box
authenticated BFF testing. Current state is a coverage hole with a shortcut at its root:
mock auth is compile-time (`#if MOCK_AUTHENTICATION`, web-spa program.cs), so no test
anywhere exercises an authenticated request through the real ingress as a real process —
the closed-box lane can only prove 401s. A philosophy of "don't mock your friends — test the
real thing" requires the real thing to be testable.

## Requirements

1. **Replace the compile-time `#if MOCK_AUTHENTICATION` with a runtime-config-gated
   registration** in the affected Program composition (web-spa; check web-server surface),
   designed FAIL-CLOSED for production:
   - hard Environment gate (mock registration only reachable when
     IHostEnvironment.IsDevelopment() or an explicit test environment) AND config flag;
     absent flag = real auth, always;
   - a **TWA analyzer** enforcing the pattern in template content (mock-auth registration
     must be inside the environment gate — build-breaking if moved), per the
     prefer-analyzers-over-convention doctrine;
   - template-smoke assertion that a Production-environment generated app cannot activate
     mock auth via config alone.
2. **Closed-box authenticated coverage:** new closed-box tests (aspire-tests or a sibling)
   driving an authenticated request through the real ingress → web-server (→ api where
   relevant) using the runtime-gated mock principal; prove at least one policy-protected
   route end-to-end (e.g. /api/Roles 200 with principal vs existing 401 without).
3. **In-proc lane unchanged:** MockAccessTokenProvider DI override keeps working; document
   the two auth-testing lanes in AGENTS.md/skill.
4. **Security review pass** (Phase 4b must include a security-focused look at the gate).
5. **Gates:** dev build 0/0; full dev test; template-smoke ×3 incl. the new Production
   fail-closed assertion; audit.

## Checklist

- [x] Runtime seam replaces #if; environment-gated fail-closed
- [x] TWA analyzer + tests enforcing the gate
- [x] Production-cannot-activate smoke assertion
- [x] Closed-box authenticated ingress test(s) green
- [x] Docs: two auth-testing lanes
- [x] Security-focused review recorded; gates green; kanban committed

## Notes

- Origin: 143 findings §4, 145-005 round-2, Steve 2026-08-02. Sequenced after 145-008 (done).

## Session

- Implementation + security review: 145-009 (2026-08-03)

## Results

### Summary

**Runtime fail-closed mock auth** replaces compile-time `MOCK_AUTHENTICATION`:

| Layer | Behavior |
|-------|----------|
| SPA | `MockAuthenticationRegistration.TryAddSpaMockAuthentication` only when Development/Testing **and** `Authentication:UseMock` |
| Web.Server | `MockIdentityPrincipalHandler` (`mock-identity-session`) returns Success only under the same gate + `X-TimeWarp-Mock-Principal-Id`; listed on `identity-session-authenticated` |
| AppHost | Sets `Authentication__UseMock=true` only when AppHost is Development/Testing |
| TWA0021 | Product tree cannot DI-register mock providers outside `MockAuthenticationRegistration` |
| Smoke | Production appsettings must not set UseMock true; registration file present; no DefineConstants MOCK_AUTHENTICATION |

In-proc `MockAccessTokenProvider` DI override in `WebTestServerApplication` unchanged (analyzer skips `tests/`).

### Verification

| Gate | Result |
|------|--------|
| `dev build` | 0/0 |
| aspire-tests | **7/7** (includes `RolesThroughIngress_Should_Ok_Given_MockPrincipalHeader`) |
| foundation-contracts matrix tests | 6 fail-closed cases |
| TWA0021 analyzer tests | 2 cases |
| web-server-integration (roles etc.) | green |
| `dotnet run tools/dev-cli/dev.cs -- template-smoke` | **SUCCEEDED** (SmokeDefault / SmokeNoPostgres / SmokeNoApi + mock-auth surfaces) |

### Review

Effort 1 **security-focused**, **clean** — `review/`

- Fail-closed gates reviewed; header inert when mock off; Production cannot activate via config alone
- Paths: `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/disposition.md`
