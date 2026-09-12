# Review framework — task 053-008

**Date:** 2026-09-12
**Host task:** kanban/in-progress/053-008-drop-leftover-mixin-wording-outside-contractsgenerator/
**Diff scope:** branch `task/053-008-drop-leftover-mixin-wording-outside-contractsgener` vs `origin/master` (product commit `45f2b864`)
**Plan / brief:** After 053-007 renamed `ContractsMixinGenerator` → `ContractsGenerator`, drop leftover mixin wording **outside** that generator. Rename `"GeneratedMixins"` fallback to `"Generated"` on Page and StateAccess generators; rephrase stale comments (FluentValidation `Include`, `[AuthApiRequest]` attribute, Purpose regions, StateAccess tests); rephrase the todo-item DTO fossil to endpoint-centric vs entity DTO. Do not rewrite `skills/*/analysis/*` RFC snapshots or closed `kanban/done/`. Do not rename `[ApiRoute]` / `[StateAccess]` / `[Page]`. Do not change Page/StateAccess emit behavior beyond the fallback namespace string. Repo-wide `rg mixin` is not a completeness gate.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle Grok session `01a09450-af3a-7fc1-8ba9-95aede51b7c7` (2026-09-12)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
