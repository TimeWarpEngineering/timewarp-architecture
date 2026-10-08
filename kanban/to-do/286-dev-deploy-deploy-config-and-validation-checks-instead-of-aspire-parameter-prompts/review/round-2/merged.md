# Round 2 — merged findings
**Date:** 2026-10-08
**Sources:** general (re-review of 20de37319 + round-1 M1–M10)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 5 | 0 |
| nit | 0 | 7 | 0 |

(Includes carried M1–M10, all re-verified as fixed, plus new N1–N2.)

## Issues

| ID | Severity | Status | Summary | Disposition notes |
|----|----------|--------|---------|-------------------|
| M1–M10 | 5 suggestion, 5 nit | fixed | See round-1/merged.md | Re-verified fixed in round-2/general.md |
| N1 | nit | fixed | The AppHost-agreement test missed the `AddParameter(Name, secret: true)`, literal and named-argument forms | Value-less regex also matches `, secret:`; a guard asserts every `AddParameter(` starts with an identifier (rejects literal/named-argument forms) |
| N2 | nit | fixed | The skill said "each probe 20s" and that a user-secrets failure is always reported | Skill now says API/registry checks use 5s and every other probe 20s, and that the failure is reported only when a parameter is missing |

## Duplicates / conflicts

- None.
