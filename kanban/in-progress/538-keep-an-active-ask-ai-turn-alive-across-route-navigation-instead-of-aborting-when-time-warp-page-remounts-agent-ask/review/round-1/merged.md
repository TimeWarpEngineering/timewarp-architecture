# Round 1 — merged findings
**Date:** 2026-11-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 3 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/agent/ask-agent-session.cs:321
- Description: `Release()` does not stop an in-flight `EnsureAsync`. If the panel is closed
  mid-build, the build resumes and installs a live agent on the session with no panel mounted.
  The old component freed such a build.
- Suggestion: a release epoch checked after each await. Free the new objects when it changed.
- Source: general
- Disposition notes: wontfix (review oracle). The held agent is idle and cancels no turn. It is
  reused on the next open when path, mode, and generation still match, and replaced on a
  generation or readiness change. The circuit scope frees it at the latest. No requirement or
  test depends on a closed panel holding nothing mid-build. Not worth a fix round. Can ride a
  later Ask lifecycle task.

### M2 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:27
- Description: Design comment has a triple space between sentences on one over-long line.
- Suggestion: reflow the paragraph.
- Source: general
- Disposition notes: wontfix (review oracle). Cosmetic, inside a comment. No reader impact.

### M3 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-agent-session-tests.cs:173
- Description: local `wrappedOffPage` holds the unwrapped function the next line asserts is unwrapped.
- Suggestion: rename to `unwrappedOffPage`.
- Source: general
- Disposition notes: wontfix (review oracle). The assertion beside it states the truth. Naming only.

### M4 — Severity: nit — Status: wontfix
- File: kanban/in-progress/538-…/evidence/home.png
- Description: `home.png` and `agent-ask.png` are byte-identical.
- Suggestion: drop the duplicate or replace it with a signed-in post-navigate view.
- Source: general
- Disposition notes: wontfix (review oracle). The change is behavioral, and the integration test
  is its proof. The shot does show the panel renders in the shell.

## Duplicates / conflicts

- None (single reviewer).

## Evidence

Viewed `evidence/agent-ask.png`. Also checked `evidence/home.png`: it is byte-identical to
`agent-ask.png` (`cmp`), so it was not opened separately. That is M4.

`agent-ask.png`, judged as a product page: the home route `/` for a signed-out visitor. The page
title "TimeWarp.Architecture" with the "Home" breadcrumb matches the content (Welcome, Built
with, Account cards). The Ask aside sits in the right column of the `TimeWarpPage` shell, as
`tw-blazor-layout` requires, and the main column and nav are intact. The header reads "Akili ·
New conversation" with new-conversation, expand, and close controls. The body copy "Sign in to
use Akili" with a primary "Sign in" button is the right readiness state for `Unauthenticated`.
Nothing is clipped, overlapping, or mislabeled. The screenshot cannot show the remount behavior
this task changes (a turn surviving navigation). That proof is
`ask-agent-session-tests.cs` `Remount_Mid_Navigate_Should_Commit_The_Turn_And_Rebuild_For_The_New_Role_Page`,
recorded as passing in `## Results`.

## Notes on the record

- Recorded gates cover the change: `./bin/dev build` 0/0, four targeted test files (3 + 9 + 14 + 9
  passing), `ganda repo audit` 44/0. No gates re-run by this review. No targeted command was run.
- The `BL0013` baseline went down (2 → 1) because the suppressed call left the component. That
  removes a suppression and adds none, so it is not check-silencing.
- Goal restatement vs task: consistent. No misread.
