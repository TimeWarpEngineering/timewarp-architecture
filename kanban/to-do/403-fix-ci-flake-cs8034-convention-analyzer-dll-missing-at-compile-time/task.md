# Fix CI flake CS8034: convention analyzer DLL missing at compile time

## Goal (implementer restatement)

CI solution builds sometimes compile a project while `timewarp-architecture-convention-analyzers.dll` is missing from `bin/Release/net11.0`, so CSC fails with CS8034. The same commit passes on a re-run. Both failures were inside one job's `dev workflow` build (`ci (publish)` on run 39621278348, `ci (playwright)` attempt 1 on run 39660389756), and each log shows that analyzer project writing the same DLL twice before the missing-file error.

Find the steps that build or load that DLL at the same time and record the race. Stop it by building the analyzer once before parallel consumers, or by stopping a second build from writing the same output. No retry loop and no `continue-on-error`. Show the race is gone locally when a stress run is practical; otherwise explain why the change removes it. `dev build` is 0 warnings and 0 errors, `dev template-smoke` passes, and `ganda repo audit` passes. CI on the PR, including a few re-runs of the affected jobs, is the host's later check; this walk records how to validate.

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

- [x] Find which steps of the failing jobs build or consume the analyzer at the same time (job logs plus `dev workflow` source), and record the race
- [x] Fix the cause: build the analyzer once before the parallel consumers, or stop concurrent builds from writing its output (no retry loop, no `continue-on-error`)
- [x] Reproduce or stress locally if possible (e.g. the failing job's commands with high parallelism, several times); otherwise explain why the fix removes the race
- [ ] CI green on the PR, and a few re-runs of the affected jobs stay green
- [x] `dev build` 0 warnings 0 errors; `dev template-smoke` passes

## Results

Both failing jobs died in the `dev workflow` Build step (`clean -> build -> …`), which is one `dotnet build` of `timewarp-architecture.slnx` in Release. The publish log (run 39621278348) and the playwright attempt-1 log (run 39660389756, job 119431617028) each show `timewarp-architecture-convention-analyzers` writing `bin/Release/net11.0/timewarp-architecture-convention-analyzers.dll` twice, then CSC failing with CS8034 on the next project (`foundation-contracts-generators`, then `dev-cli-tests`). Test and publish steps had not started. The jobs do not share a `bin/` across runners.

The second write comes from `timewarp-contracts`, the only multi-targeted project. Its net10.0 inner build used `SetTargetFramework=TargetFramework=net11.0` (task 387) so it could still run the net11.0 convention analyzers. That global property is a different MSBuild project instance from the solution build of the same csproj, and both instances compile to the same output path. Parallel nodes delete the DLL at the start of the second compile while another node’s CSC is loading it.

The net10.0 reference now sets `SkipGetTargetFrameworkProperties` and `GlobalPropertiesToRemove=TargetFramework` instead. The upward TFM check stays skipped, the inner build’s `TargetFramework` global does not flow, and the reference joins the single analyzer build. A forced rebuild of `timewarp-contracts` compiles the analyzer once, and both the net10.0 and net11.0 compilations load `bin/Release/net11.0/timewarp-architecture-convention-analyzers.dll`. Three solution builds with the analyzer `bin/` and `obj/` deleted first, at full parallelism, each produced one analyzer compile and succeeded. No retry and no `continue-on-error`.

PR CI re-runs of `ci (publish)` and `ci (playwright)` are still open. They run after the host opens the PR.

### How to validate

**Smoke**

```bash
rm -rf source/analyzers/timewarp-architecture-convention-analyzers/bin \
       source/analyzers/timewarp-architecture-convention-analyzers/obj
dotnet build timewarp-architecture.slnx -c Release -m -v:n \
  | rg "convention-analyzers ->|error CS8034|Build succeeded|Build FAILED"
```

**Expect:** one line `timewarp-architecture-convention-analyzers -> …/bin/Release/net11.0/timewarp-architecture-convention-analyzers.dll`, then `Build succeeded`. No `CS8034`.

**Automated gate**

```bash
./bin/dev build
./bin/dev template-smoke
ganda repo audit
./bin/dev check-version
```

`dev build`: 0 warnings, 0 errors. `dev template-smoke`: `Template smoke SUCCEEDED` (SmokeDefault). `ganda repo audit`: all checks passed. `dev check-version`: source `2.0.0-beta.24` is one prerelease ahead of NuGet `2.0.0-beta.23` (no version bump).

**Not in scope:** re-running the GitHub `ci (publish)` and `ci (playwright)` jobs. That proof is the PR after this branch is pushed.

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)
- Implementation: 2026-10-22. Traced CS8034 to the net10.0 `SetTargetFramework` on the convention-analyzer reference and stopped that second build.

## Notes

- Origin: Steve 2026-10-22, "yes" to filing after the second occurrence.
