# Round 1 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskSendButton.razor:60
- Description: Keyboard-activated Stop unmounts itself, so focus drops to `body`.
- Suggestion: Return focus to the composer after cancel.
- Source: general
- Disposition notes: wontfix on this task (orchestrator). The textarea and send button are
  private to the library `MessageInput` (no ElementReference), so restoring focus needs JS interop
  into library DOM, which the task scope (visual redesign + Stop) does not call for. Mouse and
  Enter-to-send paths are unaffected. Candidate follow-up if keyboard Stop use matters.

## Duplicates / conflicts

- None (single reviewer).
