# Review framework — task 458-006

**Date:** 2026-11-26
**Host task:** kanban/in-progress/458-006-publish-required-request-members-from-validator-rules-in-open-api/
**Diff scope:** branch `task/458-006-publish-required-request-members-from-validator` vs `master` (merge-base `d9e33975`, ahead 3). Product diff: `open-api-validator-required.cs`, `common-server-module.cs`, `request-agent-human-link-contracts.cs`, `skills/tw-web-api-contracts/SKILL.md`, `open-api-required-request-members-tests.cs`, `open-api-request-agent-human-link-tests.cs` (753 insertions, 47 deletions, including the kanban move).
**Plan / brief:** Publish OpenAPI `required` from the same FluentValidation `NotEmpty` / `NotNull` rules `FluentValidationBehavior` runs, drop the OpenAPI-only `[Required]` on `RequestAgentHumanLink.Command.HumanPrincipalId`, fail `/openapi/{doc}.json` when a later transformer drops that `required`, and state the rule in `skills/tw-web-api-contracts/`.
**Effort:** 3 (by-diff, 800 lines). Roster is general only.
**Reviewer roster:** general
**Session IDs:** review oracle grok 4.7, session `01a2159e-bf23-7dd3-872c-d3d4f8d7b82a`. Implementer session `b60be27f-5fba-4a2b-8da0-eb8fdb9415ed` (claude-opus-5-5).

## Budget (by-diff)

- Lines changed: 800
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
