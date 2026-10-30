# Round 2 — general
**Date:** 2026-10-29
**Scope reviewed:** fix commit `e6b00753c` (round-1 fix pass) vs round-1 base `7afc5224a`. Product delta: `source/container-apps/web/projects/web-spa/wwwroot/auth.md` only (15 lines). Kitchen edits (`task.md`, `review/round-1/merged.md`) read for status, not reviewed as product.

## Summary

The fix pass is doc-only and does exactly what the round-1 ledger asked. The credential-management paragraph is back at the end of "Onboarding tools" (auth.md:290-292), ahead of `### Optional human link` (auth.md:294), so M1 is resolved. The outcomes sentence now separates 401 (no valid agent bearer) from 403 (bearer whose principal is not an agent) at auth.md:309-310, so M2 is resolved. No product file other than auth.md changed, so runtime, the contract, llms.txt, and the schema test are untouched since round 1. No new defects.

## Verified claims

1. M1 placement: `grep -n '^### '` on auth.md shows "Onboarding tools" at 276, "Optional human link" at 294, the `---` before "Payment" at 318. The credential paragraph sits at 290-292, inside "Onboarding tools". The link subsection still ends with the JSON body example (314-316) immediately before the `---`.
2. M2 401 claim: `agent-token-authentication-scheme-server.cs:126-137` `HandleChallengeAsync` sets `Status401Unauthorized`, `WWW-Authenticate: Bearer` (or `Bearer error="invalid_token"` when a token was presented and rejected), and a problem body "A valid agent bearer token is required." The endpoint is `AuthenticationSchemes = AuthenticationSchemeNames.AgentToken` (`request-agent-human-link-contracts.cs:21`), so a human session cookie or no header reaches this challenge. "A request with no valid agent bearer gets 401" is accurate for both the missing and the rejected-token cases.
3. M2 403 claim: `request-agent-human-link-handler-application.cs:49-51` returns `AgentLinkProblems.Forbidden` when `caller is null || caller.Kind != PrincipalKind.Agent`. "A bearer whose principal is not an agent gets 403" matches. The contracts Design region (`request-agent-human-link-contracts.cs:6`, "Agent-token only") needs no change, as round 1 concluded.
4. Delta scope: `git diff --stat 7afc5224a..HEAD` lists only auth.md among product paths. Nothing to re-run for build or schema tests; round-1 orchestrator results (build 0/0, `OpenApiRequestAgentHumanLink` 2/2, `ganda repo audit` pass) still describe the tree.
5. Restatement vs task: unchanged since round 1. `## Goal (implementer restatement)` matches the task Description and the three checklist items. No misread.

## Issues

None. Both round-1 findings are fixed; the fix delta introduces no new finding.

Observation (not a finding, outside the delta): auth.md:297 says the agent calls with a bearer that has scope `identity:read`, and the outcomes list does not spell out the 403 `insufficient_scope` a valid bearer without that scope receives from `HandleForbiddenAsync`. The 403 sentence as written is true and does not claim to be exhaustive. Pre-existing round-1 text; not raised.
