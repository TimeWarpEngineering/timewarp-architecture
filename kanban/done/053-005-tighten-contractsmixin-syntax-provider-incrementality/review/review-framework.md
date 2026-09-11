# Review framework — task 053-005

**Date:** 2026-09-11
**Host task:** kanban/in-progress/053-005-tighten-contractsmixin-syntax-provider-incrementality/
**Diff scope:** branch `task/053-005-tighten-contractsmixin-syntax-provider-incremental` vs `origin/master` (product commit `9de9b74e`, Results `40ff0c05`)
**Plan / brief:** Tighten ContractsMixin incrementality leftover after 053-004 (`ForAttributeWithMetadataName` already landed). Predicate requires partial class (skip records/structs); equatable `record struct Target` with `ImmutableArray` + content `Equals`; one `{fqn}.g.cs` per type (merge the three attribute pipelines); skip emit when `Target` is unchanged. Out of scope: public attribute namespace (053-004), route parser (053-003), which members are generated (053-006).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle Grok session `01a08f94-8716-7961-a22f-87afc085beae` (2026-09-11)

Round 2 (2026-09-11): re-verify M1 against the post-fix uncommitted delta (`Transform` first-wins same-kind parts; AllowMultiple test compiles generated trees). Round 1 files are frozen. Carry M1 as `fixed` if confirmed. Scan the fix delta for new defects.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
