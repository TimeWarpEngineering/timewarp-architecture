# Review framework — task 525

**Date:** 2026-11-25
**Host task:** kanban/in-progress/525-fix-dev-db-add-migration-snapshot-output-directory/
**Diff scope:** branch `task/525-fix-dev-db-add-migration-snapshot-output-directory` vs `master` (merge-base). Product files: `tools/dev-cli/services/db-ef.cs`, `tools/dev-cli/endpoints/db-add-migration-command.cs`, `tests/tools/dev-cli-tests/db-ef-tests.cs`. Kanban move of `task.md` is in the same range. 472 insertions, 45 deletions on that range.
**Plan / brief:** `dev db add-migration` must leave `PostgresDbContextModelSnapshot.cs` in `source/container-apps/web/platform/postgres/migrations/` and must not leave a stray `TimeWarp/Architecture/Persistence/Migrations/` tree under `web-infrastructure`. Post-scaffold reconciliation in `DbEf`, called from `DbAddMigrationCommand`.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle Grok 4.7 session `01a21391-c338-7db1-952c-987f0ceb4868`. Implementer session `ef5ca338-ed06-4a4c-9a8c-211ab6a5cc56` (recorded on `task.md`).

## Budget (by-diff)

- Lines changed: 517
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
