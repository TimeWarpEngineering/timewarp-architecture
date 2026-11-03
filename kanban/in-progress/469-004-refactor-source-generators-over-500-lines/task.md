# Refactor source generators over 500 lines

## Parent

469

## Description

Four generators exceed 500 lines. A generator usually mixes syntax discovery, model building,
and text emission in one class. Separate those into collector, model, and emitter types per
generator, mirroring the pattern that works best among the four.

## Files

- source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs (866)
- source/foundation/foundation-contracts-generators/contracts-generator.cs (672)
- source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs (576)
- source/analyzers/timewarp-architecture-analyzers/generators/typed-id-source-generator.cs (548)

## Requirements

- Each file 500 lines or fewer. Generated output text is byte-identical.
- Generator tests under tests/analyzers pass unchanged. Golden under goldens/ingress unchanged
  (`dev regenerate-generated` produces no diff).
- Incremental generator pipeline shape preserved (no new non-cacheable steps).

## Goal (implementer restatement)

Split each of the four oversized generators into a thin generator class (pipeline wiring in
`Initialize`), a collector type (syntax/symbol discovery and model building), model types
(the equatable pipeline records), and an emitter type (source text). Fast-endpoint already keeps
its model in `models/`; that model/generator split is the pattern the other three follow, with
collector and emitter pulled out as `internal static` classes beside the generator.

Acceptance:

- Every resulting file is 500 lines or fewer.
- Generated output is byte-identical: the solution's emitted generated files
  (`artifacts/generated/**`) match a pre-refactor snapshot.
- `tests/analyzers` generator tests pass with no test edits.
- `dev regenerate-generated` leaves `goldens/ingress` unchanged.
- Pipeline shape unchanged: same providers, same Select/Where/Collect/Combine steps, same
  equatable models (no symbol or Location newly held in a cached step).
- `dev build` 0 warnings; `ganda repo audit` clean.

## Checklist

- [x] page-source-generator split into collector / model / emitter
- [x] contracts-generator split
- [x] fast-endpoint-source-generator split
- [x] typed-id-source-generator split
- [x] Generator tests and golden diff green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
- 2026-11-04 implementer (claude-opus-5-5, ganda task work): split all four generators; verified
  byte-identical output against a pre-refactor snapshot of `artifacts/generated`.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session ee4fc404-c6ee-4073-9f5a-043795227d69, max-turns 500, Done — 2026-11-03T20:41:21Z

## Results

Each generator is now a thin `IIncrementalGenerator` that only wires the pipeline, plus
`internal` collector, model, and emitter types beside it. Fast-endpoint already kept its emit
model in `models/endpoint-metadata.cs`; the other three now follow that model/generator split.

| Generator | Files (lines) |
|-----------|---------------|
| Page | `page-source-generator.cs` (137), `page-collector.cs` (332), `page-model.cs` (127), `page-emitter.cs` (158), `page-nav-placement-reader.cs` (168) |
| Contracts | `contracts-generator.cs` (106), `contracts-collector.cs` (382), `contracts-model.cs` (44), `contracts-emitter.cs` (190) |
| FastEndpoint | `fast-endpoint-source-generator.cs` (251, keeps `ProcessBatch` diagnostics), `fast-endpoint-collector.cs` (114), `fast-endpoint-generation-options.cs` (61), `fast-endpoint-emitter.cs` (185) |
| TypedId | `typed-id-source-generator.cs` (70), `typed-id-collector.cs` (187), `typed-id-model.cs` (44), `typed-id-emitter.cs` (287) |

- Nested private models became top-level `internal` types. Names that would collide in the
  shared namespace got a generator prefix: `TypedIdCandidate`, `TypedIdEfModel`,
  `FastEndpointGenerationOptions`, `PageParsedRoute`, `ContractPart`, `ContractTarget`.
  Equality and hash code are unchanged.
- Contracts `BuildRoute` now parses in the collector and calls `ContractsEmitter.RouteBody` for
  the member text. The StringBuilder sequence is the same, so the output is the same.
- `Initialize` bodies are textually identical to the originals once the new
  `Collector.`/`Emitter.` qualifiers are removed. No pipeline step was added, removed, or reordered.
- Gates: `dotnet build timewarp-architecture.slnx` and `dev build` 0 warnings / 0 errors.
  Source generator tests 155/155 passed and analyzer tests 259/259 passed, with no test edits.
  `dev regenerate-generated` left `goldens/ingress` unchanged. `ganda repo audit` passes. Its
  one advisory (`kanban-landed`: nine other tasks' claims have no worktree) is board state, not
  something this task can fix.
- Byte-identical proof: all 279 files these generators emit across the solution
  (`artifacts/generated/*/{timewarp-architecture-analyzers,foundation-contracts-generators}/TimeWarp*`)
  match a snapshot taken before the refactor (`diff -r`). The only diffs were in the generator
  projects' own Regex-generator output, which names the containing class
  (`PageCollector` / `ContractsCollector`).

### How to validate

Smoke:

```bash
mkdir -p /tmp/t469-base-src && git archive master | tar -x -C /tmp/t469-base-src   # pre-refactor source
(cd /tmp/t469-base-src && dotnet build timewarp-architecture.slnx -v:q)
rm -rf artifacts/generated && dotnet build timewarp-architecture.slnx -v:q
for g in timewarp-architecture-analyzers foundation-contracts-generators; do
  (cd /tmp/t469-base-src/artifacts/generated && find . -path "*/$g/TimeWarp*" -type f) | while read -r f; do
    cmp -s "/tmp/t469-base-src/artifacts/generated/$f" "artifacts/generated/$f" || echo "DIFF $f"; done; done
(cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet run)
(cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet run)
./bin/dev regenerate-generated && git diff --exit-code -- goldens/ingress
wc -l source/analyzers/timewarp-architecture-analyzers/generators/*.cs source/foundation/foundation-contracts-generators/*.cs
```

Expect: the build has 0 warnings. The `cmp` loop prints no `DIFF` line (279 files compared). Both test runs report `failed: 0`. The golden diff exits 0. No file
in either generators folder is over 500 lines.
