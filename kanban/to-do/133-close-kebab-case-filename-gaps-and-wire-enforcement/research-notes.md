# Research notes (task 133)

## Razor exception (confirmed)

- `TimeWarp.SourceGenerators` `FileNameRuleAnalyzer` default exceptions include `*.razor.cs`.
- Ganda epic 102 / task 103: `.razor` must stay PascalCase (Blazor requirement).
- SPA axis-1 grammar intentionally does not apply to `.razor` basenames.

## Analyzer location

- Package: `TimeWarp.SourceGenerators`
- File: `source/timewarp-source-generators/file-name-rule-analyzer.cs` (in that repo)
- Diagnostic: `TWA001` (Info, disabled by default) — **collides conceptually with Architecture TWA0001**
- Origin: timewarp-source-generators task 011
- ADR: timewarp-flow ADR-0013

## Gaps to close

1. Wire/enable after multi-dot partial support + id rename
2. Integration-test path kebab migration
3. Docs/skills: document razor exception in TWA
4. Docs/assets optional renames

See `task.md` and `audit-report.md`.
