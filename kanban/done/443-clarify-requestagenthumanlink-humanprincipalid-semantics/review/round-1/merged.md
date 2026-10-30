# Round 1 — merged findings
**Date:** 2026-10-29
**Sources:** general (reviewer-general sub-agent, claude-sonnet-5-5) + orchestrator gate re-run (review oracle, claude-fable-5-1)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Orchestrator verification

Re-run in this worktree on 2026-10-29, independent of the implementer's report:

- `dotnet build tests/container-apps/web/web-server-integration-tests/web-server-integration-tests.csproj`: 0 warnings, 0 errors.
- `web-server-integration-tests --filter-class OpenApiRequestAgentHumanLink`: 2 passed, 0 failed. So the XML `<summary>` and `<example>` do reach the generated document, and `[Required]` does land in the schema's `required` array. The `ShouldContain("account id")` hedge in general.md is moot: the test passes against "Account id of the human…".
- `ganda repo audit`: passes. The one advisory is `kanban-landed` (other tasks' claims with no worktree), out of scope here, same as the implementer reported.
- Handler, Validator, routes, schemes, and the `new()` constraint claim: all verified by the general reviewer with file:line evidence (see `general.md` "Verified claims"). The orchestrator re-read the handler and the agent-token authentication handler: `HandleChallengeAsync` sets 401 with `WWW-Authenticate: Bearer`, which confirms M2.
- Restatement vs task: the `## Goal (implementer restatement)` matches the task Description and checklist (required-if-pinning, description + example, auth.md/llms.txt alignment, runtime unchanged). No misread.

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/wwwroot/auth.md:290-314
- Description: The new `### Optional human link` subsection was inserted between "Onboarding tools" and the paragraph "Credential management (list/add/revoke) uses `/api/identity/credentials*` …". That paragraph closed "Onboarding tools" before; it now sits under "Optional human link" and reads as part of the link section even though it is about credential management.
- Suggestion: Move the new subsection after the credential-management paragraph, so it sits just before the `---` that precedes "Payment". Or give the credential paragraph its own short heading.
- Source: general
- Disposition notes: fix pass requested (round 1). Cheap, and the task's own acceptance is that auth.md describes the step correctly; a reader-facing structure defect in that same section is in scope.
  Fixed (fix pass, 2026-10-29): the credential-management paragraph moved back to the end of "Onboarding tools", before the `### Optional human link` heading.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/wwwroot/auth.md:305
- Description: "A caller that is not an agent gets **403**" is only partly right. The endpoint is restricted to `AuthenticationSchemeNames.AgentToken` (`request-agent-human-link-contracts.cs:21`), so a human session cookie with no bearer fails authentication and gets **401** from `HandleChallengeAsync` in `agent-token-authentication-handler-server.cs`. The handler's 403 (`request-agent-human-link-handler-application.cs`, `caller.Kind != PrincipalKind.Agent`) is a defense-in-depth branch for a bearer whose principal is not of kind Agent.
- Suggestion: Reword to the effect of "A request with no valid agent bearer gets **401**. A bearer whose principal is not an agent gets **403**." The contracts file Design region line "Handler rejects non-Agent callers" is already accurate and needs no change.
- Source: general (confirmed by orchestrator)
- Disposition notes: fix pass requested (round 1). Doc-only, one sentence.
  Fixed (fix pass, 2026-10-29): auth.md now says a request with no valid agent bearer gets **401** and a bearer whose principal is not an agent gets **403**.

## Duplicates / conflicts

- None. The two findings are distinct and both from the single general reviewer.
