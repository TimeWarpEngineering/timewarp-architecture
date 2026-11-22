# Round 1 — merged findings
**Date:** 2026-11-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: documentation/developer/guides/analyzer-package-split.md:60
- Description: The ID table stops at TWA0039–TWA0043, and line 74 says no `TWA*` id above TWA0043 is declared in either analyzer project. This change declares TWA0044, TWA0045, and TWA0046 on `ContractSpaAgreementAnalyzer`. The guide is the allocation list a later change reads before taking the next id, and that sentence is now false.
- Suggestion: Add a row for TWA0044–TWA0046 (SPA handler agreement with the contract: destructive confirm, catalog permissions, mock factory) and replace the "no id above TWA0043" sentence so the next free id is TWA0047. Leave the done task 486 note as history.
- Source: general
- Disposition notes: fixed in the fix pass. Added the TWA0044–TWA0046 row (contract-spa-agreement) and changed the closing sentence to "no id above TWA0046; the next free id is TWA0047". The task 486 note is unchanged.

### M2 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/contract-spa-agreement-analyzer.cs:213
- Description: `HasMockFactory` returns true for any static method named `GetMockResponseFactory`. The mock-factory registry registers only a public, static, parameterless method (`mock-response-factory-registry-generator.cs:51`). A private method, or a static method with parameters, silences TWA0046 while `MockWebApiService` still falls through to the real API. No current contract in this diff has such a method.
- Suggestion: Use the registry predicate (public, static, parameterless). Add a case where a private or parameterized method of that name still reports TWA0046, and keep the blank `[NoMockResponse]` case.
- Source: general
- Disposition notes: fixed in the fix pass. `HasMockFactory` now uses the registry predicate (public, static, parameterless). New test `Factory_The_Registry_Does_Not_Register_Still_Flags` covers a private and a parameterized `GetMockResponseFactory`; the blank `[NoMockResponse]` case stays.

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
