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

- [x] Signed-out Sign in row (typed source, not a string literal route)
- [x] Hidden when authenticated; Login still absent from `PageRegistry.All` and NavMenu
- [x] Ranking: "sign", "login" and "log in" each match the row
- [x] Tests (co-located Jaribu or the existing `command-palette-tests.cs` suite): signed-out
      roster contains Sign in; signed-in roster does not; running it navigates to the login URL
- [x] Purpose/Design regions reconciled on touched files
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`
- [x] Do **not** start an AppHost (`dev run`, `aspire run`, or `dotnet run` of the AppHost); record
      the browser check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 284163 (2026-09-30)
- 2026-09-30 implement (ganda task work): typed signed-out entry in `CommandPaletteRoster`;
  tests + gates green. No AppHost started; browser check not performed.

## Notes

- Follow-up from 239 validation (2026-09-30): Steve, signed out, saw only Home.
- Palette search today is deterministic string matching: name prefix, word start, substring,
  description, then name subsequence. It is not semantic, and this task doesn't change that.
- Memory discipline: run builds and tests serially and call `dotnet build-server shutdown`
  before finishing.

## Results

- **Choice: explicit typed signed-out entry** (not a registry concept), recorded in the Design
  region of `command-palette-roster.cs`. `CommandPaletteRoster.SignInRow` builds the row from
  `LoginPage.Title` ("Sign in") and `LoginPage.GetPageUrl()` — no hand-kept route string. Login
  stays out of `PageRegistry.All`; `Navigable` semantics are unchanged; no generator change.
- The row is added only when `user.Identity?.IsAuthenticated != true`. It is a `Page`-kind row, so
  running it takes the same CloseModal → `CommandPaletteRunner` → `RouteState.ChangeRoute` path as
  every page row.
- Return URL: `OpenActionSet` passes the current base-relative path; the target becomes
  `/Login?returnUrl=<escaped path>` (plain `/Login` on `/`), matching `RedirectToLogin`. LoginPage
  already validates it with `GetSafeReturnUrl`.
- Description `Log in: go to /Login`, so "sign" (name prefix), "login" (description word start)
  and "log in" (description prefix) all rank it first.
- TWA0009: Applications is the platform tier and may not reach the Identity slice, so the roster
  carries `[CrossSliceReference(typeof(LoginPage), …)]`, the same opt-out HomePage uses.
- Tests (`command-palette-tests.cs`): the signed-out roster = anonymous pages + Sign in (name and
  description pinned); the signed-in roster has no `/Login` row and `PageRegistry.All` has no
  `/Login`; `sign` / `Sign in` / `login` / `log in` each highlight Sign in; Enter from `/Counter`
  navigates to `/Login?returnUrl=%2FCounter`.
- Gates: `dev build` 0 warnings / 0 errors; `dev test` all suites passed; `ganda repo audit`
  passes (`bin/dev` was self-installed in this worktree first; it is untracked).
- **Browser check not performed.** No AppHost was started (task rule).

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-spa-integration-tests
dotnet test -c Release -- --filter-class CommandPalette
```

Manual, with an AppHost running: sign out, open any `TimeWarpPage`, press Ctrl-K, type `login`.

**Expect:**

- The filtered run reports 28 passed, 0 failed. That includes
  `Rank_The_Sign_In_Row_First_For_Sign_In_Wording` (4 inputs) and
  `Enter_On_Sign_In_Navigates_To_Login_With_The_Current_Path_As_Return_Url`.
- Manual: while signed out, the palette shows **Sign in** ("Log in: go to /Login") next to Home.
  Enter closes the palette and opens `/Login?returnUrl=<current page>`. After sign-in you land
  back on that page, and the palette no longer shows Sign in. NavMenu never lists Login.
