# Round 2 — merged findings
**Date:** 2026-11-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: 8 Design/Purpose regions outside web-server `program.cs` (see round-1/merged.md M1)
- Disposition notes: confirmed in round 2. A grep for `Program.ConfigureSettings` /
  `Program.ConfigureAuthentication` is empty, and the new names exist.

### M2 — Severity: suggestion — Status: fixed
- File: web-server-state-dev-tools.cs:6; web-server-authentication.cs:177
- Disposition notes: confirmed in round 2.

### M3 — Severity: suggestion — Status: fixed
- File: aspire-app-host/postgres-resources.cs:107, :130
- Disposition notes: confirmed in round 2. The pointers name publish-target.cs. The other
  "see the Design region" pointers point at reasoning in their own file.

### M4 — Severity: nit — Status: wontfix
- File: aspire-app-host/ingress-resource.cs:126, :316; aca-neon-compute.cs:55
- Disposition notes: rationale accepted by the round-2 reviewer. The combination (api, grpc and web
  all off) is unsupported and ungated: no smoke variant covers it, and the yarp-on form already
  failed on master before this task. Supporting it is a flag-set decision outside this refactor.
  Decided by: review oracle (round 2), on the implementer's evidence in round-1/merged.md.

### M5 — Severity: nit — Status: fixed
- File: web-server-http-clients.cs; web-server-payment-services.cs; brand-stylesheet-endpoint.cs
- Disposition notes: confirmed in round 2.

## New findings

None. The fix delta is comments and whitespace only. `./bin/dev build` passes with 0 warnings and
0 errors.

## Duplicates / conflicts

- None.
