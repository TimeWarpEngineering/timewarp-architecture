# Round 3 — merged findings
**Date:** 2026-10-30
**Sources:** general (claude-sonnet-5-5 sub-agent, read-only), orchestrator verification (Claude Fable 5.1)
**Scope:** delta 9dae2b281..20ac05ecd (fix pass for round 2 M6), product paths only

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 2 | 0 |

## Orchestrator verification (this worktree, 2026-10-30)

- `./bin/dev build`: 0 warnings, 0 errors. Every compilation the convention analyzers attach to compiled under the tightened TWA0036 rule, so no in-repo site regressed into a false positive.
- `tests/analyzers/timewarp-architecture-analyzers-tests`: 259/259 pass. The count is unchanged from round 2 because the two new rows sit inside the existing `Given_Iso_Custom_Format_Without_Invariant_Culture_Flags` case.
- Delta is one product commit (`20ac05ecd`) touching only `iso-date-rendering-analyzer.cs` (+33/−8) and `iso-date-rendering-analyzer-tests.cs` (+10), plus `task.md`.
- Orchestrator read `IsInInvariantInterpolation` and `HandlerTakesCallProvider` (`iso-date-rendering-analyzer.cs:266-304`): the exemption now needs a captured `IInterpolatedStringHandlerCreationOperation` whose `HandlerCreation` passes an `IFormatProvider` parameter from a `CallsiteArgument` placeholder, and the enclosing call must pass the invariant provider. A bind-time lowering (plain `string`, `object`, or `FormattableString` parameter) never creates a handler, so `handler` stays null and only the `FormattableString.Invariant` name check can exempt it.
- Orchestrator hand-counted the two new spans (line 16 cols 55–71, line 17 cols 49–65) and they match the raw-string source; the passing suite confirms both rows flag.
- Suppression grep for `TWA0036` across `source/`, `tests/`, `tools/` (cs, razor, props, targets, editorconfig, globalconfig), excluding the analyzer's own files and `AnalyzerReleases`: no hits.
- Restatement vs task (round 3): the restatement's round-2 fix-pass bullet (handler-taking call only, `string.Format(InvariantCulture, $"…")` and `Foo(InvariantCulture, $"…")` flag, `FormattableString.Invariant` stays exempt, flagged test rows, "handler-taking call" Design wording) matches the delta exactly and matches M6's suggestion. No misread.
- Considered and not raised: `HasInvariantProviderArgument(call)` checks that some `IFormatProvider` argument of the call is invariant rather than the specific argument the placeholder's `ArgumentIndex` points at. No BCL handler-taking API takes two providers, so the distinction has no observable effect; recording it here so a later reader knows it was weighed.

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/iso-date-rendering-analyzer.cs
- Description: An approved custom format was accepted whatever provider accompanied it.
- Source: general (round 1)
- Disposition notes: Fixed in fd2e5c0be, verified round 2. Unchanged by this delta.

### M2 — Severity: suggestion — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/iso-date-rendering-analyzer-tests.cs
- Description: The `AppendFormatted` handler branch had no test.
- Source: general (round 1)
- Disposition notes: Fixed in fd2e5c0be, verified round 2. The handler test's `Append(CultureInfo.InvariantCulture, $"…")` row still passes under the round 3 rule (reviewer and orchestrator traced it through `HandlerTakesCallProvider`).

### M3 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests (credential-row-presenter-tests.cs, ask-settings-menu-tests.cs, catalog-agent-screenshot-tests.cs)
- Description: `CreatedText`, `ExpiryText`, and `ScreenshotCapture.FileName` were asserted only against themselves.
- Source: general (round 1)
- Disposition notes: Fixed in fd2e5c0be, verified round 2. Unchanged by this delta.

### M4 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/iso-date-rendering-analyzer.cs
- Description: `text += date` was not flagged.
- Source: general (round 1)
- Disposition notes: Fixed in fd2e5c0be, verified round 2. Unchanged by this delta.

### M5 — Severity: nit — Status: fixed
- File: skills/tw-blazor/SKILL.md
- Description: The ISO rule was a clause of the grid recipe and omitted the invariant culture.
- Source: general (round 1)
- Disposition notes: Fixed in fd2e5c0be, verified round 2. Unchanged by this delta.

### M6 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/iso-date-rendering-analyzer.cs:266-304 (`IsInInvariantInterpolation`, `HandlerTakesCallProvider`); Design region :18-25
- Description: The provider-call exemption for an interpolation hole fired for any invocation that received an invariant `IFormatProvider`, including a plain `string`/`object`/`FormattableString` parameter where the string is built under the current culture first.
- Source: general (round 2); confirmed by orchestrator code read
- Disposition notes: Fixed in 20ac05ecd. The climb now captures the `IInterpolatedStringHandlerCreationOperation` it passes through (`handler ??=`), and the provider-call branch requires `handler is not null && HandlerTakesCallProvider(handler) && HasInvariantProviderArgument(call)`. `HandlerTakesCallProvider` looks for an `IFormatProvider`-typed constructor argument whose value unwraps to an `IInterpolatedStringHandlerArgumentPlaceholderOperation` with `PlaceholderKind: CallsiteArgument` (the `[InterpolatedStringHandlerArgument("provider")]` shape of `DefaultInterpolatedStringHandler` and `AppendInterpolatedStringHandler`). `FormattableString.Invariant` remains its own name check. Two flagged rows added: `string.Format(CultureInfo.InvariantCulture, $"Set {day:yyyy-MM-dd}.")` and `Text.Of(CultureInfo.InvariantCulture, $"Set {day:yyyy-MM-dd}.")` with a `Text.Of(IFormatProvider, string)` stub, expected at (16, 55–71) and (17, 49–65). The `string.Create(InvariantCulture, …)` and `StringBuilder.Append(InvariantCulture, …)` rows still pass. The Design region now reads "a handler-taking call given the invariant provider" and names the two shapes that still flag. `IsFormatProviderType` is shared with `HasInvariantProviderArgument`.

## Duplicates / conflicts

- None. One reviewer, no new findings, orchestrator concurs.
- No finding was dropped.

## Disposition

0 open. Every raised finding (M1–M6) is fixed. Outcome: `clean`. See `review/disposition.md`.
