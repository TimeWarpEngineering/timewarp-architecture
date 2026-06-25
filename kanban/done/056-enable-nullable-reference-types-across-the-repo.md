# Enable nullable reference types across the repo

## Description

Enable C# nullable reference types (`<Nullable>enable</Nullable>`) across the entire repository. Currently, nullable is disabled by design choice. This task flips that decision, enabling compiler-enforced null safety and annotating the codebase with nullable reference type annotations (e.g., `string?`, `T?`, `NotNullWhen` attributes, etc.).

## Closure (2026-06-25) — DONE

Mostly accomplished by the root migration, not a separate effort: the root
`Directory.Build.props` was created with `<Nullable>enable</Nullable>` (+ `TreatWarningsAsErrors`),
so every project inheriting it has been nullable-enabled and nullable-clean all along (the full
solution builds green). Verified there were no nullable suppressions hiding it (no `#nullable disable`,
no `CS86xx` in any `NoWarn`).

The **only holdout** was `web-server.csproj`, which had an explicit `<Nullable>disable</Nullable>`.
Its entire nullable debt was a single `CS8618` (`SampleOptions.SampleOption`). Finished it:
- [x] Audit nullable settings — root enables it repo-wide; web-server was the lone opt-out.
- [x] `<Nullable>enable</Nullable>` at repo level — already in root `Directory.Build.props`.
- [x] Remove `<Nullable>disable</Nullable>` from `web-server.csproj`.
- [x] Fix the one warning: `SampleOption` → `= string.Empty;`.
- [x] All other projects (Common/Web/Api/Grpc/Yarp/Aspire/Tests/Libraries/Analyzers) already clean.
- [x] Update `CLAUDE.md` ("Disabled by design choice" → enabled repo-wide).
- [x] web-server builds green under strict settings; full solution already green.

## Notes

- The `CLAUDE.md` currently states "Nullable Reference Types: Disabled by design choice" — this task reverses that decision.
- Consider using `<Nullable>enable</Nullable>` in `Directory.Build.props` so it applies repo-wide, rather than enabling per-project.
- Use `#nullable enable` at the file level only as a transitional measure; prefer project-level enablement.
- Common patterns to apply:
  - `string?` for optional string parameters/return types
  - `T?` for nullable generic type parameters
  - `[NotNullWhen(false)]`, `[MaybeNullWhen]` attributes on bool-returning methods
  - `ArgumentNullException.ThrowIfNull()` for parameter validation
  - Suppress false positives with `!` (null-forgiving operator) only when absolutely certain
- Generated code (source generators) should be excluded via `<GeneratedCode>` attribute or `<Nullable>warnings</Nullable>` pragma — verify the existing `Directory.Build.targets` excludes generated code appropriately.
- This is a large, cross-cutting change. Consider breaking into sub-tasks per project area if the scope is too large for one session.
