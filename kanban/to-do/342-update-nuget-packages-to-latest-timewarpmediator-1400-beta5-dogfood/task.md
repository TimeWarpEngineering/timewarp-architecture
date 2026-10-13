# Update NuGet packages to latest (TimeWarp.Mediator 14.0.0-beta.5 dogfood)

## Description

TimeWarp.Mediator `14.0.0-beta.5` shipped 2026-10-13 (https://github.com/TimeWarpEngineering/timewarp-mediator/releases/tag/v14.0.0-beta.5).
It carries the 22 repository-review changes (mediator kanban 013, PR #95): net10.0-only runtime and Contracts,
smaller runtime public API, generator handler binding and type-switch fixes, incremental generator pipeline,
TWM005-TWM011 diagnostics replacing generated-code compile errors, trimming/AOT annotations on the reflection
path. Architecture consumes `TimeWarp.Mediator.Contracts`, `.Generators` and `.Analyzers` at `14.0.0-beta.4`
and targets net10.0 + net11.0. This is the dogfood pass the maintainer asked for before Mediator `14.0.0` stable.

Also take every other pin in `Directory.Packages.props` to latest per the repo audit policy. At filing time
`ganda nuget outdated` listed 16 packages (1 major: Microsoft.OpenApi 2.12.2 → 3.x; minors: Playwright, Scalar,
Testcontainers, timewarp-simple-icons; patches: Microsoft.Extensions.*, Roslynator x3, TimeWarp.State x3,
libphonenumber). Mediator beta.5 was pushed but nuget.org had not indexed it yet; re-run `ganda nuget outdated`
when starting.

Filed by Claude (timewarp-mediator cockpit session) at Steven's request.

## Requirements

- All three TimeWarp.Mediator pins at `14.0.0-beta.5`; other pins at latest unless a bump breaks the build
  and the reason is recorded here (Microsoft.OpenApi major: evaluate separately; a hold is acceptable if recorded).
- TimeWarp.State pins move to the State prerelease that carries the same Mediator bump (timewarp-state 119)
  so the solution does not mix Mediator betas across State and Architecture.
- Clean build on both TFMs with zero new TWM diagnostics, or each triaged: fixed here, or filed on
  timewarp-mediator with a repro when the generator is wrong.
- Full test suite green. Any failure caused by the Mediator bump is filed on timewarp-mediator before this
  task closes.
- `ganda repo audit` clean.

## Checklist

- [x] `ganda nuget outdated` re-run; Mediator 14.0.0-beta.5 visible (no State build on beta.5 yet; took State 12.0.0-beta.12 per cockpit note)
- [x] Bump `Directory.Packages.props` (Mediator x3, State x3, the rest; Microsoft.OpenApi held)
- [x] Build: no CS errors; zero TWM diagnostics (solution targets net11.0 only, see Results)
- [x] Tests green
- [x] Breakages from the Mediator bump filed on timewarp-mediator (none; State findings filed on timewarp-state, see Results)
- [x] Results: before/after versions table, TWM findings, verdict for Mediator 14.0.0 stable readiness
- [ ] `ganda repo audit` clean; PR merged

## Session

- Created: 1014986 (2026-10-13)
- Implemented: ganda task work implement oracle (Claude Opus 5.5), 2026-10-13
- Review oracle: Claude Opus 5.5, effort 2, 1 round, accepted-exceptions — 2026-10-13
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-13T09:04:23Z

## Notes

- Upstream: timewarp-state 119 (same Mediator bump). Start this after a State prerelease with that bump
  is on nuget.org, or pin State last if the rest of the bumps should not wait.
- Mediator release notes: documentation/release-notes/release-notes-v14.0.0.md in timewarp-mediator.

## Cockpit note (2026-10-13, per Steve)

Mediator `14.0.0-beta.5` is on nuget.org; **no TimeWarp.State release is built on it yet** (latest
`12.0.0-beta.12` depends on Mediator `14.0.0-beta.4`; state 119 not yet filed/merged). Do **not**
block on state 119:
- Pin all three Mediator packages to `14.0.0-beta.5` directly. Central package management makes that
  direct pin win over State's transitive `beta.4`, so exactly one Mediator version resolves. Verify
  with `dotnet list package --include-transitive` (or `project.assets.json`) that no `beta.4` remains
  anywhere in the graph.
- Move TimeWarp.State to the latest available prerelease (`12.0.0-beta.12` or newer) and record that it
  was built against Mediator beta.4. If State breaks at runtime or compile time against beta.5, that is
  a finding for timewarp-state 119: file it there with a repro, and hold the State pin with the reason
  recorded here.

## Results

### Versions

| Package | Before | After |
|---------|--------|-------|
| TimeWarp.Mediator.Contracts / .Generators / .Analyzers | 14.0.0-beta.4 | 14.0.0-beta.5 |
| TimeWarp.State / .Blazor / .Plus | 12.0.0-beta.10 | 12.0.0-beta.12 (built on Mediator beta.4) |
| FluentValidation / .DependencyInjectionExtensions | 12.1.1 | 12.2.0 |
| libphonenumber-csharp | 9.0.40 | 9.0.41 |
| MessagePack | 3.1.10 | 3.1.12 |
| Testcontainers.PostgreSql | 4.15.0 | 4.16.0 |
| Scalar.AspNetCore | 2.17.11 | 2.18.0 |
| Microsoft.Extensions.AI.OpenAI | 10.10.0 | 10.10.1 |
| Microsoft.Playwright | 1.55.0 | 1.63.0 |
| timewarp-simple-icons | 16.32.0 | 16.35.0 |
| Roslynator.Analyzers / .CodeAnalysis / .Formatting | 5.0.0 | 5.0.1 |
| Microsoft.OpenApi | 2.12.2 | **held at 2.12.2** |

The Microsoft.OpenApi hold is the same one recorded in `Directory.Packages.props`. FastEndpoints.OpenApi 8.3.0 (latest stable) and 8.4.0-beta.24 still depend on Microsoft.AspNetCore.OpenApi 10.0.11, which caps Microsoft.OpenApi below 3.0.

Every `project.assets.json` resolves only `TimeWarp.Mediator.*/14.0.0-beta.5`. The direct central pin overrides State's transitive beta.4, and no beta.4 remains anywhere in the graph.

### TWM findings and Mediator verdict

- I built the Mediator bump alone first, with State held at beta.10 (commit `chore(deps)`): 0 warnings, 0 errors, 0 TWM005-TWM011 diagnostics.
- With State beta.12, the build and the full suite also show 0 TWM diagnostics and no runtime failure attributable to Mediator. State beta.12, compiled against Mediator beta.4, runs on beta.5 without problems.
- Nothing was filed on timewarp-mediator.
- **Verdict: from this consumer, Mediator 14.0.0-beta.5 is ready for 14.0.0 stable.**
- TFMs: `Directory.Build.props` sets `TargetFramework` to net11.0 only. There is no net10.0 TFM to build. Mediator's net10.0-only runtime and Contracts load fine under net11.0.

### TimeWarp.State 12.0.0-beta.10 → beta.12 migration (beta.11 breaking changes)

- Removed Redux DevTools:
  - Deleted `UseReduxDevTools`, `<ReduxDevTools />`, the `ReduxDevToolsInterop` init, the `ReduxDevToolsEnabled` DefineConstants group, the `TimeWarp.Features.ReduxDevTools` usings, the `Hydrate` overrides, and `redux-dev-tools.d.ts`.
  - `BaseComponent` now derives from `TimeWarpStateComponent`.
- Persistence is registered with `AddTimeWarpStatePersistence()`. That applies to the SPA host and to the nine SPA test hosts.
- `ApiHandler` now owns its per-state `SemaphoreSlim`, keyed by store in a `ConditionalWeakTable`, because `IStore.GetSemaphore` was removed.
  - Behavior change: an action that races a `RemoveState` now runs. Before, it was skipped.
- Generated clones (TWSG002):
  - `web-contracts` and `api-contracts` set `ProduceReferenceAssembly=false`.
  - 14 immutable contract DTOs implement `ICloneable` by returning `this`. The generated clone otherwise calls their guarded constructors with default arguments and throws at runtime.
- Tests:
  - `StateTestOptions.Enable()` is called from a module initializer in `SpaSessionFixture`.
  - The clone tests were updated for the nullable `Clone()` return.

### Upstream findings (timewarp-state)

- https://github.com/TimeWarpEngineering/timewarp-state/issues/646: the generated clone calls guarded DTO constructors with default arguments. The build is clean (TWSG002 says nothing) and the action throws at runtime.
- https://github.com/TimeWarpEngineering/timewarp-state/issues/647: local-feed packs poisoned the global NuGet cache under the release version.
  - On this machine `timewarp.state{,.blazor,.telemetry}/12.0.0-beta.12` came from a task-117 `artifacts/packages` folder. The result was `TypeLoadException: PipelineBehaviorRegistry` in 207 of 215 SPA tests.
  - I deleted those three cache entries so restore pulled the nuget.org packages.

### Gates

- `dev build`: 0 warnings, 0 errors (after `dev clean`).
- `dev test`: 22 projects, all passed, 0 failed. web-server-integration-tests has 1 skipped. Counts include web-spa-integration 215, web-spa-playwright 11, web-jaribu 329, and web-server-integration 331.
- `dev template-smoke`: SUCCEEDED.
- `dev check-version`: 2.0.0-beta.20 is ahead of nuget.org 2.0.0-beta.19, so no bump was needed.

### Review disposition

- Rounds: 1. Effort 2 (by-diff); roster: general (review oracle, Claude Opus 5.5).
- Final counts:
  - bug: 1 fixed
  - suggestion: 0
  - nit: 1 fixed, 1 wontfix
  - open: 0
- Disposition: **accepted-exceptions**.
  - M1 (bug), fixed: ApiHandler's semaphore table was a static field on a generic class, so each handler type had its own table and different actions on one state did not serialize. It moved to the non-generic `features/base/api-handler-semaphores.cs`.
  - M2 (nit), fixed: the stale comment.
  - M3 (nit), wontfix: the `Clone() => this` DTOs expose an unmutated `List<T>`. The shims go away with timewarp-state#646.
- Post-fix gates: `dev build` 0 warnings, 0 errors. `dev test` passes all projects. `ganda repo audit` clean.
- Artifacts:
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/disposition.md`

### How to validate

**Smoke**

```bash
grep -E 'TimeWarp\.(Mediator|State)' Directory.Packages.props
find . -name project.assets.json -path '*obj*' | xargs grep -ohE '"TimeWarp\.Mediator[A-Za-z.]*/[^"]*"' | sort -u
./bin/dev clean && ./bin/dev build
./bin/dev test
```

**Expect**

- Mediator x3 pinned at `14.0.0-beta.5`; State x3 at `12.0.0-beta.12`.
- Assets list only `14.0.0-beta.5` Mediator entries (no `beta.4`).
- Build prints `0 Warning(s)` / `0 Error(s)` with no TWM diagnostics.
- Every test project reports `passed` with `x0` failures.
- If SPA tests throw `TypeLoadException ... PipelineBehaviorRegistry`, the local NuGet cache is poisoned (timewarp-state #647). Delete `~/.nuget/packages/timewarp.state*/12.0.0-beta.12` and restore again.

