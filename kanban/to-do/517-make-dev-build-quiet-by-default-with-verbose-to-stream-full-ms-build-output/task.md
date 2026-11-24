# Make dev build quiet by default with --verbose to stream full MSBuild output

## Description

Walk performance analysis (Task 514, `/tw-correct` Hand-off B) found repeated cases (`499`, `483`, `503`) of agents piping `./bin/dev build` into `| tail -5` / `| head` to avoid verbose MSBuild output — truncating compiler diagnostics, masking exit codes when `pipefail` is off, and forcing a second full build (`~46.8s` wasted on Task 503 alone).

`tools/dev-cli/endpoints/build-command.cs` already implements quiet capture (`--quiet` / `-q`: capture MSBuild output and print it only on failure), but defaults to streaming full MSBuild output. Making quiet-on-success the default removes the incentive for agents to pipe `./bin/dev build` through `tail`/`head` (parallel to `timewarp-ganda` Task 683 for `ganda repo audit`).

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

- [ ] Make quiet-on-success (print full captured output on failure) the default in `tools/dev-cli/endpoints/build-command.cs`
- [ ] Add `--verbose` (`-v`) to stream MSBuild output and keep `--quiet` (`-q`) accepted for compatibility
- [ ] Verify `dev workflow` / CI build logging behavior
- [ ] Add/update unit tests for `BuildCommand` options
- [ ] Run `self-install`, `./bin/dev build`, `./bin/dev test --group fast`, and `ganda repo audit`

## Session

- Created: 2902026 (2026-11-24)

## Notes

- Source: Task 514 (`report/analysis.md`, §6 `/tw-correct` Hand-off B).
- Pairs with `timewarp-ganda` Task 683 (`repo audit` short summary by default + saved full report).
