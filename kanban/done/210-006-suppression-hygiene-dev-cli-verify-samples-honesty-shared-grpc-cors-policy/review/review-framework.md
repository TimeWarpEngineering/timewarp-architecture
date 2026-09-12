# Review framework — task 210-006

**Date:** 2026-09-12
**Host task:** kanban/in-progress/210-006-suppression-hygiene-dev-cli-verify-samples-honesty-shared-grpc-cors-policy/
**Diff scope:** branch `task/210-006-suppression-hygiene-dev-cli-verify-samples-honesty` vs `origin/master` (commit `ec10e254`). Product paths: `tools/dev-cli/endpoints/verify-samples-command.cs`, `source/container-apps/Directory.Build.props`, `source/foundation/Directory.Build.props`, `source/container-apps/grpc/projects/grpc-server/{program.cs,grpc-server.csproj,global-usings.cs}`, `source/container-apps/web/projects/web-server/web-server.csproj`, `source/container-apps/web/projects/web-spa/{program.cs,global-suppressions.cs,features/event-stream/pipeline/event-stream-behavior.cs,features/profile-menu/profile-menu-state/profile-menu-state.toggle.cs}`, `source/foundation/foundation-server/cors-policy/{cors-policy.cs,cors-policies/any-policy.cs}`. Parent ledger updates under `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md` are in scope only as status bookkeeping, not as a second product review.
**Plan / brief:** Close parent 210 findings M30, M32, M33, M34, M36 — honest `verify-samples`, curated `<NoWarn>` lists, real Program suppressions (or delete), shared `CorsPolicy` exposed-headers overload consumed by grpc-server, and task 211 for profile-menu transitions.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle `01a09610-ef49-77e1-9acc-13c3f759733a` (2026-09-12)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
