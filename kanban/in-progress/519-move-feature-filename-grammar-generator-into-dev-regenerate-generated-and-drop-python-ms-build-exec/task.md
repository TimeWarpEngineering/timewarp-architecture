# Move feature-filename-grammar generator into dev regenerate-generated and drop Python MSBuild Exec

## Description

`source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj` currently invokes a Python script (`generate-feature-filename-grammar.py`) from an MSBuild `<Target Name="GenerateFeatureFilenameGrammar">` gated only on filesystem timestamps (`Inputs`/`Outputs`).

On `2026-11-24` (PR #673, run `45374744564`), `template-smoke` on the `wslc` self-hosted runner failed in `1m 07s` with:

```text
timewarp-architecture-convention-analyzers.csproj(55,5): error MSB3073:
The command "python3 .../generate-feature-filename-grammar.py ..." exited with code 127.
```

Because `python3` is not installed in the `wslc` container image, any fresh git checkout where `feature-filename-grammar.json` happens to receive a slightly newer mtime than one of the four committed generated files breaks the build. Even with a C# script, mutating tracked source files during `dotnet build` based on checkout timestamps is brittle.

All four outputs (`feature-filename-grammar.g.cs` and the three `feature-filename-grammar.g.props` files) are already committed to git and guarded by `Should_Keep_Grammar_Registry_In_Sync`. Move the generator into C# inside `tools/dev-cli/` (`dev regenerate-generated`), delete `generate-feature-filename-grammar.py`, and remove the MSBuild `<Exec>` target.

## Requirements

1. **Port the grammar generator to C# in `tools/dev-cli/`:**
   - Implement a pure-C# generator service (e.g. `tools/dev-cli/services/generated-feature-filename-grammar.cs`) that reads `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json` and emits the exact same four files with byte-for-byte equivalent semantics (including the function→layer validation and layer-suffix nesting rejection):
     - `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.g.cs` (`Families = web, api, grpc`)
     - `source/container-apps/web/msbuild/feature-filename-grammar.g.props`
     - `source/container-apps/api/msbuild/feature-filename-grammar.g.props`
     - `source/container-apps/grpc/msbuild/feature-filename-grammar.g.props`
   - Update the auto-generated file header comments to say `Edit feature-filename-grammar.json and run dev regenerate-generated` instead of referencing the Python script / MSBuild target.
2. **Wire into `dev regenerate-generated` and CI drift check:**
   - Invoke the grammar generator from `tools/dev-cli/endpoints/regenerate-generated-command.cs` (alongside `goldens/ingress/WebServerApiRoutePrefixes.g.cs`), skipping cleanly when `feature-filename-grammar.json` is absent (e.g., in generated template apps that do not include `source/analyzers/`).
   - Extend `tools/dev-cli/endpoints/workflow-command.cs`'s post-`regenerate-generated` clean-diff check so any drift in `feature-filename-grammar.g.cs` or the three `feature-filename-grammar.g.props` files fails CI with a message naming `dev regenerate-generated`.
   - Keep `Should_Keep_Grammar_Registry_In_Sync` (`tests/analyzers/timewarp-architecture-analyzers-tests/feature-filename-grammar-analyzer-tests.cs`) passing.
3. **Delete Python script and MSBuild `<Exec>` target:**
   - Remove `<Target Name="GenerateFeatureFilenameGrammar">`, the `_FeatureFilenameGrammar*` generator properties, and `<None Include="generate-feature-filename-grammar.py" />` from `source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj`.
   - Keep `<None Include="feature-filename-grammar.json" />` and `<Compile Update="feature-filename-grammar.g.cs" AutoGen="true" DesignTime="true" />`.
   - Delete `source/analyzers/timewarp-architecture-convention-analyzers/generate-feature-filename-grammar.py`.
   - Grep the repo for any remaining references to `generate-feature-filename-grammar.py` (docs, skills, comments, tests) and update them to name `dev regenerate-generated`.
   - Update `AGENTS.md` under "Generated ingress prefixes" (or rename section to "Generated files") to note `dev regenerate-generated` covers both `goldens/ingress` and the feature filename grammar files.
4. **Tests & verification:**
   - Add unit tests in `tools/dev-cli` tests covering: emitting `.g.cs` + `.g.props` from sample JSON (routed + unrouted layers, longest-first sorting), rejecting an unregistered layer reference, and rejecting nested layer suffixes.
   - Run `dotnet run tools/dev-cli/dev.cs -- self-install`, `./bin/dev regenerate-generated`, `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, `./bin/dev test --project timewarp-architecture-analyzers-tests`, `./bin/dev template-smoke`, and `ganda repo audit`.

## Goal (implementer restatement)

The build must stop running Python. Today the convention-analyzers project shells out to
`python3 generate-feature-filename-grammar.py` from an MSBuild target whenever the grammar JSON
looks newer than the four committed outputs, and a runner without Python fails the build.

Done means:

- A C# generator in `tools/dev-cli/services/` reads `feature-filename-grammar.json` and writes the
  same four files the Python script wrote (one `.g.cs`, three family `.g.props`), with the same
  checks: a function that names an unregistered layer is refused, and a layer suffix that nests
  inside another is refused. Only the header comments change, and they now say to edit the JSON
  and run `dev regenerate-generated`.
- `dev regenerate-generated` runs that generator (and skips it when the JSON is not there, as in a
  generated app), then does the ingress golden as before.
- `dev workflow` fails CI with a message naming `dev regenerate-generated` when any of the four
  grammar outputs drifts after regeneration, just as it does for `goldens/ingress`.
- The csproj has no generator target, no `_FeatureFilenameGrammar*` properties, and no `.py`
  item; the `.py` file is gone; no live doc, skill, or comment still names it.
- dev-cli-tests cover emitting the files from sample JSON (routed and unrouted layers,
  longest-first order), the unregistered-layer refusal, and the nested-suffix refusal; the
  analyzer drift test `Should_Keep_Grammar_Registry_In_Sync` still passes.

## Checklist

- [x] Implement C# feature-filename-grammar generator service in `tools/dev-cli/services/`
- [x] Wire grammar regeneration into `dev regenerate-generated` (safe when `source/analyzers/` is absent in generated apps) and extend `dev workflow` clean-diff check
- [x] Remove MSBuild `GenerateFeatureFilenameGrammar` `<Exec>` target and delete `generate-feature-filename-grammar.py`
- [x] Update header comments, `AGENTS.md`, and any stale references to `generate-feature-filename-grammar.py`
- [x] Add `dev-cli-tests` unit tests for generation + validation rules
- [x] Run `self-install`, `./bin/dev regenerate-generated`, `./bin/dev build`, analyzer + dev-cli tests, `./bin/dev template-smoke`, and `ganda repo audit`

## Session

- Created: 2026-11-24 (from PR #673 `template-smoke` `python3` exit `127` failure on `wslc`)
- 2026-11-24 implementer: ported the generator to `tools/dev-cli/services/feature-filename-grammar-generator.cs`,
  wired it into `dev regenerate-generated` and the `dev workflow` drift check, removed the MSBuild
  target and the `.py`, updated comments, skills, and `AGENTS.md` ("Generated files" section).
  No other `.csproj`/`.props`/`.targets` in the repo runs `python`.
- Boy-scout audit fixes from `ganda repo audit --fix`: `kanban-landed` landed task 515's drifted
  kitchen onto origin-home (another live walk's committed board edits); `vscode-window-icon` wrote
  the ignored local `.vscode/settings.json`.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session ad1561c4-583c-4372-bdb6-452459f2f603, max-turns 500, Done — 2026-11-24T11:55:15Z

## Results

Regenerated outputs differ from the Python-era files only in the header comments (and the
`Families` doc comment that named the removed `<Exec>`). Everything else is byte-identical, LF, no BOM.
The ingress golden did not change.

### How to validate

- Smoke: `dotnet run tools/dev-cli/dev.cs -- self-install`, then `./bin/dev regenerate-generated`,
  then `git status --short`.
- Expect: the command prints `Regenerating feature filename grammar...` and four `Wrote …` lines
  (the `.g.cs` and the web/api/grpc `.g.props`) before the ingress golden step, and `git status`
  shows no change to those four files or `goldens/ingress`. With
  `feature-filename-grammar.json` moved away, it prints `Skipping feature filename grammar: … not
  found.` and still writes the ingress golden (checked by hand, exit 0).

### Gates

- `dotnet run tools/dev-cli/dev.cs -- self-install` — succeeded.
- `./bin/dev regenerate-generated` — succeeded; header-only diff on the four grammar outputs, no ingress diff.
- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `tests/tools/dev-cli-tests/feature-filename-grammar-generator-tests.cs` (new; not a standalone
  runfile, no `JARIBU_MULTI` guard) — `./bin/dev test --project dev-cli-tests --filter-class FeatureFilenameGrammarGenerator_` — 9/9 passed.
- `tests/analyzers/timewarp-architecture-analyzers-tests/feature-filename-grammar-analyzer-tests.cs`
  (edited comments; not a standalone runfile) — `./bin/dev test --project timewarp-architecture-analyzers-tests --filter-class Should_Keep_Grammar_Registry_In_Sync` — 1/1 passed.
- Coverage for the edited regenerate command's ingress helper: `./bin/dev test --project dev-cli-tests --filter-class GeneratedIngressPrefixes_` — 4/4 passed.
- `./bin/dev template-smoke` — Template smoke SUCCEEDED.
- `ganda repo audit` — first run failed (`kanban-landed` on task 515, `vscode-window-icon` warning);
  `ganda repo audit --fix` fixed both; rerun: Repository passes all audit checks.

## Notes

- Overlaps `tools/dev-cli/` with Task 517; do not run `517` and `519` concurrently.
- Also verify no other `.csproj` in the repo invokes `python` or `python3` in an MSBuild `<Exec>` target.
