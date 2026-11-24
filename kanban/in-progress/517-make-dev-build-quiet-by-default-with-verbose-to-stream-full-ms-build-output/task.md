# Make dev build quiet by default with --verbose to stream full MSBuild output

## Description

Walk performance analysis (Task 514, `/tw-correct` Hand-off B) found repeated cases (`499`, `483`, `503`) of agents piping `./bin/dev build` into `| tail -5` / `| head` to avoid verbose MSBuild output — truncating compiler diagnostics, masking exit codes when `pipefail` is off, and forcing a second full build (`~46.8s` wasted on Task 503 alone).

`tools/dev-cli/endpoints/build-command.cs` already implements quiet capture (`--quiet` / `-q`: capture MSBuild output and print it only on failure), but defaults to streaming full MSBuild output. Making quiet-on-success the default removes the incentive for agents to pipe `./bin/dev build` through `tail`/`head` (parallel to `timewarp-ganda` Task 683 for `ganda repo audit`).

## Depends on

- 516

## Goal (implementer restatement)

`dev build` should stop flooding the terminal on a green build, so agents stop piping it through
`tail`/`head`. By default it captures MSBuild output: on success it prints one short summary
(repo root, elapsed time, "Build completed successfully!"); on failure it prints the full captured
MSBuild output to stderr and exits non-zero. `--verbose` / `-v` streams MSBuild live (the old
default). `--quiet` / `-q` stays accepted and means the default, so existing callers keep working.
`dev workflow` (CI) keeps calling the build with defaults: a failed CI build still prints every
compiler diagnostic, a green one stays short.

Acceptance:
- Default `./bin/dev build` prints no MSBuild lines on success and a one-line summary with duration.
- Failure prints the captured MSBuild output plus "Build failed!" and a non-zero exit code.
- `-v` / `--verbose` streams; `-q` / `--quiet` is accepted (same as default); `--verbose` wins when both are passed.
- Unit tests in `tests/tools/dev-cli-tests` cover the mode choice and the summary line.
- Gates: `./bin/dev build`, the new test file, `ganda repo audit`.

## Requirements

1. **Quiet-on-success by default in `BuildCommand` (`tools/dev-cli/endpoints/build-command.cs`):**
   - Capture MSBuild output by default: on success print a concise summary line (e.g. repo root, duration, and `"Build completed successfully!"`); on failure print the full captured compiler/MSBuild error output to stderr and exit non-zero.
   - Add `--verbose` (`-v`) to stream full MSBuild output live (the previous default behavior).
   - Keep `--quiet` (`-q`) accepted as a backward-compatible no-op (or alias for default) so existing callers, CI scripts, and `dev workflow` invocations passing `--quiet` continue to work without breaking.
2. **Check `workflow-command.cs` and `dev-cli` callers:**
   - Verify whether CI (`dev workflow`) wants `--verbose` or benefits from concise success + full error output on failure; keep CI diagnostics clear when a build fails.
3. **Tests & verification:**
   - Add or update unit tests in `tools/dev-cli` tests covering the default quiet behavior, `--verbose`, and backward-compatible `--quiet`.
   - Run `dotnet run tools/dev-cli/dev.cs -- self-install`, `./bin/dev build`, `./bin/dev test --group fast`, and `ganda repo audit`.

## Checklist

- [x] Make quiet-on-success (print full captured output on failure) the default in `tools/dev-cli/endpoints/build-command.cs`
- [x] Add `--verbose` (`-v`) to stream MSBuild output and keep `--quiet` (`-q`) accepted for compatibility
- [x] Verify `dev workflow` / CI build logging behavior
- [x] Add/update unit tests for `BuildCommand` options
- [x] Run `self-install`, `./bin/dev build`, targeted dev-cli tests, and `ganda repo audit` (full `--group fast` is CI's job under the walk rules)

## Session

- Created: 2902026 (2026-11-24)

## Results

### Changes

- `tools/dev-cli/services/build-output.cs` (new, pure): `BuildOutputMode { Capture, Stream }`,
  `BuildOutput.Select(verbose, quiet)` (default and `--quiet` capture; `--verbose` streams and wins
  over `--quiet`), and `BuildOutput.SuccessSummary(repoRoot, elapsed)` →
  `Build completed successfully! (33.3s, <root>)`.
- `tools/dev-cli/endpoints/build-command.cs`: adds `--verbose` / `-v`; `--quiet` / `-q` kept and
  documented as the default. Capture mode prints the step lines plus the one summary line on success;
  on failure it prints the full captured MSBuild output to stderr, then `Build failed!`, exit 1
  (that failure path is the old `--quiet` code, unchanged). Stream mode is the old default output.
- `dev workflow` (CI): `RunPrAsync` / `RunReleaseAsync` call `new BuildCommand()` with defaults, so CI
  now captures. A green CI build prints a short summary; a red one still prints every MSBuild
  diagnostic (captured output goes to stderr in full). No `--verbose` needed in CI; no change to
  `workflow-command.cs`.
- `tests/tools/dev-cli-tests/build-output-tests.cs` (new, 5 tests) and a Compile-include of
  `build-output.cs` in `dev-cli-tests.csproj`.

### How to validate

Smoke:
1. `dotnet run --file tools/dev-cli/dev.cs -- self-install`
2. `./bin/dev build` then `./bin/dev build -v` then `./bin/dev build -q`
3. `./bin/dev test --project dev-cli-tests --filter-class BuildOutput_`

Expect:
1. `./bin/dev build`: 4 lines (repo root, `Client-name scan passed.`, `Building …slnx...`,
   `Build completed successfully! (N.Ns, <root>)`), exit 0, no MSBuild lines.
2. `-v`: full MSBuild stream (~110 lines incl. `Time Elapsed`), then `Build completed successfully!`, exit 0.
   `-q`: same 4 lines as the default, exit 0. `build --help` lists `--verbose, -v` and `--quiet, -q`.
3. 5 of 5 tests pass.

### Gates

- `dotnet run --file tools/dev-cli/dev.cs -- self-install` — installed to `./bin`, exit 0.
- `./bin/dev build` — exit 0, 0 warnings / 0 errors, 4 output lines (33.3s). Also checked `-v` (112 lines, exit 0) and `-q` (4 lines, exit 0).
- `tests/tools/dev-cli-tests/build-output-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI` guard; project-based dev-cli-tests). Ran `./bin/dev test --project dev-cli-tests --filter-class BuildOutput_`: 5 passed, 0 failed.
- `ganda repo audit` — passed, exit 0, with 1 advisory warning (`vscode-window-icon`: `.vscode/settings.json` missing in this worktree). `ganda repo audit --fix` wrote that file (gitignored, local only) and the check now passes; nothing to commit for it.

## Notes

- Source: Task 514 (`report/analysis.md`, §6 `/tw-correct` Hand-off B).
- Pairs with `timewarp-ganda` Task 683 (`repo audit` short summary by default + saved full report).
