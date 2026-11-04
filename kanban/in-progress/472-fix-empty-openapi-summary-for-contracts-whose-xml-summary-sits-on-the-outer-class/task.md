# Fix empty OpenAPI summary for contracts whose XML summary sits on the outer class

## Description

Found during task 461. The FastEndpoint generator (`source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-emitter.cs`,
collector `fast-endpoint-collector.cs`) emits an empty `<summary>` and `<remarks>` for
`SetRolePermissions`. It reads the XML `<summary>` on the inner request class, but the Roles
contracts put the summary on the outer static class (`/// <summary>Replace stored permission ids
for a role.</summary>` on `SetRolePermissions`), and it ignores `[Operation(Description = ...)]`.
The generated endpoint therefore has no OpenAPI description.

## Requirements

- Resolution order for the endpoint summary: XML `<summary>` on the request class; else
  `[Operation(Description = ...)]`; else XML `<summary>` on the containing static class; else empty.
  Record the order in the emitter's Design region.
- Cover every contract in `source/container-apps/web/features` and the template-smoke output: no
  generated endpoint with an empty summary where any of the three sources exists.
- Generator test in `tests/analyzers/` for each source and for the precedence.
- Gates: `dev build` 0 warnings, generator tests, `dev template-smoke` (generated apps compile the
  same generator), `ganda repo audit`.

## Goal (implementer restatement)

The FastEndpoint generator only reads the XML `<summary>` on the nested `Query`/`Command`.
Contracts that document the outer `[ApiEndpoint]` class, or that carry
`[Operation(Description = ...)]`, get an endpoint with no OpenAPI summary. Fix the summary
lookup so it tries, in order: the request class XML summary, the `[Operation]` description,
then the outer class XML summary. If none exists, the summary stays empty. Each XML source uses
the symbol's own documentation comment first and the contract assembly's `.xml` AdditionalFile
second (the command-line build path).

Acceptance:

- The order is in code and written in a Design region.
- Generator tests cover each source alone and the precedence between them.
- No generated endpoint in this repo, or in the template-smoke output, has an empty summary
  when one of the three sources exists.
- `dev build` has 0 warnings, generator tests pass, `dev template-smoke` passes, and
  `ganda repo audit` passes.

## Checklist

- [x] Precedence implemented and recorded in the Design region
- [x] Tests for each source and for precedence
- [x] No empty summaries in the repo's generated endpoints
- [x] Gates green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)
- Implement (2026-11-05): new `EndpointDocumentationResolver` owns the summary order;
  `EndpointEmitModel.FromSymbol` calls it. Order recorded in the emitter and resolver Design
  regions. Audit: pass, 2 advisory warnings already present (other worktrees' claims; line cap on
  migrations and a done-task runfile). Nothing to fix on this id.

## Results

- `source/analyzers/timewarp-architecture-analyzers/models/endpoint-documentation-resolver.cs` (new):
  summary = request XML `<summary>`, else `[Operation(Description)]`, else outer static class XML
  `<summary>`, else empty. Remarks = request `<remarks>`, else outer `<remarks>`. Each XML source
  reads the symbol comment first, then the contract `.xml` AdditionalFile (csc path).
- `endpoint-metadata.cs`: `FromSymbol` uses the resolver. The raw-substring `ExtractXmlContent`
  is gone. The symbol-comment path now parses XML and joins wrapped lines into one line, as the
  file path already did. A multi-line request summary in an IDE build would otherwise put a
  line break inside the `s.Summary` string literal.
- `contract-documentation.cs`: `Normalize` is internal and trims lines before dropping empty
  ones. Before this, the indentation-only line before `</summary>` left a trailing space.
- `fast-endpoint-emitter.cs`: new Design region states the order. The `///` doc text is
  XML-escaped, because inner text can now contain `&` or `<`.
- Tests: `tests/analyzers/timewarp-architecture-sourcegenerator-tests/fast-endpoint-source-generator-summary-tests.cs`
  has 8 cases: each source alone, request over Operation and outer, Operation over outer (the
  Roles shape), no source, outer summary and remarks from the AdditionalFile, and a wrapped
  summary.
- Repo scan: 73 generated endpoints in this repo (web-server + api-server). 49 have no
  summary, and none of the 3 sources exists for any of them (checked against
  `web-contracts.xml` / `api-contracts.xml` and `[Operation(Description)]` in source). The
  template-smoke `SmokeDefault` output gives the same count: 0 bad. `SetRolePermissionsEndpoint`
  now emits `s.Summary = "Replace a role's permission ids.";`.
- Version: platform `<Version>` and its pins go from 2.0.0-beta.31 to 2.0.0-beta.32 in the same
  commit, because the Analyzers package changed (beta.31 is already on NuGet). The
  `TimeWarp.Contracts` pin is its own line and stays as is. `dev check-version` passes.
- Gates: generator tests 163/163; `dev build` 0 warnings 0 errors; `dev template-smoke`
  succeeded (run again after the version bump); `ganda repo audit` passes (2 advisory warnings already present, not from this change).

### How to validate

Smoke:

```bash
dotnet run --project tests/analyzers/timewarp-architecture-sourcegenerator-tests
./bin/dev build -q
grep -n "s.Summary" artifacts/generated/web-server/timewarp-architecture-analyzers/TimeWarp.Architecture.Analyzers.FastEndpointSourceGenerator/SetRolePermissionsEndpoint.g.cs
```

Expect: the test run reports `failed: 0`, including the `FastEndpointSourceGenerator_Summary_Tests`
cases. The build prints `Build completed successfully!`. The grep prints
`s.Summary = "Replace a role's permission ids.";`.

## Notes

Follows 461 (objective bug in `rfc/rfc.md`). Independent of 473–476.
