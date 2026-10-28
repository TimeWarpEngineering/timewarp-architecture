# Round 1 — merged findings
**Date:** 2026-10-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/agent-links/agent-link-onboarding-prompt.cs:32
- Description: The prompt tells the agent to GET `api/agent-links/<linkId>/human-ux` "with the same bearer" after approval. The script keeps the bearer only in `$access`, which is lost when the script exits. The KeyId and link id are only printed, and the token expires after 15 minutes by default with no refresh. The step that fetches the `timewarp.humanUx/v1` handoff cannot run as written.
- Suggestion: Save the KeyId and link id to files in `$dir` and print `$dir`. Tell the agent to mint a fresh token (Token.v1 ceremony with the saved key and KeyId) after approval, then GET `human-ux`. Do not persist the bearer.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/agent-links/agent-link-onboarding-prompt.cs:74
- Description: The recipe hard-codes six contracts' routes and JSON member names, and the Playwright test repeats the same literals. Nothing ties the prompt to the contracts, so a rename such as 248-001's `label` → `nickname` would ship a broken prompt with green tests. AGENTS.md hard rule: two things that must agree get a generator or a build check.
- Suggestion: Add a Jaribu test that runs the prompt's bash against the in-proc web host and asserts a Pending link for the signed-in human. Alternatively, build the routes and member names in `Build` from the contract types.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
