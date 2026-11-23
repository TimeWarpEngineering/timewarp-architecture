# Round 1 — general
**Date:** 2026-11-23
**Scope reviewed:** same as framework (branch vs master, 20 files)

## Summary

The change adds `DetailsCopyGenerator` to foundation-contracts-generators. It emits
`Command.From(I*Details)` with a `CopyBeyondDetails` partial hook, and a real `ICloneable.Clone`
on partial mutable details responses. It also adds `DetailsCopyAnalyzer` (TWA0047–TWA0049), marks
the five mutable details responses partial, implements the `Version` hook on
`UpdateSiteSettings.Command`, and adds a maintainer guide. The generator design is sound.
`MemberwiseClone` keeps get-only and `[Computed]` members, `UnsafeAccessor` rewrites get-only
collections, the hint names are deterministic, and the outputs are equatable strings. The
generator unit test emits and loads the compiled output, so From and Clone are proven at runtime.

The implementation matches the restatement, and the restatement matches the task and parent 489.
That includes not editing `tw-web-api-contracts`, which parent 489 forbids in a child task.

There are three findings. The runfile that tests the real contracts is never run by CI. An
explicit `ICloneable.Clone() => this` gets past both TWA0047 and the generator. The
analyzer and the generator repeat the same predicates and are kept in agreement only by comments.

No targeted command was run. Every finding comes from reading the code and the `dev test` /
aggregator discovery code.

## Issues

### Issue 1 — Severity: bug
- File: tests/analyzers/details-copy-contracts-tests.cs:1
- Description: The only tests that run From and Clone on the **real** contracts (GetProfile Avatar
  survives, GetRole get-only `Permissions`/`AgentScopes` copied, `Version` 7/8 through
  `CopyBeyondDetails`) are in a standalone runfile that no CI path runs. `dev test`
  (`tools/dev-cli/endpoints/test-command.cs:99-105`) runs only `*.csproj` under `tests/`.
  `tests/container-apps/web/web-jaribu-tests` compiles only
  `$(SourceDirectory)container-apps/web/features/**/*-tests.cs` and `platform/**/*-tests.cs`. The
  two analyzer test projects compile only their own folders. So the acceptance criterion "Tests
  show list copies are not the same reference, `Avatar` and get-only lists survive `Clone`, and
  `Version` round-trips through the hook" is proven by one manual run recorded in `## Results`,
  and nothing guards it against regression. A later change to `UpdateSiteSettings.Command` (for
  example, deleting the hook body) or to the Guard-bearing constructors would stay green in CI.
  The generator unit test covers synthetic types, not these contracts or this hook.
- Suggestion: Move the three tests into co-located runfiles that `web-jaribu-tests` already globs.
  Add them to the existing `features/profile/get-profile/get-profile-tests.cs` and
  `features/settings/update-site-settings/update-site-settings-tests.cs`, and add a new
  `features/admin/roles/get-role/get-role-tests.cs`. The other option is
  `tests/container-apps/web/web-contracts-tests`. Delete `tests/analyzers/details-copy-contracts-tests.cs`
  and update `### How to validate` to the new location.
- Status: open

### Issue 2 — Severity: bug
- File: source/analyzers/timewarp-architecture-convention-analyzers/details-copy-analyzer.cs:191
  (also `HasClone` at :306, and source/foundation/foundation-contracts-generators/details-copy-symbols.cs:271)
- Description: `CloneReturnsThis`, `IdentityCloneLocations`, and both `HasClone` copies find Clone
  with `type.GetMembers("Clone")`. An explicit implementation, `object ICloneable.Clone() => this`,
  has the member name `System.ICloneable.Clone`, so all of them miss it.
  1. TWA0047 does not fire.
  2. The generator decides there is no Clone, and `ImplementsCloneable` is already true, so it emits
     `public object Clone()` with no base list. That compiles next to the explicit implementation.
  3. Interface dispatch picks the explicit implementation, so `((ICloneable)response).Clone()`
     (the call TimeWarp.State makes) still returns `this`. The generated deep Clone is dead code,
     and the state snapshot shares the mutable instance.

  This is the exact failure TWA0047 exists to stop, and the build gives no signal. The repo's
  current identity clones all use the public form, but the explicit form is ordinary C#. The
  analyzer tests cover only the public expression-bodied and block forms.
- Suggestion: Find the Clone by its `ICloneable.Clone` implementation:
  `type.FindImplementationForInterfaceMember(iCloneableClone)`. You can also include methods whose
  `ExplicitInterfaceImplementations` contain `System.ICloneable.Clone`. Do this in the analyzer
  (identity check, location, and `HasClone`) and in the generator's `HasClone`, so the generator
  stays silent and TWA0047 reports. Add an analyzer test for
  `object System.ICloneable.Clone() => this;` on a mutable details response.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/details-copy-analyzer.cs:296-409;
  source/foundation/foundation-contracts-generators/details-copy-symbols.cs:180-330
- Description: The analyzer reports "this will not be generated" or "this must be partial" from
  its own copies of `IsDetailsInterface`, `IsMutable`, `InterfaceProperties`, `IsPartialChain`,
  `HasFrom`, `HasClone`, and `HasParameterlessConstructor`. The generator decides what to emit
  from a second copy, and `ContractAgreementAnalyzer.IsDetailsInterface` is a third. Design-region
  comments ("matches ContractAgreementAnalyzer.IsDetailsInterface") are the only thing that keeps
  them in agreement. The AGENTS.md hard rule says "When two things must agree, generate one from
  the other or add a build check." Issue 2 shows the risk: the same blind spot was written twice.
- Suggestion: Move the shared predicates into one file under `source/analyzers/shared/` (for
  example `details-copy-shape.cs`). Link it into the convention analyzers, as
  `hosted-route-discovery.cs` and `referenceable-resource-shape.cs` already are, and into
  foundation-contracts-generators with `<Compile Include … Link=…>`.
  `ContractAgreementAnalyzer.IsDetailsInterface` should call the same predicate.
- Status: open
