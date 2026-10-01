# dev run: pass --launch-profile through to aspire run (Aspire CLI 13.6)

## Description

Aspire CLI 13.6 accepts `--launch-profile` (`-lp`) on `aspire run` / `aspire start`. Today
`dev run` (`tools/dev-cli/endpoints/run-command.cs`) always runs
`aspire run --apphost <aspire-app-host.csproj>`, so the AppHost uses its first launch profile.
In `aspire-app-host/Properties/launchSettings.json` that is `https`; the other profile is `http`.
There's no way to choose a profile through `dev`.

Add an optional `--launch-profile <name>` (short `-lp`) to `dev run` and forward it to
`aspire run`. Leave it out and today's behavior is unchanged.

Context: task 262 evaluated this as "later", because the installed 13.5.4 CLI rejected the flag.
The maintainer has now installed CLI 13.6.0 (2026-10-01) and wants it.

## Requirements

1. `dev run --launch-profile <name>` / `-lp <name>` appends `--launch-profile <name>` to the
   `aspire run` arguments. Without the option, the arguments are byte-identical to today's.
2. Validate the name against `launchSettings.json` profiles **before** launching. An unknown
   profile fails fast with the list of valid names (`https`, `http`), not an opaque Aspire error.
   Read the file. Never hard-code the list.
3. **CLI version guard:** if the installed `aspire` is older than 13.6 and `--launch-profile` was
   given, fail with a clear message (`aspire update --self` or
   `dotnet tool update -g Aspire.Cli`). Do not silently drop the option. Without the option, any
   CLI version still works.
4. Keep `dev run`'s existing environment handling (it forces `ASPNETCORE_ENVIRONMENT=Development`).
   Record in the Design region how that interacts with a profile's own `environmentVariables`:
   which one wins, verified against the CLI's behavior rather than guessed.
5. Follow Nuru DevCli conventions for the option (`[Option("launch-profile", "lp", Description = …)]`),
   and update `dev --capabilities` output and the `tw-dev-cli` / repo skill text if they list
   `dev run` options.
6. **Stale-binary footgun:** `dev` is an AOT `bin/dev` snapshot. After editing dev-cli, rebuild
   or self-install it (`dev self-install`, or `ganda repo audit --fix --checks bin-dev`) and gate
   on the fresh binary, or on `./tools/dev-cli/dev.cs` run directly.

## Checklist

- [x] `--launch-profile` / `-lp` option on `dev run`, forwarded to `aspire run`
- [x] Profile name validated against `launchSettings.json` (fail fast, list valid names)
- [x] CLI < 13.6 + option → clear error with the update command; no option → unchanged
- [x] Environment and profile precedence recorded in the Design region
- [x] Purpose/Design regions reconciled in `run-command.cs`
- [x] Tests for argument building (with and without the option), unknown-profile rejection and
      the version guard, without launching Aspire. Extract the argument and validation logic so
      it is testable; use a Jaribu runfile or the existing dev-cli test location if there is one
- [x] Skill / capabilities text updated if it lists `dev run` options
- [x] Gates: `dev build` 0/0, the dev-cli tests, `ganda repo audit`. Gate on a fresh `bin/dev`
- [x] Do **not** start an AppHost (`dev run`, `aspire run`); it shares the maintainer's user
      secrets. Verify argument construction through tests, not by launching
- [ ] Implementation review; host `open-pr`

## Session

- Created: 83286 (2026-10-01)
- 2026-10-01 implementer (claude-opus-5-5, headless task-work): option, validation, version guard,
  tests, AGENTS.md line; gates green on fresh `bin/dev`. Implementation review + open-pr pending (host).

## Notes

- Ideally merge after task 262 (#417, Aspire 13.6 packages) so the repo and the CLI are both on
  13.6. The code does not depend on 262.
- Memory discipline: run builds serially and call `dotnet build-server shutdown` before
  finishing.

## Results

- `dev run --launch-profile <name>` / `-lp <name>` (`tools/dev-cli/endpoints/run-command.cs`) forwards
  `--launch-profile <name>` to `aspire run`. Without it the arguments are exactly
  `run --apphost <csproj>` (unchanged) and no version probe runs.
- New pure helper `tools/dev-cli/services/aspire-run.cs` (`AspireRun`): `BuildRunArguments`,
  `ReadLaunchProfileNames` (reads the AppHost `Properties/launchSettings.json` via `JsonDocument`,
  AOT-safe, no hard-coded list), `ValidateLaunchProfile` (ordinal match; error lists valid names),
  `ParseCliVersion` / `ValidateCliVersionForLaunchProfile` (< 13.6 or unparseable → error naming
  `aspire update --self` / `dotnet tool update -g Aspire.Cli`).
- **Env precedence (verified, not guessed):** with Aspire CLI 13.6.0 against a throwaway `/tmp`
  AppHost (own UserSecretsId, not this repo's), parent `ASPNETCORE_ENVIRONMENT=Development` +
  `-lp staging` (profile sets `Staging`) → AppHost saw `Staging`; with no `-lp` the first profile's
  value also overrode the parent. So the profile's `environmentVariables` win; `dev run`'s forced
  `Development` only applies when the profile doesn't set it. Recorded in the run-command Design region.
- Tests: `tests/tools/dev-cli-tests/aspire-run-tests.cs` (12 tests: args with/without option,
  profile parsing incl. the real repo launchSettings → `https, http`, unknown/case-mismatched profile
  rejection, version guard). Suite 80/80.
- End-to-end with a fake `aspire` shim on PATH (no AppHost launched): 13.5.4 + `-lp http` → guard
  error, exit 1; 13.6.0 + `-lp http` → `run --apphost … --launch-profile http`; no option →
  `run --apphost …`. Fresh AOT `bin/dev run -lp nope` → lists `https, http`, exit 1.
- `dev --capabilities` shows the option (generated from the Nuru route). AGENTS.md `dev run` line
  updated; no repo skill lists `dev run` options.
- Gates: `dev build` (fresh `bin/dev` via self-install) succeeded (warnings are errors), dev-cli-tests 80/80,
  `ganda repo audit` passes.

### How to validate

Smoke (no AppHost launch):
1. `dev run -lp nope` → `Unknown launch profile 'nope'. Valid profiles in …launchSettings.json: https, http`, exit 1.
2. `cd tests/tools/dev-cli-tests && dotnet test -c Release -- --filter-class AspireRun` → 12/12 pass.
3. `dev --capabilities` → `run` lists option `launch-profile` alias `lp`.

Expect: all three as stated; `dev run` with no option still runs `aspire run --apphost <csproj>`.

Maintainer, after merge:
1. `dev run -lp http` starts the AppHost on the http profile.
2. `dev run -lp nope` fails fast and lists `https` and `http`.
3. `dev run` with no option behaves as before.
