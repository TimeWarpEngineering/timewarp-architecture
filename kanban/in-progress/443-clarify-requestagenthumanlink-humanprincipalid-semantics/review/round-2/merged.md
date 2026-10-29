# Round 2 — merged findings
**Date:** 2026-10-29
**Sources:** general (review oracle claude-fable-5-1, inline; effort 1 per host Budget.ByDiff, 49 lines)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/wwwroot/auth.md:290-294
- Description (round 1): the credential-management paragraph sat under the new `### Optional human link` heading and read as part of it.
- Verification (round 2): paragraph is at auth.md:290-292, inside "Onboarding tools"; `### Optional human link` follows at 294 and closes with the JSON example before the `---` at 318.
- Source: general
- Disposition notes: fixed in `e6b00753c`. Verified in round 2.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/wwwroot/auth.md:309-310
- Description (round 1): "A caller that is not an agent gets 403" glossed over the 401 a request with no valid agent bearer receives from the agent-token scheme.
- Verification (round 2): text now reads "A request with no valid agent bearer gets **401**. A bearer whose principal is not an agent gets **403**." Confirmed against `agent-token-authentication-scheme-server.cs:126-137` (401 challenge) and `request-agent-human-link-handler-application.cs:49-51` (403 for non-Agent kind).
- Source: general (confirmed by orchestrator in round 1)
- Disposition notes: fixed in `e6b00753c`. Verified in round 2.

## New findings

None.

## Duplicates / conflicts

- None.
