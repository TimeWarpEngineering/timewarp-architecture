# Add shell slots UserCard pin-bottom NavGroup SearchMode breakpoints

## Description

The portal sidebar pins a user card and a system group to the bottom of the nav, and the shell changes shape across three widths. This template already has a registry `NavGroup`, a command-palette search field, and Fluent's 880px drawer. This task adds the missing slots and modes without a client name and without replacing `FluentLayout`.

## Goal (implementer restatement)

A generated app can pin identity and a menu group to the bottom of the shell, and it can choose what the app-bar search does. The default shell stays the command-palette field, the 240px nav, and today's menu order.

`UserCard` is a composite: initials avatar, name, optional detail, and an optional `Href`. With `Href` the root is a link. Without it the root is a div. The shell pins that card under the menu when someone is signed in. The name is the profile alias, or the identity name, or "Account". The detail is the profile email when it is set. The card links to the profile page only when that page's policy allows it. A page can pass its own `UserCard` fragment instead, or set `ShowUserCard` false to omit the card. Signed out, the default card is absent.

`NavPlacement` on `[Page]` is `Top` or `Bottom`. `Bottom` renders that page's `NavGroup` in a second nav with `margin-top: auto`, above the user card. The default is `Top`. One named group uses one placement. The Developer pages use `Bottom`, so that group sits with the external links at the bottom of the menu. Registry order is unchanged because Developer is already last.

`ShellSearchMode` is `Palette` (the read-only Ctrl-K field), `Hidden`, or `Custom` (the `Search` fragment). The default is `Palette`. Ctrl-K stays registered in every mode. The command palette modal stays on the shell.

Widths: full nav at 240px from 1280px up, a 68px icon rail from 880px through 1279px (labels hidden, category title collapsed to the icon), and Fluent's existing drawer below 880px (`MobileBreakdownWidth`). Those numbers live in `tokens.css` and the shell styles together.

The style guide shows a linked card and a static card, and names the search modes and the three widths. Render tests pin the card, the shell slot, the signed-in default, and each search mode. The generator test pins `NavPlacement`. A file test pins the breakpoint numbers.

Acceptance: `./bin/dev build` is clean, `./bin/dev template-smoke` passes, `ganda repo audit` passes, and `git diff origin/master -- source tests` has no case-insensitive client-name match.

## Checklist

- [x] `UserCard` composite and shell slot, including the signed-in default and `ShowUserCard`
- [x] `NavPlacement` on `[Page]`, registry entry, and a pinned nav for `Bottom` groups
- [x] Developer pages use `Bottom`. External links share that pinned nav
- [x] `ShellSearchMode`: Palette, Hidden, Custom
- [x] Rail and drawer breakpoints, tokens, and label hiding
- [x] Style-guide card. Render tests. Generator test. Breakpoint file test
- [x] `./bin/dev build`, `./bin/dev template-smoke`, `ganda repo audit`, no client name in the source or test diff

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Implementation: 2026-10-24 (implementer walk)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-23T23:27:13Z
- Review oracle round 1: general reviewer (claude, Claude Opus 5.5), effort 3, headless (no session id reported), 2026-10-24. Findings: 3 bug, 2 suggestion, 2 nit, all open. A fix pass was requested.

## Notes

- Source idea (read-only): the portal shell pins a user block and a bottom nav group, and it uses three widths (full, icon rail, off-canvas drawer). This template keeps `FluentLayout` and the 880px breakdown. It does not copy that product name, that logo, or a hand-rolled grid.
- Out of scope: sign-in options (379-016) and the StyleGuidePage contract (379-017). The app-bar profile menu stays.

## Results

A generated app can pin the signed-in identity and one nav group to the bottom of the shell, and it can choose what the app-bar search does. The default shell is still the command-palette field, the 240px nav, and the same menu href order.

- `UserCard` is a native composite. With `Href` the root is an anchor. Without it the root is a div. `ShellUserCard` reads the existing profile alias, email, and auth state. It links to `/Profile` only when `ProfilePage.Policy` allows it. Signed out, it renders nothing. A page can pass `UserCard` or set `ShowUserCard` false.
- `NavPlacement` is a generated internal enum (`Top`, `Bottom`) stored on `PageRegistryEntry` as an init property, so assemblies that share `TimeWarp.Architecture` do not collide. `Bottom` puts that group in a second `FluentNav` (`twe-nav--pin`, `margin-top: auto`). Developer pages use `Bottom`. External links share that nav. Menu href order is unchanged because Developer was already last.
- `ShellSearchMode` is `Palette` (default), `Hidden`, or `Custom`. Palette keeps the read-only Ctrl-K field. Hidden omits the box. Custom renders the `Search` fragment inside `.twe-appbar__search` without the palette trigger. The palette modal stays.
- Widths: `--twe-shell-nav-width` 240px from 1280px up, `--twe-shell-rail-width` 68px from 880px through 1279px, Fluent drawer below 880. Media queries repeat 880 and 1279 because a custom property cannot sit in a media condition.
- The style guide shows a linked card and a static card, and names the search modes and the three widths.

The 68px rail was not measured in a browser. First paint ran with an app bar 1264px wide, which is the full nav. The rail numbers are pinned by the stylesheet file test.

### How to validate

**Smoke:**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release -- --filter-class PageSourceGenerator_Tests
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class UserCard_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class TimeWarpPage_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class ShellBreakpoints_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class NavigationDestinations_
cd tests/container-apps/web/web-spa-playwright-tests && dotnet test -c Release -- --filter-class FirstPaint_Given_PrerenderedShell
./bin/dev build
./bin/dev template-smoke
ganda repo audit
git diff origin/master -- source tests | rg -i crunchit
```

**Expect:**

- `PageSourceGenerator_Tests`: 33 passed, 0 failed (1s 133ms).
- `UserCard_Should_`: 5 passed, 0 failed (218ms).
- `TimeWarpPage_Should_`: 13 passed, 0 failed (600ms).
- `ShellBreakpoints_Should_`: 1 passed, 0 failed (186ms).
- `NavigationDestinations_`: 7 passed, 0 failed (477ms). Menu href order:
  - Administrator: `/,/Settings,/Profile,/AgentLinks,/Feedback,/Admin/Roles,/Admin/Principals,/Admin/Feedback,/Admin/Authentication,/Admin/AI,/Counter,/WeatherForecasts,/Superheros,/Chat,/Passkeys,/StyleGuide,/Debugger/Test,/Services,/Developer/UserClaims`
  - PlainUser: `/,/Settings,/Profile,/AgentLinks,/Feedback`
  - Developer: `/,/Counter,/WeatherForecasts,/Superheros,/Chat,/Passkeys,/StyleGuide,/Debugger/Test,/Services,/Developer/UserClaims`
  - Anonymous: `/`
- `FirstPaint_Given_PrerenderedShell`: 2 passed, 0 failed (4s 605ms). Search input stayed at x=382, y=18, width=520, height=32 from prerender through the settled shell. Search width 520.
- `./bin/dev build`: Build succeeded. 0 Warning(s). 0 Error(s). Time Elapsed 00:00:07.54.
- `./bin/dev template-smoke`: Template smoke SUCCEEDED. SmokeDefault OK. Standalone 5/5, 5/5, 2/2. web-jaribu-tests 405/405. api-jaribu-tests 9/9. timewarp-testing-tests 14/14. Generated build 0 Warning(s), 0 Error(s), Time Elapsed 00:00:16.04.
- `ganda repo audit`: Passed 33, Failed 0, Skipped 1. Repository passes all audit checks.
- Client-name scan of `git diff origin/master -- source tests` prints nothing.

### Review

- Round 1 (general, effort 3, 2026-10-24) covered `82ef665fc..cb9a17ddd`. The reviewer re-ran
  `./bin/dev build` (0 warnings, 0 errors) and the five targeted test classes above. All passed
  with the same counts. Template-smoke was not re-run.
- Open: M1 bug (the shell card name stays stale after the profile alias loads), M2 bug
  (NavPlacement is read by syntax name, so a const field silently becomes Top), M3 bug (a tall
  menu overlaps the pinned card), M4 and M5 suggestions (rail accessible names, and a rail
  Playwright check), M6 and M7 nits.
- Disposition: pending. A fix pass was requested, and round 2 follows.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
