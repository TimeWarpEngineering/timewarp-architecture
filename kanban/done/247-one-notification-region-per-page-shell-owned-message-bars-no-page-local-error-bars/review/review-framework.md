# Review framework — task 247

**Date:** 2026-09-23
**Host task:** kanban/in-progress/247-one-notification-region-per-page-shell-owned-message-bars-no-page-local-error-bars/
**Diff scope:** branch `task/247-one-notification-region-per-page-shell-owned-messa` vs `origin/master` (merge-base `8a9f4987`) — commits `af367b77`, `16907c74`, `ae61ddd9`, `88e72452`
**Plan / brief:** Design rules 1–8 in `task.md` §Design (confirmed by Steve 2026-09-23) — single shell-owned notification region, one message shape (Intent/Title/Body), dedupe+cap, lifetime (clear-on-nav / auto-dismiss success), spacing via tokens, `ToastNotificationState` → `NotificationState` rename, new TWA0025 analyzer with opt-out attribute.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** cockpit https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
