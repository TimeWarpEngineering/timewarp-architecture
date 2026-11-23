# Round 2 — merged findings
**Date:** 2026-11-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: documentation/developer/guides/analyzer-package-split.md:61
- Description: Round 1: the ID table stopped at TWA0039–TWA0043 and said no id above TWA0043 was declared.
- Suggestion: Add TWA0044–TWA0046 and name TWA0047 as the next free id.
- Source: general
- Disposition notes: Re-verified on `00eec4573`. The table row is present. Line 75 says no id above TWA0046 and the next free id is TWA0047. Declared ids in both analyzer projects stop at TWA0046. The task 486 history in that guide is unchanged.

### M2 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/contract-spa-agreement-analyzer.cs:216
- Description: Round 1: `HasMockFactory` treated any static `GetMockResponseFactory` as present, which the mock registry does not register.
- Suggestion: Use the registry predicate (public, static, parameterless). Flag a private or parameterized method of that name. Keep the blank `[NoMockResponse]` case.
- Source: general
- Disposition notes: Re-verified on `00eec4573`. The predicate matches `mock-response-factory-registry-generator.cs:51` (public, static, parameterless). `Factory_The_Registry_Does_Not_Register_Still_Flags` expects TWA0046 for `PrivateMock` and `ParameterizedMock`. `NoMockResponse_With_A_Reason_Opts_Out_And_A_Blank_Reason_Does_Not` still flags `BlankOptOut`. Recorded fix-pass result: that filter class 11/11.

## Issues

<!-- No new findings. -->

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
