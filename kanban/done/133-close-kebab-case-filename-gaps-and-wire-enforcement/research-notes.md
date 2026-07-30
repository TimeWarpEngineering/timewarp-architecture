# Research notes (task 133)

## Razor exception (confirmed)

- `TimeWarp.SourceGenerators` `FileNameRuleAnalyzer` default exceptions include `*.razor.cs`.
- Ganda epic 102 / task 103: `.razor` must stay PascalCase (Blazor requirement).
- SPA axis-1 grammar intentionally does not apply to `.razor` basenames.

## Analyzer location

- Package: `TimeWarp.SourceGenerators`
- File: `source/timewarp-source-generators/file-name-rule-analyzer.cs` (in that repo)
- Diagnostic: **`TW0001`** (Info, disabled by default) — package prefix **`TW*`**
- Architecture owns **`TWA*`** separately — **no rename needed** (not a real ID collision)
- Origin: timewarp-source-generators task 011
- Prefix SSOT: source-generators task **020** (done, 2026-07-29) — keep `TW*`, docs-only;
  reject opaque `TWG` / churn-only `TWSG`
- ADR: timewarp-flow ADR-0013

## Gaps to close (this monorepo)

1. Wire/enable **`TW0001`** after multi-dot partial support (upstream) if needed
2. Integration-test path kebab migration
3. Docs/skills: document razor exception in TWA
4. Docs/assets optional renames
5. Optional: Ganda `file-naming.md` still says `TWA001` in places — fix to **`TW0001`** there

See `task.md` and `audit-report.md`.
