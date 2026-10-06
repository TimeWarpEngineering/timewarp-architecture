# Round 1 — tests
**Date:** 2026-10-07
**Scope reviewed:** commits 2094f4636, 98bdb0a3c; tests/tools/dev-cli-tests/aspire-deploy-tests.cs (+csproj), tests/container-apps/aspire/aspire-tests/deployment-record-model-tests.cs (+csproj); against aspire-deploy.cs, aspire-deploy-preflight.cs, deploy-command.cs, deprovision-command.cs.

## Summary
dev-cli-tests: 129/129 pass (Release). aspire-tests DeploymentRecordModel_Given_ not run (Docker/Aspire testing host; not needed for this lens).
The pure helpers (target resolution, argument building, Helm/kubectl validation, record lookup, no-record / refusal text) are asserted with concrete expected values, not tautologies. All tests are pure functions fed literals: nothing touches the real ~/.aspire or HOME (AspireHome takes the profile as a parameter). The weak spot is that the command handlers (ordering of preflight, no-record, --yes gate, exit codes) and option binding are not exercised at all, and the never-CI guard has narrow coverage.

## Issues
### Issue 1 — Severity: suggestion
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs:237 (OperatorText_Given_) / tools/dev-cli/endpoints/deprovision-command.cs:55-90
- Description: The destructive-path confirmation is only tested as text. `DeprovisionWithoutYes_Should_ShowTheRecordAndRequireYes` checks `BuildDeprovisionRefusalLines`; nothing asserts that the handler actually refuses (does not invoke `aspire destroy`, ExitCode 1) when `--yes` is absent, or that no-record never reaches destroy. The ordering (record check before --yes check, destroy only if both pass) lives in the handler and is untested; a refactor that dropped the `if (!command.Yes)` branch would keep all tests green. Same for deploy-command's confirmation (`DeployConfirmationRefusal` is only checked to contain "--yes").
- Suggestion: Extract the decision (record?, yes) -> {NoRecord, NeedYes, Destroy} as a pure function in aspire-deploy.cs and assert all combinations, or drive the handler with a fake ITerminal and a Shell seam.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs:14-35
- Description: Task requirement says tests cover "argument parsing". Only `ResolveTarget` (string -> target) is covered; the Nuru route/option binding (`--target`/`-t`, `--yes`/`-y`, absence of `--yes` defaulting false) is not tested, and ExitCode behavior on bad target is not either.
- Suggestion: Add a test that routes `deprovision --target swarm` / `deploy -t kubernetes` through the app's route table (as other dev-cli tests may do) or state that ResolveTarget is the intended contract.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs:290
- Description: NeverAutomated guard scans only `.github/workflows/*.yml` plus workflow-command.cs. Gaps: `*.yaml` workflows and `.github/actions/**` (composite actions) are not scanned; other dev-cli sources (e.g. release/other commands that might shell `aspire deploy`) and other CI-called scripts are not scanned. The regex also misses forms with an intervening flag/word: `aspire --non-interactive deploy`, `aspire do deploy`, `dev -v deploy`. It does catch `./bin/dev deploy`, `dev deprovision`, `dev.cs -- deploy`, `aspire deploy`/`aspire destroy` (verified by reading the pattern; `\b` matches after `/`). It will also false-positive on a YAML comment such as `# never run dev deploy here`, and no test proves the regex matches the positive samples, so a regex regression to match-nothing would pass silently.
- Suggestion: Glob `*.y*ml` recursively under .github; add a positive-control test asserting `DeployInvocation()` matches `./bin/dev deploy`, `dev deprovision`, `aspire destroy`, `dotnet run tools/dev-cli/dev.cs deploy`; skip comment lines or accept the false positive deliberately.
- Status: open

### Issue 4 — Severity: nit
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs:307
- Description: RepoRoot walks up from AppContext.BaseDirectory looking for `.git`; works for worktrees (.git file) but fails on a source tarball / CI checkout without .git (not the case with checkout action). Acceptable, minor brittleness.
- Suggestion: None required; optionally walk to the dir containing `.github`.
- Status: open

### Issue 5 — Severity: nit
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs:60-66, 187-205
- Description: Several assertions are substring-based on prose (e.g. "Nothing was run", "nothing was removed", "data", "docker" absent via ShouldNotContain("docker")). ShouldNotContain("docker") on the compose no-record text will break if any wording mentions Docker (e.g. "Docker volume"); `ShouldContain("data")` is near-vacuous. Mildly brittle/weak.
- Suggestion: Assert on structured fragments (the exact manual command line) and drop the generic "data" check or tighten it to the phrase actually warned.
- Status: open
