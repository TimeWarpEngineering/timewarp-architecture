# Round 2 — general
**Date:** 2026-10-24
**Scope reviewed:** 7f9e21a90 against 29d4585bc, `source/` and `tests/` (the fix delta for M1 through M7).

## Summary

The fix pass covers all seven round-1 findings. The drift test now checks every default declaration against `tokens.css` and counts them against the `BrandTokenOptions` properties. Both appsettings sections bind equal to each other and to `new BrandOptions()`. The home title comes from `ProductName`. `ApplicationState` no longer has `Name` or `Logo`. The style guide shows the normalized seed. Query and fragment are allowed only on absolute logos. `/brand.css` sends `no-cache` and an ETag. One new defect is in the M7 fix: the `If-None-Match` check uses strong comparison and the test pins that behavior. HTTP requires weak comparison there.

Verified: `BrandOptions_Should_` 7/7, `Clone_Should` 4/4, `TimeWarpPage_Should_` 5/5, and `./bin/dev build` with 0 warnings and 0 errors. No code reads the removed `ApplicationState.Name` or `Logo`.

## Prior findings

| ID | Status | Evidence |
|----|--------|----------|
| M1 | fixed | `brand-options-tests.cs:72`: every emitted line must be in `tokens.css`, and the count is 19 tokens plus the logo height |
| M2 | fixed | `brand-options-tests.cs:142`: both files bind through the configuration binder and are compared property by property |
| M3 | fixed | `HomePage.razor:11`: `WelcomeTitle` is built from `ProductName` |
| M4 | fixed | `application-state.cs`: `Name` and `Logo` removed. The debug `Initialize` and the clone test were updated |
| M5 | fixed | `StyleGuidePage.razor:96`: shows `NormalizedSeedColor` |
| M6 | fixed | `brand-options.cs:115-130`: `?` and `#` are allowed only after a successful absolute http(s) parse, and the Design region records why |
| M7 | fixed | `program.cs:572`, `brand-stylesheet.cs:77`: `no-cache`, a strong SHA-256 ETag, and 304 on a match. The comparison defect is M8 |

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/configuration/brand-stylesheet.cs:107
- Description: `Matches` compares `If-None-Match` tokens with strong (ordinal) comparison, so `W/"<hash>"` never matches and the response is a full 200. RFC 9110 §13.1.2 says a recipient MUST use weak comparison for `If-None-Match`, so `W/"x"` matches `"x"`. A reverse proxy that compresses on the fly, such as nginx with gzip, turns a strong ETag into a weak one. Behind such a proxy, every revalidation of `/brand.css` downloads the full sheet, which defeats the purpose of M7. `brand-options-tests.cs:176` (`weak.StatusCode.ShouldBe(200)`) pins the non-compliant behavior, and the Results text describes it as intended.
- Suggestion: Strip a leading `W/` from each listed token before the ordinal compare, and change the test to expect 304 for `"W/" + first.EntityTag`.
- Status: open
