# Review framework — task 210-002

**Date:** 2026-09-12
**Host task:** kanban/to-do/210-002-fix-namespace-and-placement-violations-at-the-featuresplatform-boundary/
**Diff scope:** branch `task/210-002-fix-namespace-and-placement-violations-at-the-feat` vs `origin/master` (commit `dcbefd87`)
**Plan / brief:** Implementer closed parent 210 round-1 findings M3–M10: authorization engine + payment port to `platform/` with non-Features namespaces; permission ids stay Features substrate; SPA pages rehomed; TWA0015/0016 features-only scope documented; kebab exceptions listed. Requirements in `task.md`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Round 2:** re-review after M1 snapshot FQN retarget (round-1 files frozen)
**Session IDs:** review oracle grok session 01a095dd-3879-7d10-b70c-cd2e1c38c7b7 (2026-09-12); implementer grok session 01a095bd-66cd-75c3-91e6-0daf89bc14bc (2026-09-12)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Brief for general

Verify the implementer actually closed M3–M10 as specified in `task.md` Requirements, including:

- M8 split: engine under `web/platform/authorization/` (namespace `TimeWarp.Architecture.Authorization`); payment under `web/platform/payment/` (namespace `TimeWarp.Architecture.Payment`); ids remain under `features/authorization/` in bare `…Features`; Design regions no longer claim Features substrate for moved files; `tw-feature-placement` updated; leftover empty `features/payment/` or Features namespaces in `platform/` would be bugs.
- M3: api identity-host handler uses a platform namespace, not `Features.Identity`.
- M4: `agent-caller-permission-scope-source-server.cs` lives in `platform/authorization/` with a platform namespace.
- M5: `DeleteTodoItem` is `Features.TodoItems` (not `.Commands`); `[ApiRoute]` + `[ClientOnlyContract]`; DTO TODO gone.
- M6: hosted identity-session auth-state provider is in `web/platform/identity-host/`.
- M7: six pages in their feature `pages/` folders; root `web-spa/pages/` gone.
- M9: TWA0015/0016 features-only scope documented (analyzer Design, skill, AGENTS.md) or extended; analyzer test present.
- M10: `SKILL.md` and `AnalyzerReleases.{Shipped,Unshipped}.md` on kebab exception tables.
- Call sites: global-usings, handlers that dropped `using TimeWarp.Architecture.Features`, template.json postgres excludes, leftover empty folders, SPA `_Imports`/routes.
