# Enable nullable reference types across the repo

## Description

Enable C# nullable reference types (`<Nullable>enable</Nullable>`) across the entire repository. Currently, nullable is disabled by design choice. This task flips that decision, enabling compiler-enforced null safety and annotating the codebase with nullable reference type annotations (e.g., `string?`, `T?`, `NotNullWhen` attributes, etc.).

## Checklist

- [ ] Audit current nullable settings in `Directory.Build.props` and individual `.csproj` files
- [ ] Enable `<Nullable>enable</Nullable>` at the repository level (e.g., `Directory.Build.props`)
- [ ] Fix compiler warnings in `Common.Contracts` projects (shared DTOs, mixins)
- [ ] Fix compiler warnings in `Common` projects (shared libraries)
- [ ] Fix compiler warnings in `ContainerApps/Web` (Blazor server + client)
- [ ] Fix compiler warnings in `ContainerApps/Api` (FastEndpoints)
- [ ] Fix compiler warnings in `ContainerApps/Grpc` (gRPC service)
- [ ] Fix compiler warnings in `ContainerApps/Yarp` (API gateway)
- [ ] Fix compiler warnings in `ContainerApps/Aspire` (orchestration host)
- [ ] Fix compiler warnings in `Tests/` projects
- [ ] Fix compiler warnings in `Libraries/` projects
- [ ] Fix compiler warnings in `Analyzers/` projects (source generators)
- [ ] Add nullability annotations to public API surfaces (request/response DTOs, handler signatures)
- [ ] Update `CLAUDE.md` to reflect that nullable reference types are now enabled
- [ ] Run full build and ensure zero nullable warnings (`dotnet build /warnaserror`)
- [ ] Run all test suites to verify no regressions

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
