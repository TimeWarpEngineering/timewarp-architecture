# Round 1 — merged findings
**Date:** 2026-11-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: kanban/in-progress/537-…/task.md (`### How to validate` → Gates)
- Description: The template output changed (the page generator and web-spa agent code), but
  `dev template-smoke` is not in the recorded gates.
- Suggestion: Leave it to CI, which runs the default-variant template-smoke on the PR.
- Source: general
- Disposition notes: wontfix (review orchestrator). The PR CI gate runs the default-variant
  template-smoke before the walk can merge. The diff touches no `#if` flag region and no template
  symbol, so the flag-off matrix adds nothing. No product change is needed.

## Duplicates / conflicts

- None. There was one reviewer.

## Evidence

- `evidence/role-page.png` (viewed). Route `/Admin/Roles/New`, signed out. The page shows the
  TimeWarp logo above a centered "Sign in" card with "Sign in with a passkey" and "Don't have an
  account? Create account". The title "Sign in" matches the content, the card is centered, and the
  copy is plain and correct. This is the expected policy gate in front of New Role for an anonymous
  visitor. It agrees with the negative test: an anonymous visitor cannot open the page, and the
  agent does not offer it. The change itself only affects agent tools and adds no visible UI. The
  `[Page]` Description it adds is not rendered, so no signed-in screenshot was needed to judge the
  diff.
