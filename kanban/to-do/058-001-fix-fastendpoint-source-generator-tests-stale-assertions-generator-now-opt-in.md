# Fix FastEndpoint source generator tests (stale assertions; generator now opt-in)

Parent: 058. Surfaced while migrating the test projects to root (slice 1).

## Problem

`tests/analyzers/timewarp-architecture-sourcegenerator-tests` **builds** but **4 of 5 tests fail**.
These are **pre-existing failures, not migration-caused** (verified: reverting the migration edits
left them red). The FastEndpoint source generator (`source/analyzers/timewarp-architecture-analyzers/
generators/fast-endpoint-source-generator.cs`) now **defaults generation OFF** — the test code itself
notes: *"Pass the new MSBuild property to enable generation (default is now false)."* The generator
emits **0 sources** for the test inputs, so assertions like `GeneratedSources.Length should be 1 but
was 0` and `hasRouteConflict should be True but was False` fail.

Failing tests (in `fast-endpoint-source-generator-tests.cs` / `*-more-tests.cs`):
- `generatedSyntaxTrees.Length` expected 1, was 0
- `runResult.Results[0].GeneratedSources.Length` expected 1, was 0 (x2)
- `hasRouteConflict` expected True, was False

## To do

- [ ] Determine the current generator contract: what MSBuild property (e.g. `build_property.*`)
      enables generation, and confirm the tests pass it correctly via `TestAnalyzerConfigOptionsProvider`.
- [ ] Reconcile the test inputs' `using`s with where the trigger attributes now live
      (`TimeWarp.Architecture.Attributes`? `...Analyzers`?) so the sample endpoints are recognized.
- [ ] Update the stale assertions / inputs so the 5 tests pass (or delete/replace ones that no
      longer reflect the generator's behavior).
- [ ] Confirm green via `dotnet test tests/analyzers/timewarp-architecture-sourcegenerator-tests`.

## Notes

The project IS wired into `timewarp-architecture.slnx`, so `dev test` (and CI) currently runs these
4 reds. Accepted intentionally (Steven) to surface the debt rather than hide it; this task clears it.
