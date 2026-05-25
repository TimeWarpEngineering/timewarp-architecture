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

- Add support for `<EnableApiEndpointGeneration>` MSBuild property in `FastEndpointSourceGenerator`.
- Default the generator to **off** (false) unless the property is explicitly set to `true`.
- Add the property to relevant projects (starting with `web-spa.csproj` and `api-server.csproj`) to resolve current build issues.
- Establish a pattern that can be reused for other generators in the future.
- Update documentation.

## Checklist

### Phase 1: Design
- [x] Finalize the MSBuild property name → `EnableApiEndpointGeneration`
- [x] Decide default behavior → `false` (opt-in required)
- [x] Define how the generator will read the property (`AnalyzerConfigOptionsProvider`)
- [x] Document the decision and rationale in the task (see Implementation Notes)

### Phase 2: Implementation
- [x] Add support in `FastEndpointSourceGenerator`
- [ ] Re-evaluate later: Decide whether to explicitly document `<EnableApiEndpointGeneration>false</EnableApiEndpointGeneration>` in non-API projects once more generators exist (currently only one generator, so documentation overhead is low)
- [x] Add `<EnableApiEndpointGeneration>true</EnableApiEndpointGeneration>` to `api-server.csproj` (the primary project that should generate FastEndpoints)
- [x] Add tests for the new control behavior in the analyzer test project (including disabled by default + explicitly enabled cases)
- [x] Emit a diagnostic (Warning) when `EnableApiEndpointGeneration=true` but required FastEndpoints types are missing (implemented in FastEndpointSourceGenerator)

### Phase 3: Cleanup & Documentation
- [ ] Update developer documentation on how to control generator output
- [ ] Update any relevant templates or example projects
- [ ] Remove or deprecate any workarounds that were added because of this issue

## Notes

- This issue became visible while debugging persistent `CS0246` errors for `FastEndpoints` and `BaseFastEndpoint` when building `web-spa`, even in full solution builds.
- Important distinction: The existing `api`, `grpc`, `web` etc. "feature flags" in this repository are primarily **template parameters** used at `dotnet new` time to control what gets scaffolded. They are a different concern from controlling which source generators run inside an already-created project.
- The generator currently has no awareness of project intent beyond the presence of trigger attributes in referenced assemblies.

## Implementation Notes

- **2026-05-25**: 
  - Chose MSBuild property name `EnableApiEndpointGeneration` (to match `[ApiEndpoint]` attribute naming).
  - Default value: `false` (opt-in / explicit enable required).
  - Updated `FastEndpointSourceGenerator` to respect this property via `AnalyzerConfigOptionsProvider`.
  - Generator is now disabled by default for all projects unless the property is set to `true`.
- Because the default is now safe (false), no changes are required in `web-spa.csproj` (or similar projects) to resolve the original build errors.
- 2026-05-25: Added `<EnableApiEndpointGeneration>true</EnableApiEndpointGeneration>` to `api-server.csproj`.
- Added diagnostic (SG002) when `EnableApiEndpointGeneration=true` but FastEndpoints or BaseFastEndpoint cannot be resolved.
- Note: With only one generator currently in the package, explicitly setting the property to false in client projects (like web-spa) adds little value and can be deferred until more generators are added.

**Checklist items completed on 2026-05-25:**
- All of Phase 1 (Design)
- First item of Phase 2: "Add support in `FastEndpointSourceGenerator`"
