# Round 1 — general
**Date:** 2026-10-18
**Scope reviewed:** `master..d1b2d60e9` (whole branch diff), plus call sites in `tools/dev-cli` release/pack/push and `dev check-version`.

## Summary

The change adds `TimeWarp.Contracts` (`source/foundation/timewarp-contracts`, `1.0.0-beta.1`).
It holds `[Operation]`, `OperationKind`, the catalog DTOs (descriptor, member, bindings),
`IResponseRenderer<T>`, and `SharedProblemDetails`. That type moved from
`TimeWarp.Foundation.Contracts`, which now type-forwards it. The contracts generator emits
`OperationName` / `OperationDescription` on the HTTP partial. `EndpointEmitModel` carries
both values, and FastEndpoint emission is unchanged. Scope matches the task (step 2 only)
and the implementer restatement, so there is no misread. The package API, readme
derivation rules, and type forward are sound. The main risk is release mechanics: the
package's separate version line has no gate.

### Gates re-run by this reviewer (clean worktree, HEAD d1b2d60e9)

- `dotnet run tools/dev-cli/dev.cs -- build`: 0 warnings, 0 errors.
- `timewarp-architecture-sourcegenerator-tests`: 129/129 passed. `foundation-contracts-tests`: 25/25 passed.
- `ganda repo audit`: passes all checks.
- `dev check-version`: exit 0. It lists `TimeWarp.Contracts` among packages checked at the repo version `2.0.0-beta.20`, not at `1.0.0-beta.1` (see Issue 1).
- Verified with ilspycmd: `TimeWarp.Mediator.Contracts` 14.0.0-beta.5 declares `IIdempotentCommand : ICommand` and `IIdempotentCommand<T> : ICommand<T>`. The readme's "test idempotent first" rule is correct.
- Not run: `dev template-smoke`. It installs the template into the user's `dotnet new` state. The default variant is the PR CI gate and will exercise the new pack entry.

## Issues

### Issue 1 — Severity: bug
- File: source/foundation/timewarp-contracts/timewarp-contracts.csproj:12 (with tools/dev-cli/endpoints/workflow-command.cs:58 and :256-260)
- Description: `TimeWarp.Contracts` has its own hand-maintained `<Version>1.0.0-beta.1`, but
  nothing checks it. Release (`RunReleaseAsync` → `PackAsync` → `PushAsync`) repacks this
  project on every release and pushes with `--skip-duplicate`. `dev check-version` checks
  `TimeWarp.Contracts` at the repo version `2.0.0-beta.20`, a version that package never
  ships at. So the PR gate reports it as checked when its real version was never compared.
  Failure scenario: 384-001 or any later change edits a type in `timewarp-contracts` and does
  not bump `<Version>`. `dev check-version` still passes, and release packs a different
  `1.0.0-beta.1`. Push silently skips it as a duplicate, while
  `TimeWarp.Foundation.Contracts 2.0.0-beta.N` publishes depending on
  `TimeWarp.Contracts >= 1.0.0-beta.1`. Consumers then resolve the old nupkg without the new
  API: compile errors, or `TypeLoadException`/`MissingMethodException` at runtime. This
  breaks the AGENTS.md hard rule "When two things must agree, generate one from the other
  or add a build check". Here the source of `TimeWarp.Contracts` and its published version
  must agree.
- Suggestion: add a check in this repo's dev-cli that fails when `TimeWarp.Contracts`' own
  version is already on nuget.org and `source/foundation/timewarp-contracts/` changed since
  the release that published it. Run it in the release pipeline before push, and wire it
  into the PR path or the `tw-pr` gate list. A loud failure is acceptable, a silent skip is
  not. Keep the `1.0.0-beta` line; the task requires it. Also stop reporting
  `TimeWarp.Contracts` as checked at the repo version, or report its own version next to it,
  so the gate output is true. Document the bump rule in the csproj comment or the
  `workflow-command.cs` Design region.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/foundation/foundation-contracts-generators/contracts-generator.cs:282 and :308; source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs:278 and :336
- Description: Two generators read the same `[Operation]` attribute with separately written
  code, and they already disagree. The contracts generator drops a name only when it is
  null or empty, and emits `OperationDescription` for any non-empty string.
  `EndpointEmitModel.ReadOperation` drops whitespace-only names and descriptions
  (`IsNullOrWhiteSpace`, also inside `GetNamedStringArgument`). Example:
  `[Operation(" ", Description = "  ")]` yields `OperationName = " "` and
  `OperationDescription = "  "` on the partial, but null/null in the endpoint model. The
  attribute constructor throws on that name, but only at runtime reflection, never at
  compile time. The FQN `"TimeWarp.Contracts.OperationAttribute"` is also written by hand
  in both generators. Every test declares its own stub `OperationAttribute` instead of
  using the real type, so no test fails if the package type is renamed or moved.
- Suggestion: use one blank-value rule in both readers (`IsNullOrWhiteSpace`, which matches
  the constructor guard). Add one test that ties the generator constant to the real type,
  for example a `foundation-contracts-tests` (or package-referencing) test asserting
  `typeof(TimeWarp.Contracts.OperationAttribute).FullName == "TimeWarp.Contracts.OperationAttribute"`.
  Better: a contracts-generator test that compiles against the real `timewarp-contracts`
  assembly reference instead of the stub.
- Status: open
