# Round 4 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/profile/get-profile/get-profile-contracts.cs:28
- Description: The GetProfile `[EndpointAllowAnonymous]` reason is published in OpenAPI, and it still contains internal task ids (`task 148 D3 / 182-003`).
- Suggestion: Change only the attribute reason to spec-reader prose, for example: "Dual-mode read: an anonymous caller gets the demo profile, and a signed-in caller gets their stored profile." Leave the task history in the Design region. Check the `OpenApiAnonymousIntent_` tests that assert on the description.
- Source: general (round 1, carried through rounds 2–4)
- Disposition notes: Still unfixed at HEAD `0c6ffb48`. The walk was resumed with no operator ruling, so this round asks for another fix pass. It is a one-line edit. If the next fix pass is also a no-op (`num_turns: 1`, no commit), the next reviewer should park the walk as a stalemate again rather than keep looping.
- Fix (implement-fix-4, Claude Opus 5.5, 2026-10-29): the attribute reason is now "This read is dual-mode: an anonymous caller gets the demo profile, and a signed-in caller gets their stored profile." Task ids stay in the Design region only. `OpenApiAnonymousIntent_` asserts the `dual-mode` fragment and still passes (6/6). No other `[EndpointAllowAnonymous]` reason carries a task id.

## Duplicates / conflicts

- None. The delta was empty, so there are no new findings.
