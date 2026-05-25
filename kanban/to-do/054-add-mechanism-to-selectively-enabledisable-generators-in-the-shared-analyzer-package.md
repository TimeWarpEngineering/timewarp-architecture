# Add mechanism to selectively enable/disable generators in the shared analyzer package

## Parent

053-replace-morrismoxy-with-standard-c-source-generators (related)

## Summary

The `timewarp-architecture-analyzers` package contains multiple source generators (FastEndpointSourceGenerator, and others). When a project references this analyzer (often conditionally under feature flags like `#if(api)`), the generators currently run unconditionally if they detect their trigger attributes in any referenced assembly. This causes incorrect behavior, such as attempting to generate server-side FastEndpoints endpoint code when building `web-spa`.

We need a first-class mechanism for consuming projects (or the feature flag system) to explicitly control which generators are active.

## Background / Problem

- All generators live in a single analyzer package for convenience.
- Projects opt into the analyzer via conditional `<ProjectReference OutputItemType="Analyzer">` blocks tied to feature flags (`api`, `grpc`, `web`, etc.).
- Generators scan `ReferencedAssemblySymbols` for trigger attributes (e.g. `[ApiEndpoint]`) and emit code without checking whether the current project actually wants that generator's output.
- Result: Generated code for one feature (e.g. API server endpoints) leaks into the compilation of unrelated projects (e.g. web-spa), causing missing reference errors for types like `FastEndpoints` and `BaseFastEndpoint`.

## Chosen Approach: MSBuild Properties (Option 1)

We will use MSBuild properties as the mechanism to control generator behavior.

Initial property under consideration:
- `<EnableApiEndpointGeneration>true</EnableApiEndpointGeneration>` (or `<EnableFastEndpointGeneration>`)

The generator will read this value at build time and skip generation when the property is not set (or explicitly set to false).

This approach integrates with the existing build system. Projects (or `Directory.Build.props`) can set the property directly, and it can eventually be defaulted based on template choices if desired. We are deliberately **not** using a hybrid with assembly attributes to avoid confusion between multiple control mechanisms.

## Scope

- Start with adding a flag to `FastEndpointSourceGenerator` (e.g. `EnableApiEndpointGeneration` or `EnableFastEndpointGeneration`) so it can be turned on/off.
- Decide the exact property name and default behavior.
- Add the property to relevant projects (starting with `web-spa.csproj` and `api-server.csproj`) to resolve current build issues.
- Establish a pattern that can be reused for other generators in the future.
- Update documentation.

## Checklist

### Phase 1: Design
- [ ] Finalize the MSBuild property name (starting with `EnableApiEndpointGeneration` or `EnableFastEndpointGeneration`)
- [ ] Decide default behavior (e.g. default to `true` when the analyzer is referenced, or require explicit opt-in)
- [ ] Define how the generator will read the property (`AnalyzerConfigOptions`)
- [ ] Document the decision and rationale in the task or a short ADR

### Phase 2: Implementation
- [ ] Add support in `FastEndpointSourceGenerator` (and any other generators as needed)
- [ ] Add the control mechanism to `web-spa.csproj` (and other relevant projects) so the current build errors are resolved
- [ ] Add tests for the new control behavior in the analyzer test project
- [ ] Ensure diagnostics are emitted when a generator is suppressed or when a required dependency is missing

### Phase 3: Cleanup & Documentation
- [ ] Update developer documentation on how to control generator output
- [ ] Update any relevant templates or example projects
- [ ] Remove or deprecate any workarounds that were added because of this issue

## Notes

- This issue became visible while debugging persistent `CS0246` errors for `FastEndpoints` and `BaseFastEndpoint` when building `web-spa`, even in full solution builds.
- Important distinction: The existing `api`, `grpc`, `web` etc. "feature flags" in this repository are primarily **template parameters** used at `dotnet new` time to control what gets scaffolded. They are a different concern from controlling which source generators run inside an already-created project.
- The generator currently has no awareness of project intent beyond the presence of trigger attributes in referenced assemblies.

## Implementation Notes

(Record design decisions and progress here)
