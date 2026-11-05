# Remove unused Riok.Mapperly reference from web-application

## Description

`web-application.csproj` references `Riok.Mapperly`, and `Directory.Packages.props` pins it at
4.3.1. No code uses it: there is no `[Mapper]` anywhere in the repo. Task 476 decided that
projections stay hand-written and that no mapper library or generator is adopted
(`tw-aggregate-pattern`, "Projection and mutation calls stay hand-written"). Remove the reference so
generated apps do not ship an unused package.

## Goal (implementer restatement)

Generated apps should not carry a package nobody uses. Task 476 settled that projections stay
hand-written, and no code has a `[Mapper]`, so `Riok.Mapperly` is dead weight. Drop the package
reference from `web-application.csproj` and its version pin from `Directory.Packages.props`.

Acceptance: `git grep -n -i mapperly -- ':!kanban'` prints nothing; `dev build` is 0 warnings and
0 errors; `dev template-smoke` passes; `ganda repo audit` exits 0.

## Requirements

- Remove `<PackageReference Include="Riok.Mapperly" />` from
  `source/container-apps/web/projects/web-application/web-application.csproj`.
- Remove the `Riok.Mapperly` `<PackageVersion>` line from `Directory.Packages.props` when nothing
  else references it.
- `git grep -n -i mapperly -- ':!kanban'` prints nothing afterwards.

## Checklist

- [x] Reference and version pin removed
- [x] `dev build` is 0 warnings, 0 errors
- [x] `dev template-smoke` passes (template output changes)
- [x] `ganda repo audit` exits 0

## Session

- Created: 1002142 (2026-11-05)
- 2026-11-05 implementer: removed the `Riok.Mapperly` reference and pin; ran build, template-smoke, audit, check-version.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 00a2def6-6f77-44f6-9a86-cbf2560d0ee0, max-turns 500, Done — 2026-11-05T08:08:22Z

## Notes

Filed from task 476 (decision A, analyzer alone). Evidence: 476 `proposal.md` section 5.

## Results

- Removed `<PackageReference Include="Riok.Mapperly" />` from
  `source/container-apps/web/projects/web-application/web-application.csproj`.
- Removed `<PackageVersion Include="Riok.Mapperly" Version="4.3.1" />` from
  `Directory.Packages.props`. Nothing else referenced it.
- `git grep -n -i mapperly -- ':!kanban'` prints nothing (exit 1).
- `dev build`: 0 warnings, 0 errors.
- `dev template-smoke` (default variant): SUCCEEDED, generated Jaribu aggregators 464/464, 9/9, 14/14.
- `ganda repo audit`: exit 0. The `kanban-landed` and `source-file-line-cap` warnings were already
  there and are warning severity by policy; nothing to fix.
- `dev check-version`: source 2.0.0-beta.32 is ahead of NuGet 2.0.0-beta.31; no bump needed.

### How to validate

Smoke: `git grep -n -i mapperly -- ':!kanban'`, then `./bin/dev build` and `./bin/dev template-smoke`.

Expect: the grep prints nothing. The build reports 0 warnings and 0 errors, and template smoke
reports `Template smoke SUCCEEDED`.
