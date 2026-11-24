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

## Checklist

- [ ] Implement C# feature-filename-grammar generator service in `tools/dev-cli/services/`
- [ ] Wire grammar regeneration into `dev regenerate-generated` (safe when `source/analyzers/` is absent in generated apps) and extend `dev workflow` clean-diff check
- [ ] Remove MSBuild `GenerateFeatureFilenameGrammar` `<Exec>` target and delete `generate-feature-filename-grammar.py`
- [ ] Update header comments, `AGENTS.md`, and any stale references to `generate-feature-filename-grammar.py`
- [ ] Add `dev-cli-tests` unit tests for generation + validation rules
- [ ] Run `self-install`, `./bin/dev regenerate-generated`, `./bin/dev build`, analyzer + dev-cli tests, `./bin/dev template-smoke`, and `ganda repo audit`

## Session

- Created: 2026-11-24 (from PR #673 `template-smoke` `python3` exit `127` failure on `wslc`)

## Notes

- Overlaps `tools/dev-cli/` with Task 517; do not run `517` and `519` concurrently.
- Also verify no other `.csproj` in the repo invokes `python` or `python3` in an MSBuild `<Exec>` target.
