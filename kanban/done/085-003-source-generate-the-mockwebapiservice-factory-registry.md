# Source-generate the MockWebApiService factory registry

## Parent
085-analyzer-and-source-gen-opportunities-to-remove-inference-collected-candidates

## Description

`MockWebApiService.Factories` is a hand-maintained `Dictionary<Type, Delegate>`; a contract can
define `GetMockResponseFactory()` and never be registered (task 078 had to hand-add the CreateRole
entry). Source-generate the registry: scan referenced contract types for the
`GetMockResponseFactory()` convention and emit the dictionary population into a partial of
`MockWebApiService` — the manual registration step disappears entirely.

## Checklist

- [x] `MockResponseFactoryRegistryGenerator` in the existing `timewarp-architecture-analyzers`
      generator assembly (web-spa already references it as an analyzer — zero new wiring).
      Scans referenced *contracts* assemblies for public static parameterless
      `GetMockResponseFactory()` + nested Query/Command; emits a sorted
      `GeneratedMockResponseFactories.Create()` registry. Gated on the compilation declaring a
      `MockWebApiService` class, so servers referencing contracts get nothing.
- [x] Registry design refined from "partial class": the generator emits a standalone
      `GeneratedMockResponseFactories` class and the hand-written service consumes it — the
      manual dictionary is gone. The old "comment a line out to use the real API" affordance is
      preserved as an explicit `UseRealApi` exclusion set in the hand file.
- [x] Generator tests (2): factory contract registered / factory-less contract absent;
      no `MockWebApiService` host → nothing emitted. Sourcegen suite 16/16.
- [x] Mock mode verified: built web-spa with the `MOCK_WEB_API` define enabled — compiles clean
      against the generated registry. All suites green (analyzer 26, sourcegen 16, contracts 7,
      web-server integration 22); `dev build` 0/0.

## Results

**The generator's first real output caught live drift**: the hand dictionary had **7** entries;
the generated registry has **8** — `GetSignInToken` defined a factory that was never registered.
Exactly the agreement-by-memory failure this candidate predicted. From here, defining
`GetMockResponseFactory()` on a contract IS registering it; new contracts (like the four roles
factories added this week) appear in mock mode with zero registration work.

## Notes

- Convention detection: `public static MockResponseFactory<T> GetMockResponseFactory()` on the
  static contract shell. Cross-assembly metadata scan, like the FastEndpoint generator does.
- Incidental fix: two pre-existing brace-less `if`s in `ModalContainer.razor.cs` surfaced when
  the changed analyzer assembly forced full re-analysis of web-spa; brought up to repo style.
