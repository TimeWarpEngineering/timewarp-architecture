# Round 3 — general
**Date:** 2026-10-24
**Scope reviewed:** `9ab07f923..f0bb4fd10` (fix pass for round 2: M2 and M8)

## Summary

The generator now reads `NavPlacement` without the generated enum. It strips parentheses and
`checked`, takes any integral constant, follows a cast to its operand and a const field to its
initializer (with a depth limit of 8), and reads an unbound `NavPlacement.Bottom` or a
using-static `Bottom` by name. A foreign `Foo.Bottom` stays Top. The tests no longer declare the
enum. The `Run` harness compiles only the test source, and the enum comes from
`RegisterSourceOutput`, so the tests now match how an app compiles. `shell-rail.ts` returns a
per-menu handle (`matches`, an idempotent `stop`), and `NavMenu` keeps that handle. It stops the
handle when the menu is disposed during the import, and it releases the module reference in
`finally`. The round 2 findings are fixed. This round raised no new findings.

Re-run by this reviewer: `PageSourceGenerator_Tests` 36/36, `ShellBreakpoints_Should_` 1/1,
`NavigationDestinations_` 7/7 (the real Developer pages with `NavPlacement.Bottom`, which the
generator change affects), and `./bin/dev build` with 0 errors. Playwright and template-smoke
were not re-run, since the implementer reported them passing for this pass.

Observations, not raised as findings:

- A using alias (`using P = TimeWarp.Architecture.NavPlacement;` then `P.Bottom`) is not one of
  the accepted shapes, so it stays Top. The Design region says plainly that any other shape
  stays Top with no diagnostic. Round 2 offered a const-walk or a diagnostic as alternatives, and
  the implementer chose the walk.
- The test's bare `NavPlacement = 1` does not compile in an app (CS0266, since only a literal 0
  converts to an enum implicitly). The walk reads it harmlessly.

## Issues

<!-- None. -->
