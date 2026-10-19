# Remove Tye references from package tags and docs

## Goal (implementer restatement)

The `TimeWarp.Architecture` template package still advertises Project Tye, which is retired.
Drop `Tye` from the tags that ship on the nupkg. Review the rest of that tag list and drop any
tag that does not describe a feature the template still ships. Leave kanban history as written.
A doc file is boy-scouted only when this change edits it. `dev build` is 0 warnings and 0 errors,
and `ganda repo audit` is clean. If source `<Version>` is already the published NuGet version,
bump `<Version>` and the platform pins in the same change so the new tags can ship; if the
current version is still ahead of NuGet, leave the version alone so the tag edit rides with
that release.

## Description

Project Tye was the precursor to .NET Aspire and is retired. The `TimeWarp.Architecture` template
package still lists `Tye` in its NuGet tags. Remove every live Tye reference and do boy-scout cleanup
in any doc file touched.

A `git grep -i '\btye\b'` on 2026-10-19 found one live hit:

- `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`
  `<PackageTags>` contains `Tye`

Kanban history under `kanban/done/` is a record and stays as written.

## Checklist

- [x] Remove `Tye` from `<PackageTags>`; review the rest of the tag list for anything else stale
      (for example `pwa` if the template no longer ships a PWA) and keep tags that describe what ships
- [x] Re-sweep for Tye in any form (`tye.yaml`, `Microsoft.Tye`, "Project Tye", tag lists, readmes,
      skills, package descriptions, `.template.config`) outside kanban history; remove what is found
- [x] Boy-scout: in each doc file touched, fix stale or wrong content you notice (skills are public:
      no client names, no past-tense history)
- [x] `dev build` is 0 warnings, 0 errors
- [x] `ganda repo audit` is clean

## Session

- Created: 2026-10-19
- Implementation: 2026-10-19. Dropped `Tye` and the stale `pwa` tag. Bumped `<Version>` and the
  platform pins to `2.0.0-beta.23` because `v2.0.0-beta.22` is already the GitHub release and the
  latest NuGet version.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-19T13:13:18Z
- Review oracle: claude (claude-opus-5-5), headless print mode, session id not exposed. Effort 1, roster
  general, 1 round. Disposition: clean. 2026-10-19.

## Results

The evaluated `PackageTags` for `TimeWarp.Architecture` are
`dotnet-new;templates;aspnet;blazor;spa;timewarp;gRPC;Aspire;YARP;Clean Architecture;Domain Driven Design`.
`Tye` is gone. `pwa` is gone because `web-spa.csproj` leaves the Blazor service-worker items
commented out and no script registers a service worker. The web app manifest stays. The remaining
tags match what the template ships: Blazor SPA, Aspire, and the default-on gRPC and YARP flags,
plus clean architecture and domain-driven design.

`git grep -I` for Tye outside `kanban/` is empty. `kanban/done/` is unchanged. Open task 174 still
names Tye as stale content on the old docfx site; that kitchen stays as that task's record.

No product doc was edited, so the boy-scout pass had no file to correct.

`v2.0.0-beta.22` is published, so `<Version>` in `source/Directory.Build.props` and
`timewarp-templates/Directory.Build.props`, and the eleven platform pins in
`Directory.Packages.props`, are `2.0.0-beta.23`. `TimeWarp.Contracts` takes that `<Version>`.

`timewarp-templates/source/timewarp-architecture-template/Directory.Build.props` still contains a
space-separated `PackageTags` value. The pack uses the csproj property. Evaluated
`PackageTags` is the semicolon list above, and the props string has no Tye.

### How to validate

Smoke:

```bash
git grep -I -n -i -E '\btye\b|tye\.yaml|Microsoft\.Tye' -- ':!kanban'
dotnet msbuild timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj -getProperty:PackageTags,Version -nologo
./bin/dev check-version
./bin/dev build
ganda repo audit
```

Expect:

- The grep prints nothing.
- `PackageTags` is `dotnet-new;templates;aspnet;blazor;spa;timewarp;gRPC;Aspire;YARP;Clean Architecture;Domain Driven Design` and `Version` is `2.0.0-beta.23`.
- `check-version` exits 0 and reports source `2.0.0-beta.23`, NuGet `2.0.0-beta.22`, one prerelease increment ahead, and "Version in source is new — safe to release."
- `dev build` ends with `0 Warning(s)` and `0 Error(s)`.
- `ganda repo audit` reports `Repository passes all audit checks.`

Ran on 2026-10-19. Grep was empty. Property evaluation matched. `check-version` exited 0 with that
report. `dev build` succeeded with 0 warnings and 0 errors. `ganda repo audit` passed 32 and failed 0.

### Review

- Rounds: 1. Effort 1, roster `general`.
- Final counts: bug 0, suggestion 0, nit 0. All are 0 open, 0 fixed, 0 wontfix.
- Disposition: **clean**. No wontfix and no escalation.
- The reviewer re-ran these gates in the claim worktree on 2026-10-19:
  - The Tye grep was empty.
  - `PackageTags` and `Version` evaluated as listed above.
  - `dev check-version` exited 0.
  - `dev build` reported 0 warnings and 0 errors.
  - `ganda repo audit` passed.
- Out-of-scope observations, for a possible follow-up:
  - `timewarp-architecture-template/Directory.Build.props` still sets a dead `PackageTags`, a legacy
    `PackageIconUrl`, and a `PackageReleaseNotes` TODO.
  - `service-worker.published.js` is unused.
- Paths:
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/disposition.md`

## Notes

Requested by Steve 2026-10-19: "create a task to do a review for Tye in the docs tags etc... Tye was
the precursor to Aspire years ago. I still see it in the tags on the TimeWarp.Architecture NuGet.
Please remove it. Feel free to do boyscout cleanup when in a doc file also."

The NuGet tags change only when the next version is released. 2.0.0-beta.22 was being cut when this
task was created, so this lands in the release after it; bump `<Version>` and the platform pins
(`dev check-version`) if master is already at the published version.
