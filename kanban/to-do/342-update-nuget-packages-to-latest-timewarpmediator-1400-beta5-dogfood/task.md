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

- [ ] `ganda nuget outdated` re-run; Mediator 14.0.0-beta.5 and the State prerelease from 119 visible
- [ ] Bump `Directory.Packages.props` (Mediator x3, State x3, the rest)
- [ ] Build net10.0 + net11.0: no CS errors; TWM005-TWM011 output reviewed and triaged
- [ ] Tests green
- [ ] Breakages from the Mediator bump filed on timewarp-mediator (link issue numbers in Results)
- [ ] Results: before/after versions table, TWM findings, verdict for Mediator 14.0.0 stable readiness
- [ ] `ganda repo audit` clean; PR merged

## Session

- Created: 1014986 (2026-10-13)

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
