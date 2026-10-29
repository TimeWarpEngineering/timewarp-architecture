# All user-visible dates render in ISO format with analyzer guard

## Description

ALL DATES SHOULD BE EXPRESSED IN ISO FORMAT. Reported 2026-10-28: Settings
shows `Created 10/5/2026, 7:51:51 PM`. That string is
CredentialRowPresenter.CreatedText (`M/d/yyyy, h:mm:ss tt`). Sweep every
user-visible date to ISO 8601 and add a compile-time analyzer so no new
culture-shaped date can land.

## Checklist

- [ ] Convert the known offenders to ISO 8601 (instants as round-trip `O`,
  calendar dates as `yyyy-MM-dd`; keep ToLocalTime where the UI shows local):
  - credential-row-presenter.cs:102 CreatedText (`M/d/yyyy, h:mm:ss tt`)
  - credential-row-presenter.cs:148 RelativeTime fallback (`on M/d/yyyy`)
  - SuperheroPage.razor:30 (`MM/dd/yyyy`)
  - WeatherForecastsPage.razor:34 (ToShortDateString)
  - AskSettingsMenu.razor:57 (ToString "t", CurrentCulture)
  - AssemblyInfoModal.razor:40 (`yyyy-MM-dd HH:mm:ss` — space separator, no
    offset; make it true ISO)
- [ ] Re-sweep source/ for other render paths (ToString date specifiers,
  ToShort/Long Date/TimeString, CurrentCulture date formats) including tests
  that pin the old strings, and update them.
- [ ] Add analyzer TWA0036 (next free id; TWA0035 is latest in
  AnalyzerReleases.Unshipped.md) banning non-ISO date rendering in app code:
  ToShort/LongDate/TimeString and DateTime/DateTimeOffset.ToString with a
  format that is not an approved ISO shape. Warning severity like the other
  TWA design rules; register in AnalyzerReleases.Unshipped.md with tests.
- [ ] `dev build` is 0 warnings, 0 errors after the sweep.

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)

## Notes

TS side is clean (no toLocale/toDateString in web-spa source/). The relative
buckets in RelativeTime ("3 minutes ago") are not dates and stay. Decided
format examples: instant `2026-10-05T19:51:51-04:00` (O), date `2026-10-05`.
Implementer confirms exact shapes before editing pinned tests.
