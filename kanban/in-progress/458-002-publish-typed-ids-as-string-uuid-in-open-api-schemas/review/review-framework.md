# Review framework — task 458-002

**Date:** 2026-11-26
**Host task:** kanban/in-progress/458-002-publish-typed-ids-as-string-uuid-in-open-api-schemas/
**Diff scope:** branch `task/458-002-publish-typed-ids-as-string-uuid-in-open-api` vs `master` (product: `source/foundation/foundation-server/open-api-typed-id.cs`, `common-server-module.cs`, `tests/container-apps/web/web-server-integration-tests/open-api-typed-id-tests.cs`)
**Plan / brief:** Publish every `[TypedId]` OpenAPI schema as `type: string, format: uuid` (keep the description) and `Nullable<T>` of one as `type: ["null","string"], format: uuid`. Recognize typed ids from the generator stamp, not a name list. A document test fails when any component schema is untyped.
**Effort:** 2 (by-diff; roster axis general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a2155c-7760-7680-b020-1670544592ee`; implementer session recorded on `task.md`

## Budget (by-diff)

- Lines changed: 418
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
