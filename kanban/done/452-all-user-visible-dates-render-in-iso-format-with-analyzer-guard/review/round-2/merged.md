# Round 2 — merged findings
**Date:** 2026-10-30
**Sources:** general (claude-sonnet-5-5 sub-agent a8b7dccb3ea1223e3, read-only), orchestrator verification (Claude Fable 5.1)
**Scope:** delta 542d1d24e..HEAD (fix pass for round 1 M1–M5: commits fd2e5c0be, cd2fe21d0), product paths only

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 3 | 0 |
| nit | 0 | 2 | 0 |

## Orchestrator verification (this worktree, 2026-10-30)

- `./bin/dev build`: 0 warnings, 0 errors.
- `tests/analyzers/timewarp-architecture-analyzers-tests`: 259/259 pass (was 257 in round 1; +2 new cases).
- `tests/container-apps/web/web-spa-integration-tests`: 578/578 pass (was 576; +2 new pins).
- `./bin/dev lint-skills`: 9/9 pass (`skills/tw-blazor/SKILL.md` changed).
- No `TWA0036` suppression anywhere (reviewer grep across cs, razor, props, targets, csproj, `.editorconfig`, `.globalconfig`, json).
- Every remaining custom-format date `ToString` in `source/`, `tests/`, `tools/` passes `CultureInfo.InvariantCulture`; the one `:yyyy-MM-dd` hole outside the analyzer tests sits under `FormattableString.Invariant` (`contracts-generator-tests.cs:285`).
- Orchestrator read `IsInInvariantInterpolation` (`iso-date-rendering-analyzer.cs:259-279`) and confirms M6 by code: after the climb reaches the `IArgumentOperation`, nothing checks that the climb passed through an `IInterpolatedStringHandlerCreationOperation`, so the provider check applies to a plain `string` parameter too.
- Restatement vs task (round 2): the restatement's fix-pass bullet (invariant provider required for custom formats, handler test, `+=`, new pins, standalone skill section) matches what the delta does and what round 1 asked for. No misread.

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/iso-date-rendering-analyzer.cs:158 (`DescribeToString`), :227 (`DescribeHoleFormat`), :237-253 (`NeedsInvariantProvider`, `HasInvariantProviderArgument`, `IsInvariantProvider`)
- Description: An approved custom format was accepted whatever provider accompanied it; interpolation holes with a custom format could never carry one.
- Source: general
- Disposition notes: Fixed in fd2e5c0be. `ToString(customFormat)` flags unless an `IFormatProvider` argument is `CultureInfo.InvariantCulture` or `DateTimeFormatInfo.InvariantInfo`. A hole with a custom format flags unless its interpolated string is the direct argument of `FormattableString.Invariant` or a call given the invariant provider; both lowerings (`IInterpolationOperation` and handler `AppendFormatted`) climb the same chain. Message now ends "with CultureInfo.InvariantCulture". The three call sites (`AiPage.razor:90`, `screenshot-capture.cs:62`, `development-email-sender-server.cs:49`) plus `tools/dev-cli/services/entra-setup.cs:130` pass `CultureInfo.InvariantCulture`. Tests: `Given_Iso_Custom_Format_Without_Invariant_Culture_Flags` (no provider, `CurrentCulture`, provider variable, bare hole, `string.Create(CurrentCulture, …)` flag; `InvariantCulture` passes) and the approved-forms table. Residual over-acceptance of the exemption is M6.

### M2 — Severity: suggestion — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/iso-date-rendering-analyzer-tests.cs:215-249
- Description: The `AppendFormatted` handler branch had no test.
- Source: general
- Disposition notes: Fixed in fd2e5c0be. `Given_Interpolated_String_Handler_Flags_Each_Non_Iso_Hole_Once` uses `StringBuilder.Append($"…")`: no format, `:u`, `:yyyy-MM-dd`, and `,10` each report once; `:O` and `Append(CultureInfo.InvariantCulture, $"…")` are clean. Spans hand-checked by the reviewer.

### M3 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credential-row-presenter-tests.cs:129-140; features/application/ask-settings-menu-tests.cs:68-71, :145-148; features/application/catalog-agent-screenshot-tests.cs:164-173
- Description: `CreatedText`, `ExpiryText`, and `ScreenshotCapture.FileName` were asserted only against themselves.
- Source: general
- Disposition notes: Fixed in fd2e5c0be. `Created_Text_Is_A_Local_Iso_Instant` matches the local-instant regex and parses back to `credential.CreatedAt` (the fixture has zero sub-second ticks). `Show_Active_With_Its_Expiry` extracts the visible `Expires at …` text, matches the regex, and parses back to `expiresAt` truncated to seconds, matching the `sszzz` render. `Screenshot_File_Name_Is_An_Iso_Basic_Utc_Stamp` pins `screenshot-20261028T123456Z-7.png` from a -04:00 capture.

### M4 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/iso-date-rendering-analyzer.cs:124, :217-225
- Description: `text += date` was not flagged.
- Source: general
- Disposition notes: Fixed in fd2e5c0be. `OperationKind.CompoundAssignment` registered; `AnalyzeCompoundAssignment` reports when the operator is `Add`, the target is `string`, and the value is a date type. Design region lists `text += date`. Test expectation at `iso-date-rendering-analyzer-tests.cs:283`.

### M5 — Severity: nit — Status: fixed
- File: skills/tw-blazor/SKILL.md:234-249, :272
- Description: The ISO rule was a clause of the grid recipe and omitted the invariant culture.
- Source: general
- Disposition notes: Fixed in fd2e5c0be. Standalone `# Rendered dates` section: invariant culture required, `yyyy-MM-dd` for a date, `.ToLocalTime()` with `zzz` for local, `.UtcDateTime` with `'Z'` for UTC, the interpolation rule, and why `u` is out. Grid item 6 is a one-line pointer. Present tense, no history, no client names. `dev lint-skills` 9/9.

### M6 — Severity: suggestion — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/iso-date-rendering-analyzer.cs:268-278 (`IsInInvariantInterpolation`); Design region :18-22
- Description: The provider-call exemption for an interpolation hole fires for any invocation that receives an invariant `IFormatProvider`, whether or not the interpolated string binds to a handler that takes that provider. When the parameter is `string`, `object`, or a `FormattableString` other than `FormattableString.Invariant`, the string is built under the current culture before the callee sees it, and the provider only affects the callee. Example: `Foo(CultureInfo.InvariantCulture, $"{at:yyyy-MM-dd}")` with `Foo(IFormatProvider, string)`, or `string.Format(CultureInfo.InvariantCulture, $"{at:yyyy-MM-dd}")`. The hole is an `IInterpolationOperation`; the climb reaches the `IArgumentOperation` with no `IInterpolatedStringHandlerCreationOperation` in between; `HasInvariantProviderArgument(call)` is true; no diagnostic. The Design region sentence "any invocation that passes the invariant provider to an IFormatProvider parameter" states the over-claim. No in-repo site hits this today (the sweep is clean), so this is a guarantee gap, not a rendered defect.
- Suggestion: Accept the provider-call exemption only when the climb passed through an `IInterpolatedStringHandlerCreationOperation` (or when the argument's parameter type carries `[InterpolatedStringHandler]`). Keep `FormattableString.Invariant` as the separate method-name check. Add a flagged test row with a `Foo(IFormatProvider, string)` stub (or `string.Format(CultureInfo.InvariantCulture, $"{at:yyyy-MM-dd}")`) next to the passing `string.Create` row, and tighten the Design sentence to "a handler-taking call given the invariant provider".
- Source: general; confirmed by orchestrator code read
- Disposition notes:

## Duplicates / conflicts

- None. One new finding from one reviewer, confirmed by the orchestrator against the analyzer source.
- No finding was dropped.

## Fix-pass brief

Fix M6 on this task id (no sibling task). Keep product edits to the analyzer (`IsInInvariantInterpolation` and its Design region) and `iso-date-rendering-analyzer-tests.cs`. After the fix: `./bin/dev build` (0/0) and the analyzer test suite. Re-review lands under `review/round-3/`.
