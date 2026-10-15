# Disposition — task 370

**Date:** 2026-10-15
**Outcome:** clean
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer (effort 2, by-diff budget). One suggestion (M1): semaphore scheduling let
fast projects jump ahead of cap-blocked integration projects, so the documented start order
did not hold. It was fixed on this task with a dispatcher loop in `RunAllAsync` and verified by
`dev build` (0/0) and `dev test --no-build -j 4 --quiet` (23/23, 163s, start order held). No bugs.
Port-base math, build-phase race fix, output buffering, and exit code checked clean.

## Exception log (if accepted-exceptions)

None.

## Escalations

- None.
