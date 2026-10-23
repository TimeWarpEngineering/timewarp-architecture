# Round 3 — general
**Date:** 2026-10-24
**Scope reviewed:** commit 5aa2a680e (`fix(web-spa): revalidate /brand.css when If-None-Match is weak`) against 865587ec3 (the round-2 review commit), `source/` and `tests/` only. This is the fix delta for M8.

## Summary

`BrandStylesheet.Matches` now matches `*` first. Then it removes a leading case-sensitive `W/` from a listed token that continues with a quote, and compares the result ordinally to the strong tag. This is RFC 9110 §13.1.2 weak comparison for `If-None-Match`. The response tag stays strong. `W/*` does not become a wildcard. The pure test now expects 304 for the weak tag and for a list that contains it, and 200 for `W/"other"`. The first-paint Playwright test adds a weak revalidation GET that expects 304. The `program.cs` and Design-region comments match the behavior. Risk is low.

Checks re-run: `BrandOptions_Should_` 7/7 (Release) and `./bin/dev build` with 0 warnings and 0 errors. Playwright was not re-run in this round.

## Prior findings

- M8 (suggestion): fixed. `brand-stylesheet.cs:106-124` does weak comparison, and `brand-options-tests.cs:175-189` pins it.

## Issues

None in the source or test delta.

The kitchen `task.md` lost the blank line between the `## Review` list and `## Notes`. That is not a product finding, and the reviewer restored the line during disposition.

No misread: the Goal restatement's new `If-None-Match` paragraph matches the M8 fix and the task Description.
