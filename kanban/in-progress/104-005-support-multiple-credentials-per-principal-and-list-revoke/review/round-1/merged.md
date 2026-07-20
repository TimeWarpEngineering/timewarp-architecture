# Round 1 — merged findings
**Date:** 2026-07-20
**Sources:** general, security

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 0 |

Full descriptions: `general.md` / `security.md`. **Security verdict: the IDOR/ownership model is
SOUND — zero exploitable findings.** Verified by counterfactual (the foreign-credential test flips
404→409 if the ownership check is removed); key material structurally omitted with a wire-level
assertion; identity:read→403 least-privilege proven live; retry loop never leaks
ConcurrencyConflictException as 500; cross-principal handle collision → identical 409, no oracle,
Credential.Create always binds callerId. General: another clean commit — contracts/handlers/loop
all correct, unified accessor validated by cookie+bearer list tests, coverage hits every matrix
cell. 0 bugs across both lenses. Suites green (169/38/78+1).

## Issues

### M1 — Severity: suggestion — Status: open
- File: i-current-principal-accessor.cs:41-44; http-current-principal-accessor.cs:36-46
- Description: The accessor's "HttpContext.User IS the winning scheme's principal" wording is imprecise for the dual-auth case — a request carrying BOTH a valid cookie (principal A) and a valid agent token (principal B) merges both identities; FindFirstValue resolves the principal id by framework merge order, not "which scheme won." NOT a bug (caller legitimately holds both → no escalation, fails safe) but the resolution should be explicit and the wording tightened.
- Suggestion: Make resolution deterministic/explicit (e.g. document that in the multi-identity case the caller demonstrably controls both principals so either is acceptable; or resolve from a defined precedence) and correct the Design-region wording to describe merged-identity claim resolution accurately.
- Source: security
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: get-credentials.cs:34 (+ revoke-credential.cs, add-passkey.cs, add-agent-key.cs; constant at credential-management-defaults.cs:44)
- Description: Policy name `"credential-management"` duplicated as string literals across contracts + server constant — the THIRD instance of the contract-vs-server policy-name coupling (after identity-session-authenticated and agent-scope:identity:read). Fail-closed and test-caught here.
- Suggestion: Already tracked as task 111 (TWA policy-name-agreement analyzer). Add this third instance to 111's motivation; no per-task fix.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: tests/.../Credential_Add_Tests.cs:82-120
- Description: The cross-principal duplicate-handle 409 (add a handle already owned by ANOTHER principal → identical 409, no attach, no oracle) is only tested same-principal; the code is correct but the regression test is missing.
- Suggestion: Add a cross-principal duplicate-handle test.
- Source: security
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: tests/.../Credential_List_Tests.cs:159-160 (+ contract twin identity-contracts-serialization-tests.cs:377-378)
- Description: The `json.ShouldNotContain("handle")` wire-check is fragile to Label content (a Label containing "handle" would false-fail); structural omission (the type has no Handle/PublicMaterial property) is the real guarantee.
- Suggestion: Assert structurally (response type / round-tripped object has no such members) or use distinctive non-colliding material, keeping the wire check as belt-and-suspenders.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: add-passkey-handler.cs:66 (challenge consume) and add-agent-key-handler.cs (same shape)
- Description: The authenticated add handlers reuse the anonymous Registration ceremony type/challenge without a Design-region note explaining why the shared type is safe. Security confirmed no confused-deputy risk (the auth boundary is the endpoint's [EndpointAuthorize], not the intent-agnostic one-time challenge; Credential.Create binds callerId), but the reuse should be documented.
- Suggestion: One Design-region sentence on each add handler: the Registration challenge is an intent-agnostic one-time liveness proof; the new-principal-vs-add distinction is enforced by the endpoint's auth boundary and the caller-sourced principal id, not the challenge — so sharing the ceremony type is safe.
- Source: general (security concurs, no risk)
- Disposition notes:

## Duplicates / conflicts

- General nit 3 (ceremony-type reuse) explicitly deferred the confused-deputy judgment to security, which found no risk → captured as M5 (documentation only).
