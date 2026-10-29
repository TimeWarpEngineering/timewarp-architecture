# All user-visible dates render in ISO format with analyzer guard

## Description

ALL DATES SHOULD BE EXPRESSED IN ISO FORMAT. Reported 2026-10-28: Settings
shows `Created 10/5/2026, 7:51:51 PM`. That string is
CredentialRowPresenter.CreatedText (`M/d/yyyy, h:mm:ss tt`). Sweep every
user-visible date to ISO 8601 and add a compile-time analyzer so no new
culture-shaped date can land.

## Goal (implementer restatement)

Every date or time the app renders for a person reads as ISO 8601, and the build refuses new
culture-shaped renderings.

Acceptance:

- Instants render as `yyyy-MM-ddTHH:mm:sszzz` (local, with offset) where the UI shows local
  time, or `yyyy-MM-ddTHH:mm:ssZ` where it already showed UTC. Calendar dates render as
  `yyyy-MM-dd`. Fractional seconds stay out of user text (`O` stays approved for machine text).
- The six listed offenders plus every other site the sweep finds (`"u"` feedback timestamps,
  the AI page `:u` hole, the screenshot/dev-mail file stamps) use those shapes; tests that pinned
  the old strings pin the new ones.
- Analyzer TWA0036 (Design, Warning) flags `ToShort/LongDate/TimeString`, a date-type
  `ToString` with no format or a non-ISO constant format, an interpolation hole of a date type
  with no format or a non-ISO one, and string concatenation of a date. Razor `@date` content
  (implicit ToString) is flagged too. Approved shapes are ISO 8601 extended and basic forms plus
  `O`/`o`/`s`. Registered in `AnalyzerReleases.Unshipped.md` with Jaribu tests.
- `dev build` 0 warnings 0 errors; affected test suites pass; `ganda repo audit` clean.

## Checklist

- [x] Convert the known offenders to ISO 8601 (instants as round-trip `O`,
  calendar dates as `yyyy-MM-dd`; keep ToLocalTime where the UI shows local):
  - credential-row-presenter.cs:102 CreatedText (`M/d/yyyy, h:mm:ss tt`)
  - credential-row-presenter.cs:148 RelativeTime fallback (`on M/d/yyyy`)
  - SuperheroPage.razor:30 (`MM/dd/yyyy`)
  - WeatherForecastsPage.razor:34 (ToShortDateString)
  - AskSettingsMenu.razor:57 (ToString "t", CurrentCulture)
  - AssemblyInfoModal.razor:40 (`yyyy-MM-dd HH:mm:ss` — space separator, no
    offset; make it true ISO)
- [x] Re-sweep source/ for other render paths (ToString date specifiers,
  ToShort/Long Date/TimeString, CurrentCulture date formats) including tests
  that pin the old strings, and update them.
- [x] Add analyzer TWA0036 (next free id; TWA0035 is latest in
  AnalyzerReleases.Unshipped.md) banning non-ISO date rendering in app code:
  ToShort/LongDate/TimeString and DateTime/DateTimeOffset.ToString with a
  format that is not an approved ISO shape. Warning severity like the other
  TWA design rules; register in AnalyzerReleases.Unshipped.md with tests.
- [x] `dev build` is 0 warnings, 0 errors after the sweep.

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)
- Implemented 2026-10-30 (ganda task work implement oracle, Claude Opus 5.5)
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session 3bea41c0-e2cf-4643-be7c-e275aabfee89, max-turns 500, Done — 2026-10-29T21:20:26Z

## Notes

TS side is clean (no toLocale/toDateString in web-spa source/). The relative
buckets in RelativeTime ("3 minutes ago") are not dates and stay. Decided
format examples: instant `2026-10-05T19:51:51-04:00` (O), date `2026-10-05`.
Implementer confirms exact shapes before editing pinned tests.

## Results

Shapes chosen: an instant shown in local time renders `yyyy-MM-dd'T'HH:mm:sszzz`
(`2026-10-05T19:51:51-04:00`); an instant the UI already showed in UTC renders
`yyyy-MM-dd'T'HH:mm:ss'Z'`; a calendar date renders `yyyy-MM-dd`. `O` was not used for user
text because it adds seven fractional digits.

Product sweep (web-spa unless noted):

- `credential-row-presenter.cs` CreatedText → local instant; RelativeTime fallback →
  `on yyyy-MM-dd` (doc comment and `credential-row-presenter-tests.cs` pin updated).
- `SuperheroPage.razor`, `WeatherForecastsPage.razor` → `yyyy-MM-dd` (invariant).
- `AskSettingsMenu.razor` ExpiryText → local instant, invariant culture (was `"t"`, current culture).
- `assembly-info-text.cs` CommitDate (the modal row the task named) → `yyyy-MM-dd'T'HH:mm:ssK`;
  `assembly-info-copy-tests.cs` expected clipboard text → `2026-10-28T12:34:56`.
- Sweep finds beyond the list: `FeedbackAdminListPage`, `FeedbackAdminItemPage` (×2),
  `FeedbackItemPage` used `ToString("u")` (space separator) → UTC instant; `AiPage` `{SetAt:u}`
  → UTC instant; `screenshot-capture.cs` file name `yyyyMMdd-HHmmss` → `yyyyMMdd'T'HHmmss'Z'`;
  web `development-email-sender-server.cs` pickup stamp `yyyyMMddHHmmssfff` →
  `yyyyMMdd'T'HHmmss.fff'Z'` (ISO basic).
- TS side unchanged (already clean).

Analyzer: `source/analyzers/timewarp-architecture-convention-analyzers/iso-date-rendering-analyzer.cs`
(TWA0036, Design, Warning), registered in `AnalyzerReleases.Unshipped.md` (reserved note removed).
It flags ToShort/LongDate/TimeString, format-less `ToString`, non-ISO constant formats,
interpolation holes with no or non-ISO format (handler and FormattableString lowerings),
string concatenation of a date, and razor `@date` content (`RenderTreeBuilder.AddContent`
with a boxed date). Approved: `O`/`o`/`s` and ISO 8601 extended/basic custom shapes; `u` is
rejected. Razor-generated trees are analyzed; other generated trees are exempt. Scope is every
compilation the convention analyzers attach to (tests included). Tests:
`tests/analyzers/timewarp-architecture-analyzers-tests/iso-date-rendering-analyzer-tests.cs`
(8 cases including an approved/rejected format table). `skills/tw-blazor/SKILL.md` grid
rule 6 names the shapes and TWA0036.

Gates run (2026-10-30, this worktree):

- `./bin/dev build`: 0 warnings, 0 errors.
- Manual probe: a temporary `@hero.BirthDate`, `.ToShortDateString()`, and `$"{BirthDate}"` in
  SuperheroPage.razor produced three TWA0036 errors at the `.razor` line; reverted.
- `tests/analyzers/timewarp-architecture-analyzers-tests`: 257/257.
- `tests/container-apps/web/web-spa-integration-tests`: 576/576.
- `./bin/dev lint-skills`: 9/9 skills pass.
- `./bin/dev check-version`: source 2.0.0-beta.29 ahead of NuGet beta.28.
- `./bin/dev template-smoke` (default variant): SUCCEEDED.
- `ganda repo audit`: passes. One advisory warning (`kanban-landed`: 8 other tasks' claims
  with no usable worktree: 070-001, 104-024/025/026, 123, 141, 198, 244). Those belong to other
  tasks' holders, so they are not released from this id.

### How to validate

Smoke:

1. `./bin/dev build` from the task worktree.
2. `(cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release)`.
3. `(cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release)`.
4. Add `<Cell>@hero.BirthDate.ToShortDateString()</Cell>` to `SuperheroPage.razor`, then run
   `dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Release`. Revert it.
5. Optional: `dev run`, open Settings → credentials and the Ask settings menu.

Expect:

1. 0 warnings, 0 errors.
2. All tests pass, including the 8 `Should_Require_Iso_Date_Rendering` cases.
3. All tests pass (credential presenter, assembly-info copy, ask settings menu).
4. The build fails with `TWA0036: ToShortDateString() does not render ISO 8601` at the
   SuperheroPage.razor line.
5. The credential row reads `Created 2026-10-05T19:51:51-04:00` (your local offset). The
   expiry reads `Expires at <yyyy-MM-ddTHH:mm:ss±hh:mm>`. Feedback timestamps read
   `yyyy-MM-ddTHH:mm:ssZ`.
