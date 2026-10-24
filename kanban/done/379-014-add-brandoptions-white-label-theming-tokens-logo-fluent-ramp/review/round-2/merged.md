# Round 2 — merged findings
**Date:** 2026-10-24
**Sources:** general (review oracle)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 1 | 2 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/configuration/brand-options-tests.cs:72
- Description: Default tokens restated `tokens.css` with a partial drift test.
- Source: general (round 1)
- Disposition notes: Fixed in 7f9e21a90. Every declaration from `Render(new BrandOptions())` must be in `tokens.css`, and the count must equal the `BrandTokenOptions` property count plus one. Re-verified 2026-10-24: `BrandOptions_Should_` 7/7.

### M2 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/configuration/brand-options-tests.cs:142
- Description: The duplicated `BrandOptions` sections were kept in step only by a comment.
- Source: general (round 1)
- Disposition notes: Fixed in 7f9e21a90. `Bind_The_Checked_In_Sections_To_The_Code_Defaults` binds both files and compares them to each other and to `new BrandOptions()`.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/pages/HomePage.razor:11
- Description: The home card title was fixed text.
- Source: general (round 1)
- Disposition notes: Fixed in 7f9e21a90. The title is `"Welcome to " + ProductName`.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/application-state/application-state.cs:33
- Description: `ApplicationState.Name` and `Logo` were a second copy of the brand.
- Source: general (round 1)
- Disposition notes: Fixed in 7f9e21a90. Both properties are removed, and the debug `Initialize` and the clone test are updated. `Clone_Should` 4/4. No remaining readers.

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:96
- Description: The style guide showed the raw seed.
- Source: general (round 1)
- Disposition notes: Fixed in 7f9e21a90. It shows `NormalizedSeedColor`.

### M6 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/configuration/brand-options.cs:115
- Description: Cache-busted CDN logo URLs were rejected.
- Source: general (round 1)
- Disposition notes: Fixed in 7f9e21a90. A query or fragment is allowed on an absolute http(s) URL and rejected on a relative or root-relative path. Tests cover both cases.

### M7 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-server/program.cs:572
- Description: `/brand.css` had no caching or validation headers.
- Source: general (round 1)
- Disposition notes: Fixed in 7f9e21a90. The response sends `Cache-Control: no-cache` and a strong ETag, and a match is 304. M8 covers the comparison defect.

### M8 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/configuration/brand-stylesheet.cs:107
- Description: `If-None-Match` uses strong comparison, so `W/"<hash>"` returns 200. RFC 9110 §13.1.2 requires weak comparison for `If-None-Match`. A proxy that compresses on the fly (for example, nginx with gzip) weakens the ETag, and then every revalidation downloads the full sheet. `brand-options-tests.cs:176` pins the non-compliant 200.
- Suggestion: Strip a leading `W/` from each listed token before comparing, and change the test to expect 304 for the weak form of the tag.
- Source: general (round 2)
- Disposition notes:

## Duplicates / conflicts

- None. M8 is new in the M7 fix delta.
- No misread: the Goal restatement still matches the task Description. Its additions (the drift check, the two-file bind, ETag caching, the logo query rule) are the round-1 fixes.
