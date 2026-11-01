# Review framework

## Budget (by-diff)

- Lines changed: 150
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope — task 459

**Date:** 2026-11-01
**Host task:** kanban/in-progress/459-consume-timewarpcontracts-from-timewarp-contracts-and-remove-the-in-repo-copy/
**Diff scope:** branch `task/459-consume-timewarpcontracts-from-timewarp-contracts` vs merge base with `master` (`3cfa29bae` product commit; kanban-only commits ignored)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)` — delete the in-repo `TimeWarp.Contracts` project, consume nuget.org `2.0.0-beta.30`, drop it from smoke/release pack lists, update skills/AGENTS.md, Nuru `4.0.0-beta.11` + audit `--fix` ride-along.
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** reviewer = Claude Opus 5.5 (ganda task work review oracle, headless print mode)

## Scope — round 2

**Date:** 2026-11-01
**Diff scope:** commit `97ff4440a` (package roll: TimeWarp.Contracts `2.0.0-beta.31`, TimeWarp.Mediator `15.0.0-beta.4`, TimeWarp.State* `12.0.0-beta.16`, Scalar.AspNetCore `2.18.7`, Microsoft.OpenApi held at `2.13.0`). The extraction was re-checked for regressions only.
**Plan / brief:** the task.md `## Session` scope add of 2026-11-01 and the updated `## Goal (implementer restatement)`.
**Effort:** 3 (by-diff budget, 1984 lines). **Roster:** general.
**Session IDs:** reviewer = Claude Opus 5.5 (claude-opus-5-5), ganda task work review oracle, headless print mode; session id not reported.

## Scope — round 3

**Date:** 2026-11-01
**Diff scope:** the fix pass on round 2. That is delta `fa07128db..285351c94`, which is commit `285351c94`: 3 files, +40/−31.
**Plan / brief:** round-2 `merged.md` findings M1 and M2, and the updated `## Goal (implementer restatement)`.
**Effort:** 2 (by-diff budget, 209 lines). **Roster:** general. **Turn cap:** 120.
**Session IDs:** reviewer = Claude Opus 5.5 (claude-opus-5-5), ganda task work review-2 oracle, headless print mode; session id not reported.

## Scope — round 4

**Date:** 2026-11-01
**Diff scope:** the fix pass on round 3, delta `285351c94..3cd8eef26`. The only product change is the `catalog-agent-tests.cs` Design region, +2/−2.
**Plan / brief:** round-3 `merged.md` finding M3, and the updated `## Goal (implementer restatement)`.
**Effort:** 1 (by-diff budget, 104 lines). **Roster:** general. **Turn cap:** 80.
**Session IDs:** reviewer = Claude Opus 5.5 (claude-opus-5-5), ganda task work review-3 oracle, headless print mode; session id not reported.

## Scope — round 5

**Date:** 2026-11-01
**Diff scope:** the CI fix for PR #620 (run 41281550727, `aspire-tests`), commit `278424009`, delta `3cd8eef26..0151ed962`. Product changes: `skills/tw-deploy/SKILL.md`, the AppHost `program.cs` Design region, and five `aspire-tests` files (+23/−16).
**Plan / brief:** the `## Session` CI-red note and the updated `## Goal (implementer restatement)`. Follow the `TimeWarp.State.DevTools.Hosting` `12.0.0-beta.16` default name `timewarp-devtools`, and never pin State backward.
**Effort:** 3 (by-diff budget, 2377 lines). **Roster:** general. **Turn cap:** 200.
**Session IDs:** reviewer = Claude Opus 5.5 (claude-opus-5-5), ganda task work review oracle, headless print mode; session id not reported.

## Scope — round 6

**Date:** 2026-11-01
**Diff scope:** the fix pass on round 5, delta `0151ed962..1ca6eae81`. The only product changes are two Design regions, in `aspire-app-host/program.cs` and `state-dev-tools-model-tests.cs`, and they change comments only (+4/−6).
**Plan / brief:** round-5 `merged.md` finding M4, and the updated `## Goal (implementer restatement)`.
**Effort:** 1 (by-diff budget, 150 lines). **Roster:** general. **Turn cap:** 80.
**Session IDs:** reviewer = Claude Opus 5.5 (claude-opus-5-5), ganda task work review-2 oracle, headless print mode; session id not reported.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
