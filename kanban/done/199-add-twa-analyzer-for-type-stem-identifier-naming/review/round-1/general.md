# Round 1 — general
**Date:** 2026-08-20
**Scope reviewed:** TWA0023 analyzer + attribute + tests + AGENTS.md / Unshipped.md

## Summary

TWA0023 matches the plan: `Naming` / Warning / `isEnabledByDefault: false`, identifier
`EndsWith` the `OriginalDefinition.Name` stem (`OrdinalIgnoreCase`, interface I-strip), no
vendor-prefix heuristic, `[TypeStemIdentifier(reason)]` simple-name hatch (empty/whitespace still
flags), `GeneratedCodeAnalysisFlags.None`, Unshipped.md `Disabled`, AGENTS.md pointer-only row, and
package-range comments `TWA0002–0016, TWA0020–0023`. `.editorconfig` does not enable the rule.
Required matrix cases (exact, mismatch, I-strip, qualifier-head, primitives, boxes, opt-out + empty
reason, foreach, discard, arrays, override/explicit-impl, pragma) plus the IHttpClientFactory true
positive are present. Descriptor assertion locks default-off. No match/hatch/default-on bugs found.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-analyzers-tests/type-stem-identifier-analyzer-tests.cs:264
- Description: The documented do-not-skip set is almost unproven. `Given_ILogger_Stem_Is_Logger`
  claims ILogger is “not in the skip set” but only uses `logger` / `catalogLogger`, which pass
  whether the type is analyzed (stem `Logger`) or skipped entirely. DateTime, Guid, TimeSpan,
  CancellationToken, and enums-as-types have no cases at all. Enum *members* (the Design skip) are
  also untested — dropping `ContainingType?.TypeKind == TypeKind.Enum` would flag every named
  value and no test would fail. Only `Given_IHttpClientFactory_Factory_Flags` (`factory` →
  `HttpClientFactory`) actually locks a do-not-skip entry with a true positive.
- Suggestion: Add true-positives that fail if the type is skipped (`ILogger<T> log`, `DateTime dt`,
  `Guid id`, `TimeSpan ts`, `CancellationToken ct`, `HttpStatusCode code`) and one clean enum-member
  case (`enum Color { Red }`).
- Status: open
