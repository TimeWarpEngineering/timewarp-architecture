# Round 1 — merged findings
**Date:** 2026-09-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: .github/workflows/workflow.yml:173
- Description: `dotnet tool install --global TimeWarp.Ganda --prerelease` is unpinned while the temp nuget.config still lists nuget.org (max published `1.0.0-beta.15`) alongside GitHub Packages. That mirrors `timewarp-ganda/install-ganda.cs`, and with working `packages: read` auth the highest version on the org feed should win. If the GitHub Packages source soft-fails (401/403) and NuGet continues with nuget.org, CI can install beta.15 and still exit 0 — a stale tool that predates checks such as `global-usings-analyzer` (landed 2026-09-20), making the new guard a silent no-op for the exact audit surface this task restored.
- Suggestion: Pin an explicit floor (`--version 1.0.0-beta.29` or current) and/or remove nuget.org / add `packageSourceMapping` so `TimeWarp.Ganda` resolves only from `github`. Prefer fail-closed install over falling back to the public feed.
- Source: general
- Disposition notes: Fixed 2026-09-21 — nuget.org dropped from the install configfile (github only) and a post-install version gate refuses nuget.org-vintage `1.0.0-beta.15` or earlier. Fail closed if GitHub Packages does not supply a current tool.

### M2 — Severity: suggestion — Status: wontfix
- File: .github/workflows/workflow.yml:16-33 (and matching `pull_request.paths` 44-60)
- Description: Workflow path filters correctly cover `source/**`, `.editorconfig`, and `.githooks/**`, and `repo-audit` is correctly not gated on `detect-paths` (so editorconfig/hooks-only PRs still run audit when the workflow fires). They do **not** include `kanban/**`. A PR that only adds or mode-fixes a kanban research shebang runfile (today: `kanban/done/238-…/research/run-rank-experiment.cs`) never starts the workflow, so `repo-audit` cannot catch `runfile-executable` / `runfile-shebang` regressions there. New co-located `*-tests.cs` under `source/` are covered.
- Suggestion: Add `kanban/**` (or a tighter `kanban/**/*.cs`) to both push and pull_request path filters, or document that kanban runfiles are intentionally outside the CI guard.
- Source: general
- Disposition notes: Wontfix 2026-09-21 — orchestrator. Task requirement scoped the CI guard to new co-located `*-tests.cs` runfiles (under `source/**`, already in path filters). Adding `kanban/**` would start this workflow on every kanban-only PR. Kanban research runfiles stay a local `ganda repo audit` concern (tw-pr gate) rather than a CI path.

## Duplicates / conflicts

- None (single reviewer).
