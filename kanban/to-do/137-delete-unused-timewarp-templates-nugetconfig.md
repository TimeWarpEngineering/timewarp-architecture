# Delete unused timewarp-templates NuGet.config

## Description

Delete `timewarp-templates/NuGet.config`. It is dead isolation plumbing: nuget.org-only with
`<clear />`, scoped to the packaging tree.

**Why it is unused:**

- The only project under `timewarp-templates/` (`timewarp-architecture-template.csproj`) has
  **zero** `PackageReference`s — it only packs content (`IncludeBuildOutput=false`). Restore has
  nothing to fetch from any feed.
- The file is **not** packed into the `TimeWarp.Architecture` template nupkg (pack allow-list is
  monorepo `source/`, `tests/`, `msbuild/`, root props, etc.).
- Generated apps do **not** inherit it; smoke/publish gates write their own app-local
  `NuGet.config` (`template-smoke`, `template-publish-smoke`).
- Root monorepo already has no `NuGet.config` on purpose (task 066: wrapper nuget.org-only was
  identical to default behavior).

Safe to remove. If a future template-test harness under this tree needs feed isolation, re-add
then with a real package restore dependency.

## Checklist

- [ ] Delete `timewarp-templates/NuGet.config`
- [ ] Grep for residual references to that path (docs, scripts, CI) and clean if any
- [ ] Spot-check pack still works: `dotnet pack` on
      `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`
      (or the existing pack path used by `dev`/workflow) succeeds
- [ ] Commit

## Notes

- Same class of leftover as `timewarp-templates/Directory.Packages.props` (CPM disable kept as
  headroom for a removed test harness) — this file has even less ongoing value because nothing
  restores packages under the tree today.
- Related history: task 066 (root intentionally has no NuGet.config); rename task 064 moved the
  tree to kebab-case without changing this file's role.

## Session

- Created: 2026-07-29 — filed after Q&A on purpose of the file.
