# Round 1 — general
**Date:** 2026-09-21
**Scope reviewed:** branch task/242-bring-master-back-to-a-clean-ganda-repo-audit-beta vs origin/master

## Summary

The branch restores a clean `ganda repo audit` (verified locally: 28/28, exit 0) by committing +x on 26 shebang runfiles, standardizing the 238 research shebang, adding TW0007 filename under `[*.cs]` with a forward-only SourceGenerators pin to 1.0.0-beta.11, scaffolding memsearch hooks via the fixer (including post-* `ganda repo attest`), setting `peacock.color`, and adding a `repo-audit` CI job that installs TimeWarp.Ganda from GitHub Packages. Overall risk is low: metadata/config/hooks/CI only, with TW0007 severity intentionally left package-default off. Dominant themes are CI-guard hardening gaps (unpinned tool install soft-fallback; `kanban/**` outside workflow path filters) rather than product regressions.

## Issues

### Issue 1 — Severity: suggestion
- File: .github/workflows/workflow.yml:173
- Description: `dotnet tool install --global TimeWarp.Ganda --prerelease` is unpinned while the temp nuget.config still lists nuget.org (max published `1.0.0-beta.15`) alongside GitHub Packages. That mirrors `timewarp-ganda/install-ganda.cs`, and with working `packages: read` auth the highest version on the org feed should win. If the GitHub Packages source soft-fails (401/403) and NuGet continues with nuget.org, CI can install beta.15 and still exit 0 — a stale tool that predates checks such as `global-usings-analyzer` (landed 2026-09-20), making the new guard a silent no-op for the exact audit surface this task restored.
- Suggestion: Pin an explicit floor (`--version 1.0.0-beta.29` or current) and/or remove nuget.org / add `packageSourceMapping` so `TimeWarp.Ganda` resolves only from `github`. Prefer fail-closed install over falling back to the public feed.
- Status: open

### Issue 2 — Severity: suggestion
- File: .github/workflows/workflow.yml:16-33 (and matching `pull_request.paths` 44-60)
- Description: Workflow path filters correctly cover `source/**`, `.editorconfig`, and `.githooks/**`, and `repo-audit` is correctly not gated on `detect-paths` (so editorconfig/hooks-only PRs still run audit when the workflow fires). They do **not** include `kanban/**`. A PR that only adds or mode-fixes a kanban research shebang runfile (today: `kanban/done/238-…/research/run-rank-experiment.cs`) never starts the workflow, so `repo-audit` cannot catch `runfile-executable` / `runfile-shebang` regressions there. New co-located `*-tests.cs` under `source/` are covered.
- Suggestion: Add `kanban/**` (or a tighter `kanban/**/*.cs`) to both push and pull_request path filters, or document that kanban runfiles are intentionally outside the CI guard.
- Status: open
