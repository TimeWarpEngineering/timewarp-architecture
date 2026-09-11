# Review framework — task 053-007

**Date:** 2026-09-12
**Host task:** kanban/in-progress/053-007-rename-contractsmixingenerator-drop-leftover-mixin-wording/
**Diff scope:** branch `task/053-007-rename-contractsmixingenerator-drop-leftover-mixin` vs `origin/master` (product commit `25dd0a58`, Results `dcb2e589`)
**Plan / brief:** Rename leftover mixin wording off the bundled contracts generator. Canonical names: `ContractsMixinGenerator` → `ContractsGenerator`, `contracts-mixin-generator.cs` → `contracts-generator.cs`, `ContractsMixinGenerator_Tests` → `ContractsGenerator_Tests`, `contracts-mixin-generator-tests.cs` → `contracts-generator-tests.cs`, `ContractsMixinAttributes.g.cs` → `ContractsGeneratorAttributes.g.cs`, `MixinHintNames` → `HintNames`, `CreateMixinProvider` → `CreateAttributeProvider`. Sweep foundation-contracts-generators, analyzer/sourcegenerator tests, skills (`tw-web-api-contracts` + analysis), documentation/how-to, `#region` Purpose/Design. Do not change emit behavior (parser 053-003, FQN 053-004, incrementality 053-005, slimmer members 053-006). Do not rename `[ApiRoute]` / `[AuthApiRequest]` / `[OpenDataQueryParameters]` or `IAuthApiRequest`. Historical `kanban/done/` snapshots and analysis RFC `[RouteMixin]` ballots may stay; generator type/file paths in analysis should match current names.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle Grok session `01a0916f-df56-79e1-9c5f-0c0cb4d01228` (2026-09-12)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
