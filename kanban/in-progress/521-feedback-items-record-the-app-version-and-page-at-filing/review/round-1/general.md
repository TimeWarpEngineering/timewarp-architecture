# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/521-feedback-items-record-the-app-version-and-page-at` vs `master`, product files only (domain, submit contract and handler, admin contracts and item page, EF mapping and migration, version accessor, SPA page-path picker, tests). Compared with the original task and with `## Goal (implementer restatement)`. Recorded `## Results` and `### How to validate` cover the change (build, the filing runfile, related feedback runfiles, model mapping, page-path tests, agent-token integration, audit rerun). Gates were not re-run.

## Summary

Filing now stores the web-server assembly's informational version, read on the server through `IAppVersionAccessor`, and an optional app-relative page path. The path is fixed at `FeedbackItem.File` with the other filing fields, nullable in the migration, copied onto the admin list item and `FeedbackAdminView`, and shown on the admin item page. Filer contracts are unchanged. The SPA sends the current page, or the newest route before `/Feedback`, with the query and fragment removed, and sends null when the server would refuse the path so the filing still succeeds.

The restatement matches the task, including refusing a non-path instead of stripping it. `RouteState.Routes` is a stack (newest first), which is what `FeedbackPagePath.From` assumes. No issues.

## Issues

<!-- none -->
