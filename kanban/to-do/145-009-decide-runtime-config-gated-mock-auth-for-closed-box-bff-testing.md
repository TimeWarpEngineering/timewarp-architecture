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

- [ ] Runtime seam replaces #if; environment-gated fail-closed
- [ ] TWA analyzer + tests enforcing the gate
- [ ] Production-cannot-activate smoke assertion
- [ ] Closed-box authenticated ingress test(s) green
- [ ] Docs: two auth-testing lanes
- [ ] Security-focused review recorded; gates green; kanban committed

## Notes

- Origin: 143 findings §4 (compile-time blocker), 145-005 round-2 (closed-box HTTP coverage
  traded away consciously — this restores it where it matters, with auth), Steve 2026-08-02
  (excellence over parking). Sequence AFTER 145-008 (closed-box tests want the session
  fixture model). TWA0010 note: MOCK_AUTHENTICATION DefineConstants cleanup rides along.
