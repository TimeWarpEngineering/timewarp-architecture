# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** e653349df (workflow skill-lint job, AGENTS.md, evals/readme.md, `lint-skills-command.cs`, `skill-spec-lint.cs`, `vally-cli.version`, dev-cli-tests)

## Summary

The change adds `dev lint-skills`, pins vally at `0.19.0` in one file, and makes the CI skill-lint job install that pin and call the verb. `SkillSpecLint.WorkflowDriftError` fails dev-cli-tests if the workflow stops reading the pin or runs `vally lint` itself. Every checklist item is met, and the implementer restatement matches the task. Risk is low. The only issue found is a Windows gap in the PATH probe.

Re-verified on 2026-10-22 in the claim worktree:

- `cd tests/tools/dev-cli-tests && dotnet test -c Release`: 333 passed, 0 failed.
- `dotnet run tools/dev-cli/dev.cs -- lint-skills` with vally 0.6.0 on PATH: exit 1, message names 0.6.0, the pin, and `npm install -g @microsoft/vally-cli@0.19.0`.
- Same verb with `@microsoft/vally-cli@0.19.0` installed to a temp prefix ahead on PATH: `9 skill(s) linted, 9 passed`, the eval spec is valid, exit 0.
- Temp git repo with only the pin file and `skills/too-long/SKILL.md` at 527 lines: exit 1, vally's `File length (527 lines) exceeds limit (500).`

Also checked: the exit-code style (`Environment.ExitCode` with a `Unit` return) matches `test-command.cs`. The setup-dotnet block matches the other jobs. Adding `^tools/dev-cli/` to the skill path filter is needed, because the job now compiles the dev CLI. `tools/dev-cli/` ships in generated apps (template.json excludes only `tests/tools/dev-cli-tests/**`), so the verb and the pin file ship too. That is consistent with `skills/` shipping.

## Issues

### Issue 1 — Severity: nit
- File: tools/dev-cli/services/skill-spec-lint.cs:106
- Description: `IsOnPath` probes `vally` and `vally.exe`. On Windows, `npm install -g` creates `vally.cmd` and `vally.ps1` shims and no `.exe`, so an installed vally is reported as "not on PATH". Fixing the probe alone would not be enough: `Shell.Builder("vally")` would also have to resolve the `.cmd` shim (PATHEXT) to launch it.
- Suggestion: Either drop the `.exe` probe and state that the verb is Linux/macOS/WSL-only, or resolve PATHEXT in both the probe and the launch.
- Status: open
