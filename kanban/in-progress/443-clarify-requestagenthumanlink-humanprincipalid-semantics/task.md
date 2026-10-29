# Clarify RequestAgentHumanLink humanPrincipalId semantics

## Description

`RequestAgentHumanLink_Command.humanPrincipalId` is optional (non-required
uuid). Callers cannot tell from the spec alone what omitting it means
(self-link? error?) vs the successful humanPrincipalId-pinned flow used on
2026-10-28.

## Goal (implementer restatement)

The OpenAPI document shows `humanPrincipalId` on `POST /api/agent-links` as an optional uuid.
In code it is not optional: the property is a non-nullable `Guid`, the Validator rejects an
empty value, and the handler links the calling agent only to the human it names. There is no
self-link or default human. The published contract should say that.

Acceptance:

- In `/openapi/v1.json`, the `RequestAgentHumanLink` request schema lists `humanPrincipalId` in
  `required`, with a description of what it is and what omitting it does, and a uuid example.
- Runtime behavior does not change. An omitted or empty id is still a 400 from the Validator.
- `auth.md` and `llms.txt` describe the optional human link step and say the field is required.
- A test fails if the schema loses `required`, the description, or the example.

## Checklist

- [x] Document omitting vs providing humanPrincipalId
- [x] Mark required if pinning is the contract
- [x] Add description + example; align auth.md/llms.txt wording

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implemented under ganda task work (implement oracle), 2026-10-29.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session c5708c32-27fc-453a-b85b-abc0d8c7985c, max-turns 500, Done — 2026-10-29T15:37:50Z
- Review oracle, round 1 (ganda task work, 2026-10-29): review oracle claude-fable-5-1 (orchestrator,
  gates re-run, claim verification); reviewer-general sub-agent claude-sonnet-5-5 (agent a6b5c07a7a4582d67).
  Effort 2, roster general. Artifacts: `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`. Outcome: 0 bug, 2 open (M1 suggestion, M2 nit), 0 wontfix.
  Fix pass requested for M1, M2; next round under `review/round-2/`.
- Fix pass (implement-fix-2 oracle, claude-opus-5-5, 2026-10-29): M1 and M2 fixed in auth.md.
- Review oracle: review by reviewer-claude-fable (claude, model claude-fable-5-1), session e2fb4198-9ffb-4400-bdee-d55ee91e5c92, max-turns 120 — 2026-10-29T15:44:08Z

## Review

- Round 1: Blocked — open M1, M2

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.

## Results

Pinning is the contract. The handler links the calling agent only to the human the body names.
An omitted `humanPrincipalId` binds to `Guid.Empty` and the Validator's `NotEmpty` rule returns
400. There was no self-link path to document. Only the OpenAPI document was wrong: a
non-nullable value type without a required marker comes out optional.

- `request-agent-human-link-contracts.cs`: `HumanPrincipalId` now has
  `[System.ComponentModel.DataAnnotations.Required]`, an XML `<summary>` (the human's account id
  from the Agent links page; no default, no self-link; omitted or empty is 400), and an
  `<example>` uuid. The Design region records why. `[Required]` only affects the document,
  because nothing runs DataAnnotations validation at runtime and the Validator stays the rule.
  The C# `required` keyword was not used because it breaks the `new()` constraint that
  `AgentLinkOnboardingPrompt.HttpMethod<TRequest>` relies on.
- `wwwroot/auth.md`: new "Optional human link" subsection under Agents. It has a routes table
  (request, list, approve/deny, human-ux), the `humanPrincipalId` rules, the 400/401/403/404/409
  outcomes, and a body example. It sits after the credential-management paragraph that closes
  "Onboarding tools".
- `wwwroot/llms.txt`: an agent-auth bullet for `POST /api/agent-links` that says the field is
  required with no default, and links to auth.md.
- New test `tests/container-apps/web/web-server-integration-tests/open-api-request-agent-human-link-tests.cs`
  asserts the request schema lists `humanPrincipalId` in `required`, and that the field is a
  `uuid` with a description and a non-empty uuid example. Both tests failed before the
  contract change and pass after it.

Gates run in this worktree: `ganda repo audit` passes. Its one advisory warning is
`kanban-landed`, about other tasks' claims with no worktree, which is out of scope here.
`dev build` passed with 0 warnings. `dev test -g fast` passed 14/14 projects and
`dev test -g integration` passed 8/8. `dev template-smoke` (default variant) succeeded.
`dev check-version` reports source 2.0.0-beta.29 is ahead of NuGet. The playwright group was
not run: it only checks that auth.md returns 200, and this change edits content only.

### Review (round 1, 2026-10-29)

Disposition: **not yet closed — fix pass requested** (round 1 ledger `review/round-1/merged.md`).

- Rounds: 1 so far. Roster: general (claude-sonnet-5-5) plus orchestrator verification. Effort 2.
- Counts after merge: bug 0; suggestion 1 open (M1, the credential-management paragraph in auth.md
  now sits under the new "Optional human link" heading and reads as part of it); nit 1 open (M2,
  auth.md says a non-agent caller gets 403, but a request with no valid agent bearer gets 401 from the
  agent-token scheme; 403 is only the handler backstop for a bearer whose principal is not an agent).
  0 wontfix.
- Orchestrator re-ran the gates: web-server-integration-tests build 0 warnings / 0 errors;
  `OpenApiRequestAgentHumanLink` 2 passed, 0 failed, which confirms `[Required]`, the summary, and the
  example reach the generated document; `ganda repo audit` passes (advisory `kanban-landed` only).
- Every falsifiable claim in Results and auth.md was checked against the handler, Validator, the five
  agent-links contracts and their schemes, the scope seed, and `AgentLinkOnboardingPrompt.HttpMethod<TRequest>`
  (`new()` constraint confirmed). Restatement matches the task; no misread.
- `review/disposition.md` is written when the next round reaches 0 open.

### Fix pass (round 1 findings, 2026-10-29)

- M1 fixed: in auth.md the credential-management paragraph is back at the end of "Onboarding
  tools", so it no longer reads as part of "Optional human link".
- M2 fixed: auth.md now says a request with no valid agent bearer gets **401** (agent-token
  scheme challenge) and a bearer whose principal is not an agent gets **403** (handler check).
- Both are doc-only edits to `wwwroot/auth.md`. Status updated to `fixed` in
  `review/round-1/merged.md`.

### How to validate

Smoke:

```bash
dotnet build tests/container-apps/web/web-server-integration-tests/web-server-integration-tests.csproj
(cd tests/container-apps/web/web-server-integration-tests/bin/Debug/net11.0 && ./web-server-integration-tests --filter-class OpenApiRequestAgentHumanLink)
grep -n "humanPrincipalId" source/container-apps/web/projects/web-spa/wwwroot/auth.md source/container-apps/web/projects/web-spa/wwwroot/llms.txt
```

Expect: 2/2 tests pass. In `/openapi/v1.json`, the `POST /api/agent-links` request schema has
`"required": ["humanPrincipalId"]`, and the property has `format: uuid`, a description that
says there is no default or self-link and that an omitted id is 400, and an example uuid. Both
auth.md and llms.txt say `humanPrincipalId` is required. In auth.md the credential-management
paragraph sits before `### Optional human link`, and the outcomes list 401 for no valid agent
bearer and 403 for a non-agent bearer. A deployed host shows the same thing at
`<origin>/openapi/v1.json` after this ships.
