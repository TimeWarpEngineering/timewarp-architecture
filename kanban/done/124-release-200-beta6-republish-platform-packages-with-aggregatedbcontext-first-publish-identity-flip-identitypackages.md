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
- [x] Cut GitHub Release / tag v2.0.0-beta.6 on master (Steve blesses; release event triggers
      OIDC publish, NO test gate)
- [x] Verify on nuget.org: all 8 platform packages + template at beta.6; Foundation.Infrastructure
      nupkg contains AggregateDbContext; **TimeWarp.Identity exists (first publish)**
- [x] AFTER publish confirmed: bump template CPM `PackageVersion` pins to 2.0.0-beta.6
      (several lag at beta.2 — Foundation.*, Modules; Identity pin already 2.0.0-beta.5, update)
- [x] Flip `identityPackages` template.json default false→true; drop/keep vendored-source dual
      mode per AGENTS.md plan (dual-mode MSBuild stays; only the default flips)
- [x] Prove the actual fix: generate an app OUTSIDE the monorepo against real nuget.org
      (no local feed) and build it — this is the case template-smoke structurally cannot cover
- [x] `dev build` 0/0 + `dev template-smoke` still green after pin/default changes

## Notes

Origin: "publish residuals" thread 2026-07-24. Sequencing: 122 (Golden→Aggregate rename)
landed BEFORE this on purpose — beta.6 ships the correct names as the first public API.
Blocks nothing else, but until it ships every real greenfield generation is broken while the
monorepo stays green.

## Results (2026-07-24)

Took TWO releases — the beta.6 template packed before the pin bump (chicken-and-egg the task
sequencing missed), so **v2.0.0-beta.7** is the release that actually fixes real users:

- v2.0.0-beta.6: republished all platform packages WITH AggregateDbContext (verified by nupkg
  strings); **TimeWarp.Identity first publish** — rode the repo-scoped OIDC trusted publishing
  with zero credential work, exactly as Steve said it would. Template pins were stale (beta.2).
- v2.0.0-beta.7: **pins == release version policy** (AGENTS.md updated) — pins bump in the
  same commit as <Version>; packages + template publish together. identityPackages default
  true; shared IPrincipalStore contract suite rehomed to timewarp-testing; Use*Packages
  switches hoisted to root Directory.Build.props; smoke packs/pins/maps Identity locally;
  stale docfx workflow disabled (task 125).
- Marked beta.6 prerelease retroactively (created without the flag); beta.7 created
  --prerelease. check-version gotcha: it reads LOCAL tags — `git fetch --tags` after
  gh-created releases.

Final proof (the real-user path, no local feeds anywhere): published beta.7 template nupkg
from flatcontainer → dotnet new in a clean hive → generated app pins beta.7 → restored
against nuget.org ONLY → **Build succeeded, 0 Warnings, 0 Errors** (after git init; the
27 warnings pre-init were TimeWarp.Build.Tasks git-metadata fallback, environmental).
NuGet website search-index lag is cosmetic — flatcontainer (the restore path) had every
version within minutes.
