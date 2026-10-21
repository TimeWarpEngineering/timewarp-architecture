# Fix CI flake CS8034: convention analyzer DLL missing at compile time

## Description

CI build jobs intermittently fail with:

```
CSC : error CS8034: Unable to load Analyzer assembly
.../source/analyzers/timewarp-architecture-convention-analyzers/bin/Release/net11.0/timewarp-architecture-convention-analyzers.dll
: System.IO.FileNotFoundException: Could not find file ...
```

Seen twice, both passing on a re-run with no code change:

- PR #545 (task 393), run 39621278348, job `ci (publish)`
- PR #549 (task 397), run 39660389756, job `ci (playwright)`

Every project gets the analyzer through `source/Directory.Build.props:36-37` (a
`ProjectReference` to the analyzer csproj, unless `UseAnalyzerPackages` is true). A compile that
reads `bin/Release/net11.0/…dll` while another node builds, cleans or overwrites that same output
would produce exactly this. Candidates to check: parallel `dotnet build` / `dotnet test` / `dotnet
run --file` invocations in one job sharing the analyzer's `bin/`, a clean or restore step between
them, runfiles (`#:project`) building the analyzer separately, and `--no-build` / `--no-dependencies`
combinations in `dev workflow`.

## Checklist

- [ ] Find which steps of the failing jobs build or consume the analyzer at the same time (job logs plus `dev workflow` source), and record the race
- [ ] Fix the cause: build the analyzer once before the parallel consumers, or stop concurrent builds from writing its output (no retry loop, no `continue-on-error`)
- [ ] Reproduce or stress locally if possible (e.g. the failing job's commands with high parallelism, several times); otherwise explain why the fix removes the race
- [ ] CI green on the PR, and a few re-runs of the affected jobs stay green
- [ ] `dev build` 0 warnings 0 errors; `dev template-smoke` passes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)

## Notes

- Origin: Steve 2026-10-22, "yes" to filing after the second occurrence.
