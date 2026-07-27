# Review framework — task 126-003

**Date:** 2026-07-27
**Host task:** kanban/in-progress/126-003-add-post-publish-release-gate-generate-published-template-against-real-nugetorg/
**Diff scope:** commit `76d38327` — feat(dev-cli): add post-publish template-publish-smoke release gate
**Plan / brief:** Required post-publish gate after real nuget.org push: flatcontainer wait, clean hive install, generate (≠ sourceName), pin assert, nuget.org-only restore+build. Leave template-smoke untouched. Failure blocks release.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator grok-build 2026-07-27

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Files in scope

- `tools/dev-cli/endpoints/template-publish-smoke-command.cs` (new)
- `tools/dev-cli/endpoints/workflow-command.cs` (wire after push)
- `documentation/developer/how-to-guides/HowToRelease.md` (new)
- `documentation/developer/how-to-guides/Overview.md` (link)
- `.github/workflows/workflow.yml` (comment only)

## Checklist claims to verify

- Insertion after PushAsync only when API key present (pack-only skips)
- Flatcontainer wait (not website search index); bounded retry
- Clean hive isolation (DOTNET_CLI_HOME, NUGET_PACKAGES, nuget.org-only config)
- Pin assert + synthetic self-check for stale-pin class
- Matrix default + --postgres false
- template-smoke untouched
- Manual fallback documented
- Failure blocks (ExitCode nonzero)
