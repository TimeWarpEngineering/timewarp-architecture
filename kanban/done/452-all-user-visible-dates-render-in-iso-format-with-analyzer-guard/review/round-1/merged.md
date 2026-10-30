# Round 1 — merged findings
**Date:** 2026-10-30
**Sources:** general (claude-sonnet-5-5 sub-agent, read-only), orchestrator verification (Claude Fable 5.1)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 3 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Orchestrator verification (this worktree, 2026-10-30)

- `./bin/dev build`: 0 warnings, 0 errors.
- `tests/analyzers/timewarp-architecture-analyzers-tests`: 257/257 pass.
- `tests/container-apps/web/web-spa-integration-tests`: 576/576 pass.
- No `TWA0036` suppression anywhere (`#pragma`, `[SuppressMessage]`, `.editorconfig`, `NoWarn`): confirmed by grep.
- No leftover culture-shaped renderings in `source/` or `tests/` (C#, razor, TS): confirmed by grep.
- web-spa `program.cs:131` `SetIsoCulture()` forces en-US in WASM; web-server has no request localization or
  default-culture override, so server-side rendering of the same components uses the host culture.
- Restatement vs task: the checklist says "instants as round-trip `O`" while the Notes example
  `2026-10-05T19:51:51-04:00` has no fractional seconds. The implementer chose the Notes shape
  (`yyyy-MM-dd'T'HH:mm:sszzz`) and documented why. Recorded under task `## Learnings`; not a defect.

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/iso-date-rendering-analyzer.cs:208 (`NonIsoConstantFormat` / `IsApprovedFormat`); call sites source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:90, source/container-apps/web/projects/web-spa/features/application/screenshot-state/screenshot-capture.cs:62, source/container-apps/web/platform/mail/development-email-sender-server.cs:48
- Description: An approved custom format is accepted whatever provider accompanies it, and an interpolation hole with a custom format can never carry one. Custom formats are culture-dependent: `:` renders the culture's `TimeSeparator` (`HH.mm.ss` where the separator is `.`), and `yyyy` renders the culture's default calendar (`th-TH` gives 2569). Only `O`, `o`, `s` are invariant. The three sites above use the thread culture. WASM is forced to en-US, but web-server renders the same razor under the host culture with no localization override, so TWA0036 still passes code that can render a non-ISO date. This is the exact guarantee the task asked the analyzer to provide.
- Suggestion: In the analyzer, accept a custom (non `O`/`o`/`s`) format only when the invocation also passes an `IFormatProvider` argument that is `CultureInfo.InvariantCulture` (or `DateTimeFormatInfo.InvariantInfo`); flag an interpolation hole with a custom format unless the interpolated string is the argument of `FormattableString.Invariant` / `string.Create(CultureInfo.InvariantCulture, …)` (or simply flag it and point at `ToString(fmt, CultureInfo.InvariantCulture)`). Convert the three sites to `ToString(fmt, CultureInfo.InvariantCulture)`. Add test rows: `at.ToString("yyyy-MM-dd")` with no provider flags; with `CultureInfo.CurrentCulture` flags; with `CultureInfo.InvariantCulture` passes. Update the analyzer Design region and the message text.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/analyzers/timewarp-architecture-analyzers-tests/iso-date-rendering-analyzer-tests.cs:177
- Description: The `AppendFormatted` branch (`DescribeAppendFormatted`) has no test. A string-typed or `object`-typed interpolated string binds as `IInterpolationOperation` (the `DefaultInterpolatedStringHandler` rewrite happens in lowering, which IOperation does not see); only a bind-time handler conversion (for example `StringBuilder.Append($"…")`, `Debug.Assert(cond, $"…")`) produces `IInterpolatedStringAppendOperation` with an `AppendFormatted` invocation. The Design region claims both lowerings are covered with no double report; nothing pins it.
- Suggestion: Add a case using `new StringBuilder().Append($"{at}")`, `$"{at:u}"`, `$"{at:yyyy-MM-dd}"`, and an aligned hole `$"{at,10}"`. Expect exactly one diagnostic per non-ISO hole and none for the ISO one.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credential-row-presenter-tests.cs:120 (also credential-list-render-tests.cs:55)
- Description: `CreatedText`, the string the original report complained about, is asserted only against itself (`ShouldContain(CreatedText(credential))`, `ShouldBe("Created " + CreatedText(credential))`). Nothing pins its ISO shape. The same holds for `ExpiryText` (AskSettingsMenu) and `ScreenshotCapture.FileName`. The acceptance line "tests that pinned the old strings pin the new ones" is met only for `RelativeTime` and the assembly-info copy.
- Suggestion: One assertion per site with a fixed instant. `CreatedText` and `ExpiryText` use `ToLocalTime()`, so match a regex `^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$`. `FileName` is UTC and deterministic: `screenshot-20261028T123456Z-<id>.png`.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/iso-date-rendering-analyzer.cs:185
- Description: `AnalyzeBinary` handles `IBinaryOperation` only. `text += date;` is an `ICompoundAssignmentOperation` and is not flagged, although it is the same implicit `ToString()`. The Design region lists concatenation as covered without this exception.
- Suggestion: Register `OperationKind.CompoundAssignment` and report when the operator is `Add`, the target type is `string`, and the value is a date type. Or add `+=` to the Design region's "Not covered" list.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: skills/tw-blazor/SKILL.md:255
- Description: The ISO rule is general (every rendered date, analyzed in all razor and C#) but is a clause of grid recipe item 6 under "Server-sorted FluentDataGrid". An author formatting a date outside a grid will not find it. It also omits `CultureInfo.InvariantCulture`, which every compliant call site passes (M1).
- Suggestion: Move the rule to a short standalone paragraph in the general Razor rules and leave item 6 a one-line pointer. State: pass `CultureInfo.InvariantCulture`; `.UtcDateTime` with the `'Z'` shape for UTC; `.ToLocalTime()` with the `zzz` shape for local.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Five findings from one reviewer, each independently re-verified by the orchestrator against the repo.
- No finding was dropped.

## Fix-pass brief

Fix M1–M5 on this task id (no sibling task). Keep product edits to: the analyzer and its tests, the three
call sites named in M1, the web-spa integration tests named in M3, and `skills/tw-blazor/SKILL.md`.
After the fix: `./bin/dev build` (0/0), the analyzer test suite, the web-spa integration suite, and
`./bin/dev lint-skills` (SKILL.md changed). Re-review lands under `review/round-2/`.
