# Round 1 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:102
- Description: The AI page fetches site settings only when no snapshot exists, but sign-in has
  already loaded one. The rename form therefore uses the `Version` from sign-in. After any other
  write, every save returns 409, and reopening the page does not fix it. Only a full reload
  recovers. Admin → Authentication always refetches on init, so the old placement did not have
  this problem.
- Suggestion: Dispatch `FetchSiteSettings` on every interactive init, as AuthenticationPage does,
  then `SyncAssistantDraft`. Add a SPA test that seeds a stale snapshot and checks that the draft
  `Version` matches the server after the page opens.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:110
- Description: The page declares `UpdateAssistantName` as an agent action. An agent rename while
  the page is open leaves the form's name and `Version` stale. The next manual save returns 409,
  and the form shows the old name while the top bar shows the new one.
- Suggestion: Re-sync the draft when the stored `Version` advances and the field is unedited, or
  refetch site settings after a 409 from `UpdateAssistantName` so the page recovers by itself.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. M2 shares a root cause with M1 (the page-local draft never refreshes from the server), but
  it is a separate trigger: an in-page agent write rather than a write before the page opened.
