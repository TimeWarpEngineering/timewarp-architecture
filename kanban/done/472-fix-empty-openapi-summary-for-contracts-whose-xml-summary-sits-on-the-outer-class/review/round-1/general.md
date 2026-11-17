# Round 1 — general
**Date:** 2026-11-05
**Scope reviewed:** branch vs merge-base 40b6e4350: `endpoint-documentation-resolver.cs` (new),
`endpoint-metadata.cs`, `contract-documentation.cs`, `fast-endpoint-emitter.cs`,
`fast-endpoint-source-generator.md`, `fast-endpoint-source-generator-summary-tests.cs`, version
bump (`source/Directory.Build.props`, `timewarp-templates/Directory.Build.props`,
`Directory.Packages.props`).

## Summary

The change moves summary and remarks lookup into `EndpointDocumentationResolver`. The order
matches the task: request XML `<summary>`, then `[Operation(Description)]`, then the outer
`[ApiEndpoint]` class XML `<summary>`, then empty. Each XML source reads the symbol comment
first and the `.xml` AdditionalFile second. The order is written in the Design regions of both
the resolver and the emitter. `[Operation]` is read from the request class, which is where the
contracts put it (`set-role-permissions-contracts.cs:29`). The `[EndpointAllowAnonymous]`
reason still goes through `AnonymousReason` and is unchanged. Risk is low.

## Verification

- Generator tests: `dotnet run --project tests/analyzers/timewarp-architecture-sourcegenerator-tests`
  gives 163 total, 0 failed. That includes the 8 `FastEndpointSourceGenerator_Summary_Tests` cases:
  each source alone, request over Operation and outer, Operation over outer, no source, the
  AdditionalFile path for the outer class, and a wrapped summary.
- `artifacts/generated/web-server/.../SetRolePermissionsEndpoint.g.cs:26` emits
  `s.Summary = "Replace a role's permission ids.";`.
- Scan: for every generated web-server and api-server endpoint with no `s.Summary`, I looked at
  its contracts file for an `[Operation] Description =`, an outer-class `<summary>`, or a
  request `<summary>`. None had one. This agrees with the implementer's 0-bad claim.
- Goal restatement compared with the task: they match. The restatement adds the order of the
  symbol comment and the AdditionalFile. That only says how the existing csc path works. It is
  not a misread.
- Version bump: the platform `<Version>` and its pins move together to 2.0.0-beta.32, which
  follows the task-124 policy. The Analyzers package changed, so a bump is needed.
- Not re-run by this reviewer: `dev build`, `dev template-smoke`, `ganda repo audit`. The
  implementer reports them green in Results.

## Issues

None.

## Observations (not findings)

- `XElement.Value` turns `<see cref="X"/>` into empty text, so the referenced name is lost. The
  AdditionalFile path (`ContractDocumentation`) already worked this way, and the symbol path now
  matches it. Today no endpoint request class or outer class summary uses `<see>` or `<c>`. The
  hits in `source/container-apps` are all on members or non-endpoint types. If an endpoint
  summary ever uses that markup, the fix belongs in `ContractDocumentation.Normalize` or in a
  text extractor that knows about cref. This change is not the place for it.
