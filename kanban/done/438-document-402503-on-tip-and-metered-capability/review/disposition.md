# Disposition — task 438

**Date:** 2026-10-29
**Outcome:** clean
**Rounds:** 5
**Final open count:** 0

## Summary

Round 1 (general, effort 2) raised 2 suggestions and 1 nit: M1 (hard-coded challenge
`resource.url`), M2 (the test hard-coded routes), and M3 (a dead fallback). The fix commit
`0264769f3` resolved all three and round 2 found nothing new. PR #600 then failed `ci (integration)`
twice with 199 failures: the payment test's public stub endpoints were auto-discovered by every
in-process api-server host and registered duplicate routes. Round 3 (general, effort 3) confirmed it
locally and opened M4 (bug) for it, plus M5 (nit, route literals in the info description). Round 4
(general, effort 1) found the product tree unchanged because the implement-fix-2 oracle made no change.
Implement-fix-3 landed `b1a6cb150`: api-server sets `DisableAutoDiscovery = true` beside its explicit
`Assemblies`, mirroring web-server, and `FreeRoutesNever402` is interpolated from `TipRoute` and
`MeteredRoute`. Round 5 (general, effort 1) re-verified the delta: `dev build` 0 warnings, and
`dotnet run tools/dev-cli/dev.cs -- test --group integration` passed 8 of 8 projects with no
`Duplicate routes detected!` in the log. Every raised finding (M1–M5) is fixed and nothing is open.
Final ledger: `review/round-5/merged.md`.

## Exception log (if accepted-exceptions)

None.

## Escalations

None. The round-4 note about the host replaying a journaled implementer result did not recur: the
implement-fix-3 pass ran and produced the product commit.
