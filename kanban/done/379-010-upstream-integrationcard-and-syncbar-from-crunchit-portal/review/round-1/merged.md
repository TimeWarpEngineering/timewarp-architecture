# Round 1 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:49
- Description: `StatusLine`, `KeyBadgeStatus`, and `KeyBadgeLabel` each repeat the three-way key-state
  branches. `StatusLine` also checks `Suffix is null`, so `Configured` with no `Suffix` would show
  "No key stored." beside a "Configured" badge.
- Suggestion: Derive one page-private key state and map the badge, label, and sentence from it.
- Source: general
- Disposition notes: wontfix (orchestrator). The mismatch cannot happen. The server sets `Configured`
  only from a readable row, and that row carries a `Suffix` (`site-xai-api-key-application.cs:118-123`).
  The host-managed status, which has a null `Suffix`, renders on the read-only branch. The three
  getters sit next to each other and the badge pair already reads as one mapping. Leaving the nit
  open would cost a full fix-and-re-review cycle for a page-local style change with no behavior
  impact. A later change to the AI page can fold it in.

## Duplicates / conflicts

- None (single reviewer).
