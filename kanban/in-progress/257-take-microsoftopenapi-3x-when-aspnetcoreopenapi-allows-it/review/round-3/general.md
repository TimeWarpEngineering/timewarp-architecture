# Round 3 — general
**Date:** 2026-11-01
**Scope reviewed:** 52efa0805..de1c67301 (product commit 925b61eb3: `Directory.Packages.props`,
`tests/container-apps/web/web-server-integration-tests/features/identity/fast-endpoint-auth-schemes-tests.cs`)

## Summary

The fix pass changes comments only and addresses round-2 M3 and M4. Compared with the deleted
block at 25c77b924:
- The shared-framework RC3 / SDK `11.0.100-rc.3.26522.148` fact is restored on the Extensions ItemGroup
  comment.
- The EF/Npgsql coupling note (exact pin, NU1608/NU1107) sits directly above the EF family and
  Npgsql pins.
- Every `10.11.0` Extensions monthly pin, and `System.ServiceModel.Primitives` `10.1.0`, has a
  "no 11.x" comment.

None of the new comments recite task-by-task history, which matches the restatement. The
auth-schemes Design region no longer names FastEndpoints 8.3 or ASP.NET Core 10. No code changed,
so a build re-run adds nothing. The fix pass's recorded `./bin/dev build` (0 warnings, 0 errors) and
`ganda repo audit` (exit 0) cover this delta.

The round-2 addendum in `## Goal (implementer restatement)` matches the round-2 findings. I found
no misread.

## Issues

None.
