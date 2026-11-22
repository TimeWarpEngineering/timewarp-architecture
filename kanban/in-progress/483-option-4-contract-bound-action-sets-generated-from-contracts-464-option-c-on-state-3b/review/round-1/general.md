# Round 1 — general
**Date:** 2026-11-22
**Scope reviewed:** branch `task/483-option-4-contract-bound-action-sets-generated-from` vs `master`, checked against `task.md` (original requirements and `## Goal (implementer restatement)`) and the recorded `## Results` / `### How to validate`. Gates were not re-run.

## Summary

`[ContractState]` on a state emits, per contract, an ActionSet (`Action` plus `DefaultApiHandler`), a typed `Sender.Send` dispatch method, and the hook declarations. Required hooks are extended partial methods (`On<C>Loaded` for every Query, `Build<C>Request` for `DraftRequests`, `Confirm<C>Async` for `[Destructive]`), so a missing body is CS8795. Optional hooks are `partial void`. No Razor is generated. Roles uses it for GetRoles, SetRolePermissions, and SetRoleAgentScopes. CreateRole stays hand-written. Mediator's generated ClientPipeline sender registers the three new actions with the same behavior chain as CreateRole, including action tracking. The recorded build, generator tests (10/10, project 173/173), analyzer tests (10/10, project 273/273), Roles runtime tests (3/3), template-smoke, check-version, and audit cover that path. The generated output on disk matches the emitter. Two maintainability holes are open: the analyzer ID guide still says nothing above TWA0043 exists, and TWA0046 treats a factory as present in a case the mock registry will not register.

## Issues

### Issue 1 — Severity: suggestion
- File: documentation/developer/guides/analyzer-package-split.md:60
- Description: The ID table stops at TWA0039–TWA0043, and line 74 says no `TWA*` id above TWA0043 is declared in either analyzer project. This change declares TWA0044, TWA0045, and TWA0046 on `ContractSpaAgreementAnalyzer` in `timewarp-architecture-convention-analyzers` (`AnalyzerReleases.Unshipped.md`). The guide is the allocation list a later change reads before taking the next id, and that sentence is now false.
- Suggestion: Add a row for TWA0044–TWA0046 (SPA handler agreement with the contract: destructive confirm, catalog permissions, mock factory) and replace the "no id above TWA0043" sentence so the next free id is TWA0047. Leave the done task 486 note as history.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/contract-spa-agreement-analyzer.cs:213
- Description: `HasMockFactory` returns true for any static method named `GetMockResponseFactory`. `MockResponseFactoryRegistryGenerator` registers a factory only when the method is public, static, and parameterless (`source/analyzers/timewarp-architecture-analyzers/generators/mock-response-factory-registry-generator.cs:51`). A private method, or a static method with parameters, silences TWA0046 while `MockWebApiService` still has no entry and falls through to the real API. That fall-through is the failure TWA0046 describes. No current contract in this diff has such a method; the recorded analyzer tests use a public parameterless factory.
- Suggestion: Use the registry predicate (public, static, parameterless). Add a case where a private or parameterized method of that name still reports TWA0046, and keep the blank `[NoMockResponse]` case.
- Status: open
