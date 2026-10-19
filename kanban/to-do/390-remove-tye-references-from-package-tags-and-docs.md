# Remove Tye references from package tags and docs

## Description

Project Tye was the precursor to .NET Aspire and is retired. The `TimeWarp.Architecture` template
package still lists `Tye` in its NuGet tags. Remove every live Tye reference and do boy-scout cleanup
in any doc file touched.

A `git grep -i '\btye\b'` on 2026-10-19 found one live hit:

- `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`
  `<PackageTags>` contains `Tye`

Kanban history under `kanban/done/` is a record and stays as written.

## Checklist

- [ ] Remove `Tye` from `<PackageTags>`; review the rest of the tag list for anything else stale
      (for example `pwa` if the template no longer ships a PWA) and keep tags that describe what ships
- [ ] Re-sweep for Tye in any form (`tye.yaml`, `Microsoft.Tye`, "Project Tye", tag lists, readmes,
      skills, package descriptions, `.template.config`) outside kanban history; remove what is found
- [ ] Boy-scout: in each doc file touched, fix stale or wrong content you notice (skills are public:
      no client names, no past-tense history)
- [ ] `dev build` is 0 warnings, 0 errors
- [ ] `ganda repo audit` is clean

## Session

- Created: 2026-10-19

## Notes

Requested by Steve 2026-10-19: "create a task to do a review for Tye in the docs tags etc... Tye was
the precursor to Aspire years ago. I still see it in the tags on the TimeWarp.Architecture NuGet.
Please remove it. Feel free to do boyscout cleanup when in a doc file also."

The NuGet tags change only when the next version is released. 2.0.0-beta.22 was being cut when this
task was created, so this lands in the release after it; bump `<Version>` and the platform pins
(`dev check-version`) if master is already at the published version.
