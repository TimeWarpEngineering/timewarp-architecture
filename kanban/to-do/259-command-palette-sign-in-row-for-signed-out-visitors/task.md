# Command palette: Sign in row for signed-out visitors

## Description

When no one is signed in, the Ctrl-K palette (task 239-003) lists only Home. Every other
registered destination needs a permission, and catalog commands only appear for authenticated
principals. Signed out, the palette is close to useless, and it doesn't offer the one thing a
signed-out visitor most needs: signing in.

Add a **Sign in** row that appears only while the principal is unauthenticated. Running it takes
the visitor to the login page, with the current URL as the return URL if the login page already
supports one. Once signed in, the row disappears. "Sign out" is already a catalog command that
shows for authenticated users.

## Requirements

- The row's source must not be a hand-kept route string. Pick one of these and record the choice
  in the owning file's Design region:
  - an explicit, typed signed-out entry built from `LoginPage.GetPageUrl()` (or its generated
    route member) in `CommandPaletteRoster`; or
  - a registry-level concept, for example a `[Page]` opt-in meaning "destination for anonymous
    visitors only".

  Keep `Navigable` semantics unchanged. Login stays out of `PageRegistry.All` (239-001
  excluded it on purpose: NavMenu must not list it).
- The row is shown only when `user.Identity?.IsAuthenticated != true` and is hidden once signed in.
- It ranks like any other row: typing "sign", "login" or "log in" finds it. Give it a description
  (e.g. "Go to /Login") so a description match works, and add "log in" / "login" wording if the
  name alone would not match.
- Running it uses the same navigation path as page rows (CloseModal, then navigate), so focus
  return and modal state behave identically.
- The palette is hosted on `TimeWarpPage` only; the Login screen (`TimeWarpFocusedPage`) has no
  palette. That doesn't change.

## Checklist

- [ ] Signed-out Sign in row (typed source, not a string literal route)
- [ ] Hidden when authenticated; Login still absent from `PageRegistry.All` and NavMenu
- [ ] Ranking: "sign", "login" and "log in" each match the row
- [ ] Tests (co-located Jaribu or the existing `command-palette-tests.cs` suite): signed-out
      roster contains Sign in; signed-in roster does not; running it navigates to the login URL
- [ ] Purpose/Design regions reconciled on touched files
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`
- [ ] Do **not** start an AppHost (`dev run`, `aspire run`, or `dotnet run` of the AppHost); record
      the browser check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 284163 (2026-09-30)

## Notes

- Follow-up from 239 validation (2026-09-30): Steve, signed out, saw only Home.
- Palette search today is deterministic string matching: name prefix, word start, substring,
  description, then name subsequence. It is not semantic, and this task doesn't change that.
- Memory discipline: run builds and tests serially and call `dotnet build-server shutdown`
  before finishing.

## Results

*(fill when done)*

### How to validate

*(required before done)*
