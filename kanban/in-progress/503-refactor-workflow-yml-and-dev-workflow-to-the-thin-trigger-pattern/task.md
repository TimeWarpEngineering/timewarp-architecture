# Refactor workflow.yml and dev workflow to the thin-trigger pattern

## Depends on

- 501

## Description

Refactor `.github/workflows/workflow.yml` (currently 594 lines — 200 lines longer than any other TimeWarp repo) and `tools/dev-cli/endpoints/workflow-command.cs` to match the org thin-trigger standard (~180–220 lines, modeled on `timewarp-nuru` and `timewarp-ganda`).

All pipeline logic should live in C# (`dev workflow`), shared runner setup should live in a single local composite action, and superseded runs should be cancelled via `concurrency:`.

## Requirements

- **Extract shared setup into a local composite action** (e.g. `.github/actions/setup-ci/action.yml`, modeled on `timewarp-nuru/.github/actions/setup-dotnet`) covering checkout, `setup-dotnet` (`global.json`), NuGet cache, optional `dotnet dev-certs https --trust`, and optional pinned Aspire CLI install (reading `<AspireVersion>` from `Directory.Packages.props`). Remove the 5x copy-pasted checkout/.NET/cache blocks and 3x copy-pasted Aspire CLI install blocks from `workflow.yml`.
- **Update `dev-cli-tests` workflow guards:** Update `AspireVersionSources.Validate` and `tests/tools/dev-cli-tests/aspire-run-tests.cs` to validate the single Aspire CLI install source (composite action or C# helper) instead of grepping duplicated inline `sed` lines in `workflow.yml`. Keep `SkillSpecLint.WorkflowDriftError` (`tests/tools/dev-cli-tests/skill-spec-lint-tests.cs`) passing.
- **Unify path classification in C# (`dev workflow --phase classify`):** Emit `ci`, `skill`, and `template` outputs directly from `WorkflowCommand` (using `PullRequestBuildClassifier` + repo rules) and delete the 54-line inline PowerShell `git diff`/regex block in `detect-paths`.
- **Move ingress-golden verification into C#:** Move `regenerate-generated` + `git diff --exit-code -- goldens/ingress` + clean-status check from `workflow.yml` (`ci (fast)`) into `dev workflow --phase test --group fast`.
- **Add `concurrency:` control:** Cancel superseded pull request runs and superseded `master` pushes while keeping `release` and `workflow_dispatch` uncancelled (matching `timewarp-nuru` / `timewarp-ganda`).
- **Drop PR-level Aspire publish artifact uploads:** Remove the 4 `Upload ... publish output` steps (`compose`, `helm`, `aca`, `aca-neon`, 56 lines) from `ci-group` — `dev publish all` still runs all targets, safety tests, and `helm lint` in `ci (publish)`.
- **Align actions, CLI invocation, and comments:** Use `dotnet run --file tools/dev-cli/dev.cs -- ...`, update GitHub Actions versions to current fleet standard, add `confirm=release` break-glass guard if appropriate for `workflow_dispatch`, and trim archaeological `# Task ...` comments (move durable rationale to `Design` regions).

## Checklist

- [ ] Create local composite action for shared CI setup (checkout, .NET, NuGet cache, optional dev-cert + Aspire CLI)
- [ ] Emit `ci`, `skill`, and `template` from `dev workflow --phase classify` and remove inline PowerShell regexes
- [ ] Move `regenerate-generated` + `goldens/ingress` diff check into `dev workflow --phase test --group fast`
- [ ] Add `concurrency:` block and remove the 4 PR-level Aspire publish artifact upload steps
- [ ] Update `AspireVersionSources.Validate` / `aspire-run-tests.cs` and verify `skill-spec-lint-tests.cs`
- [ ] Run `./bin/dev build`, `dev-cli-tests`, `dev template-smoke`, and `ganda repo audit`

## Session

- Created: 2026-11-23 (from Task 500 walk performance review)

## Notes

- Reference patterns: `timewarp-nuru/.github/workflows/workflow.yml`, `timewarp-ganda/.github/workflows/workflow.yml`, and `timewarp-ganda/source/timewarp-ganda/templates/repo-baseline/github/workflows/workflow.yml.template`.
- Parent performance review: **500** (`kanban/to-do/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md`).
