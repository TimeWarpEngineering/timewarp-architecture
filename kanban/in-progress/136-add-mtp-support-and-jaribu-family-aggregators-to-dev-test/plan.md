# Task 136 — Implementation Plan

**Add MTP support and Jaribu family aggregators to `dev test`**

Plan agent: 2026-07-31. Product forks already decided (134 §8 Q2). No residual human questions.

## Context (verified)

| Fact | Evidence |
|------|----------|
| `dev test` globs `tests/**/*.csproj`, serial, `DotNet.Test().WithProject(path).WithConfiguration("Release")` from RepoRoot | `tools/dev-cli/endpoints/test-command.cs` |
| That form fails MTP on .NET 10 | Task 134 M2 |
| Co-located runfiles: web create-role (5), api weather-forecast (2, port 7255, SetupOnce/CleanUpOnce) | `source/container-apps/.../*-tests.cs` |
| CPM: `TimeWarp.Jaribu` 1.0.0-beta.14 only — no TestingPlatform pin yet | `Directory.Packages.props` |
| NuGet: `TimeWarp.Jaribu.TestingPlatform` 1.0.0-beta.14 exists | nuget.org |
| Package props: `IsTestingPlatformApplication`, `OutputType=Exe`, `Features=FileBasedProgram` | jaribu upstream props |
| Root `global.json`: SDK 10.0.301, no test.runner | root |
| Root `test.runner=MTP` is all-or-nothing — breaks Fixie | MS docs |
| Template excludes `tests/container-apps/api/**` when `!api`, web when `!web` | `template.json` |
| CI: `dev workflow` → test; no direct `dotnet test` | workflow.yml |

## Design decisions

### D1 — MTP invoke form

Bare `dotnet test -c Release` with **cwd = project directory**. Keep Fixie as `DotNet.Test().WithProject(csproj).WithWorkingDirectory(RepoRoot)`.

**Detect MTP:** project-local `global.json` contains `Microsoft.Testing.Platform` as test runner.

### D2 — global.json

**Per-aggregator only.** Do not put `test.runner` on root. Each aggregator mirrors root SDK pin (10.0.301) +:

```json
{
  "sdk": { "version": "10.0.301", "rollForward": "latestFeature" },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

### D3 — Package pins

Add `TimeWarp.Jaribu.TestingPlatform` **1.0.0-beta.14** to CPM (keep Jaribu at beta.14).

### D4 — Aggregator paths (not in `.slnx`)

- `tests/container-apps/web/web-jaribu-tests/`
- `tests/container-apps/api/api-jaribu-tests/`

Each: `JARIBU_MULTI`, PackageReference TestingPlatform + Shouldly, Compile+Link globs for that family's `features/**/*-tests.cs` and `platform/**/*-tests.cs`. No local entrypoint (MTP generates Main).

### D5 — ProjectReferences

| Aggregator | Refs |
|------------|------|
| web-jaribu-tests | web-contracts |
| api-jaribu-tests | api-contracts + timewarp-testing |

New runfiles that add `#:project` deps must extend the family aggregator refs.

### D6 — Template

No template.json change — path excludes already cover `tests/container-apps/{api,web}/**`.

### D7 — Template-smoke tier 3

After solution build: assert aggregators exist (flags on); bare `dotnet test -c Release` per aggregator dir; expect web **5**, api **2** succeeded. Serial (port 7255).

### D8 — CI / slnx

No slnx entries. CI workflow semantics unchanged (`dev workflow`).

## Implementation order

0. Baseline `dev test` green; use runfile/self-install for dev-cli after edits.
1. **MTP support in test-command.cs first** (before any aggregator csproj appears).
2. CPM pin TestingPlatform.
3. web aggregator → 5 tests; prove via bare `dotnet test` + `dev test`.
4. api aggregator → 2 tests; full serialized `dev test`.
5. Template-smoke tier 3.
6. AGENTS.md + tw-feature-placement notes; `dev build` 0/0; audit; kanban.

## File list

**Create:** web/api `*-jaribu-tests.csproj` + `global.json` each.

**Modify:** `Directory.Packages.props`, `test-command.cs`, template-smoke harness/command, `AGENTS.md`, `skills/tw-feature-placement/SKILL.md`, task.md.

**Do not change:** co-located runfiles, Fixie projects, root global.json runner, template.json, .slnx, grpc, Aspire tier.

## Risks

- Port 7255 collision → keep serialized dev test; CleanUpOnce.
- SDK pin drift in project-local global.json → mirror root on bumps.
- Missing FileBasedProgram if package props fail → explicit import fallback.
- Stale `./bin/dev` → verify via `dotnet run tools/dev-cli/dev.cs -- test`.

## Residual human questions

None. Escalate only if FileBasedProgram does not flow from NuGet props, or MTP smoke total parsing is unstable.
