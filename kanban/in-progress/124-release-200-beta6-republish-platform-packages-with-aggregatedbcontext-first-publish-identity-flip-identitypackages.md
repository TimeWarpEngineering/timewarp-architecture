# Release 2.0.0-beta.6 - republish platform packages with AggregateDbContext, first-publish Identity, flip identityPackages

## Description

**Real greenfield apps are broken today.** All platform packages sit on nuget.org at
2.0.0-beta.5 (tag ef00b464, 2026-07-15) — verified to contain neither GoldenDbContext nor
AggregateDbContext. But template content now requires `TimeWarp.Foundation.Persistence.AggregateDbContext`
(113/121/122), and `foundationPackages=true` is the generate default — so a real
`dotnet new timewarp-architecture` app restores stale packages and fails CS0234.
`dev template-smoke` cannot see this by design (it packs the monorepo into a local feed).
Grounding checks 2026-07-24: nuget.org flatcontainer indexes + strings on the published
Foundation.Infrastructure nupkg; TimeWarp.Identity returns 404 (never published).

Publishing mechanics (verified): release mode of `dev workflow` packs `PackableProjects`
(which already includes `timewarp-identity.csproj`) and pushes via OIDC trusted publishing.
The policy is scoped to repo + workflow.yml, NOT per package ID — so Identity's first publish
needs no credentials or per-ID setup (Steve's correction; memory updated). Release publishes
as long as it builds; tests gated the PR (#288, merged green).

## Checklist

- [x] Bump `<Version>` to 2.0.0-beta.6 (both trees, commit 7cad5fcb, PR 289) (source/Directory.Build.props; check timewarp-templates
      tree version note — both publish at this version)
- [ ] Cut GitHub Release / tag v2.0.0-beta.6 on master (Steve blesses; release event triggers
      OIDC publish, NO test gate)
- [ ] Verify on nuget.org: all 8 platform packages + template at beta.6; Foundation.Infrastructure
      nupkg contains AggregateDbContext; **TimeWarp.Identity exists (first publish)**
- [ ] AFTER publish confirmed: bump template CPM `PackageVersion` pins to 2.0.0-beta.6
      (several lag at beta.2 — Foundation.*, Modules; Identity pin already 2.0.0-beta.5, update)
- [ ] Flip `identityPackages` template.json default false→true; drop/keep vendored-source dual
      mode per AGENTS.md plan (dual-mode MSBuild stays; only the default flips)
- [ ] Prove the actual fix: generate an app OUTSIDE the monorepo against real nuget.org
      (no local feed) and build it — this is the case template-smoke structurally cannot cover
- [ ] `dev build` 0/0 + `dev template-smoke` still green after pin/default changes

## Notes

Origin: "publish residuals" thread 2026-07-24. Sequencing: 122 (Golden→Aggregate rename)
landed BEFORE this on purpose — beta.6 ships the correct names as the first public API.
Blocks nothing else, but until it ships every real greenfield generation is broken while the
monorepo stays green.
