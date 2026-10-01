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

- [ ] `--launch-profile` / `-lp` option on `dev run`, forwarded to `aspire run`
- [ ] Profile name validated against `launchSettings.json` (fail fast, list valid names)
- [ ] CLI < 13.6 + option → clear error with the update command; no option → unchanged
- [ ] Environment and profile precedence recorded in the Design region
- [ ] Purpose/Design regions reconciled in `run-command.cs`
- [ ] Tests for argument building (with and without the option), unknown-profile rejection and
      the version guard, without launching Aspire. Extract the argument and validation logic so
      it is testable; use a Jaribu runfile or the existing dev-cli test location if there is one
- [ ] Skill / capabilities text updated if it lists `dev run` options
- [ ] Gates: `dev build` 0/0, the dev-cli tests, `ganda repo audit`. Gate on a fresh `bin/dev`
- [ ] Do **not** start an AppHost (`dev run`, `aspire run`); it shares the maintainer's user
      secrets. Verify argument construction through tests, not by launching
- [ ] Implementation review; host `open-pr`

## Session

- Created: 83286 (2026-10-01)

## Notes

- Ideally merge after task 262 (#417, Aspire 13.6 packages) so the repo and the CLI are both on
  13.6. The code does not depend on 262.
- Memory discipline: run builds serially and call `dotnet build-server shutdown` before
  finishing.

## Results

*(fill when done)*

### How to validate

*(required before done)*

Maintainer, after merge:
1. `dev run -lp http` starts the AppHost on the http profile.
2. `dev run -lp nope` fails fast and lists `https` and `http`.
3. `dev run` with no option behaves as before.
