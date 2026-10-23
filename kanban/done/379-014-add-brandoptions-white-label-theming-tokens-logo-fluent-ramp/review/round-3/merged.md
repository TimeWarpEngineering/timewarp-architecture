# Round 3 — merged findings
**Date:** 2026-10-24
**Sources:** general (review oracle)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- Disposition notes: Fixed in 7f9e21a90. Confirmed in round 2.

### M2 — Severity: bug — Status: fixed
- Disposition notes: Fixed in 7f9e21a90. Confirmed in round 2.

### M3 — Severity: suggestion — Status: fixed
- Disposition notes: Fixed in 7f9e21a90. Confirmed in round 2.

### M4 — Severity: suggestion — Status: fixed
- Disposition notes: Fixed in 7f9e21a90. Confirmed in round 2.

### M5 — Severity: nit — Status: fixed
- Disposition notes: Fixed in 7f9e21a90. Confirmed in round 2.

### M6 — Severity: nit — Status: fixed
- Disposition notes: Fixed in 7f9e21a90. Confirmed in round 2.

### M7 — Severity: nit — Status: fixed
- Disposition notes: Fixed in 7f9e21a90. Confirmed in round 2.

### M8 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/configuration/brand-stylesheet.cs:106
- Description: `If-None-Match` used strong comparison, so `W/"<hash>"` returned 200.
- Source: general (round 2)
- Disposition notes: Fixed in 5aa2a680e. A listed tag matches after a leading `W/"` prefix loses its `W/`. `*` is matched before that, so `W/*` is not a wildcard. The tests expect 304 for the weak tag and for a list that contains it, and 200 for `W/"other"`. Playwright adds a weak revalidation that expects 304. Re-verified 2026-10-24: `BrandOptions_Should_` 7/7 and `./bin/dev build` with 0 warnings and 0 errors.

## Duplicates / conflicts

- None. Round 3 raised no new findings.
