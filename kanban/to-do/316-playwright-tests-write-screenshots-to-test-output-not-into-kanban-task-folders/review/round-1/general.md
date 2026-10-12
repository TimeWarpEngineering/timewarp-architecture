# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** same as framework

## Summary

The change routes every web-spa Playwright screenshot and the captured prerendered HTML through
`PlaywrightScreenshot.PathFor<TTest>`, which writes under `AppContext.BaseDirectory/playwright-screenshots/<class>/`.
All kanban globbing and `artifacts/playwright/<n>` paths are gone (verified by grep across `tests/`,
`tools/`, `.github/`). The CI upload path now points at the test bin. A source-scan guard ships with
the suite. Risk is low; requirements in task.md are met.

Verified: `first-paint` still uses `RepoRoot()` only for source scanning (line 44), not for output;
`prerendered.html` goes through the helper; upload-artifact accepts `**` globs; `bin/` is ignored,
so runs leave `git status` clean.

## Issues

### Issue 1 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/screenshot-path-guard-tests.cs:50
- Description: The guard regex only matches string literals that *start* with `kanban` (`"kanban"`, `"kanban/…"`). A literal like `"../kanban/"` or an interpolated `$"{root}/kanban"` would slip through.
- Suggestion: Optionally widen to any path segment inside a literal; accept as heuristic otherwise.
- Status: open
