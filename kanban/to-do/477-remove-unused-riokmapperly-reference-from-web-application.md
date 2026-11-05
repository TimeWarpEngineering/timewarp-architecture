# Remove unused Riok.Mapperly reference from web-application

## Description

`web-application.csproj` references `Riok.Mapperly`, and `Directory.Packages.props` pins it at
4.3.1. No code uses it: there is no `[Mapper]` anywhere in the repo. Task 476 decided that
projections stay hand-written and that no mapper library or generator is adopted
(`tw-aggregate-pattern`, "Projection and mutation calls stay hand-written"). Remove the reference so
generated apps do not ship an unused package.

## Requirements

- Remove `<PackageReference Include="Riok.Mapperly" />` from
  `source/container-apps/web/projects/web-application/web-application.csproj`.
- Remove the `Riok.Mapperly` `<PackageVersion>` line from `Directory.Packages.props` when nothing
  else references it.
- `git grep -n -i mapperly -- ':!kanban'` prints nothing afterwards.

## Checklist

- [ ] Reference and version pin removed
- [ ] `dev build` is 0 warnings, 0 errors
- [ ] `dev template-smoke` passes (template output changes)
- [ ] `ganda repo audit` exits 0

## Session

- Created: 1002142 (2026-11-05)

## Notes

Filed from task 476 (decision A, analyzer alone). Evidence: 476 `proposal.md` section 5.
