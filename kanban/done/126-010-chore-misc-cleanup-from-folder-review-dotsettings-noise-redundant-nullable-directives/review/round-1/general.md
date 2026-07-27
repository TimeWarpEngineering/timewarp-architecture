# Round 1 — general
**Date:** 2026-07-27
**Scope reviewed:** DotSettings deletion + #nullable enable strip

## Summary
Both `.DotSettings` files held only obsolete ReSharper NamespaceFoldersToSkip paths — no real conventions. Eleven hand-written file-level `#nullable enable` directives removed; generator emitters, `.g.cs`, and analyzer test fixture strings left intact. Build 0/0 after IDE2000 blank-line fix on program.cs; tests and template-smoke both green. Template no longer ships DotSettings.

## Issues
No issues.
