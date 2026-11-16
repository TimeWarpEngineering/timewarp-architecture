# Move the EF Core family and Npgsql provider from 11.0.0-rc.3 to 11.0.0 GA when Npgsql.EntityFrameworkCore.PostgreSQL 11.0.0 ships

## Description

Task 492 moved every Microsoft.* pin to 11.0.0 GA except the EF Core family, because
`Npgsql.EntityFrameworkCore.PostgreSQL` was only at `11.0.0-rc.3` on nuget.org and pins
`Microsoft.EntityFrameworkCore` / `.Relational` exactly at `[11.0.0-rc.3.26522.148]`.
Moving EF alone fails restore (NU1608/NU1107). The explanation is the comment above the
EF pins in `Directory.Packages.props`.

This task finishes the move once the GA provider is on nuget.org. It is one commit that
moves all seven pins together and removes the rc.3 comment.

### Pins to move (all seven, same commit)

| Package | From | To |
|---|---|---|
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.EntityFrameworkCore | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.EntityFrameworkCore.Design | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.EntityFrameworkCore.InMemory | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.EntityFrameworkCore.Tools | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 11.0.0-rc.3 | 11.0.0 |

If a later patch (11.0.x) is current by then, use the current version `ganda nuget outdated`
reports. Never backward-pin. Keep the Roslyn-lift comment and the
`Microsoft.CodeAnalysis.CSharp.Workspaces` arrangement below the EF pins unless GA Design
changes its Roslyn dependency; verify with `dev build`.

### Precondition

`Npgsql.EntityFrameworkCore.PostgreSQL` 11.0.0 (or later stable) is listed at
https://api.nuget.org/v3-flatcontainer/npgsql.entityframeworkcore.postgresql/index.json.
If it is not, stop and report; do not start the walk. Tracking issue:
https://github.com/npgsql/efcore.pg/issues/4123 (code merged in npgsql/efcore.pg#4125 on
2026-11-11; release not yet tagged as of 2026-11-16).

## Checklist

- [ ] Precondition met: Npgsql provider 11.0.0 stable on nuget.org
- [ ] All seven pins moved in `Directory.Packages.props`; rc.3 explanatory comment removed (keep one line saying the family moves with the Npgsql provider)
- [ ] `ganda nuget outdated` reports 0
- [ ] `dev build` 0 warnings / 0 errors
- [ ] `dev test`
- [ ] `dev template-smoke --matrix` (postgres flag affects EF; matrix covers flag-off)
- [ ] `dev check-version` passes (bump `<Version>` only if `check-version` requires it; pins-only change needs no release)
- [ ] `ganda repo audit` clean
- [ ] No cross-repo work; stop and report on anything outside this repo

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-16)

## Notes

- Filed per Steve 2026-11-16 ("yes file it") after 492 left the EF family on rc.3.
- Pins-only template change; no library packages change, so no release is required
  (see "Release only when platform packages change").
