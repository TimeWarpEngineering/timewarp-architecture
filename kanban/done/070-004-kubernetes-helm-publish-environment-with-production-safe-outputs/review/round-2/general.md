# Round 2 — general (re-verification of round-1 fixes)
**Date:** 2026-10-06
**Scope reviewed:** fix delta on program.cs and kubernetes-publish-tests.cs after round 1

## Summary

All five round-1 findings (M1–M5) are fixed in the code. M1, M3 and M4 are comment and Design
region edits that now match the generated chart. For M2, `OptionalMapping` returns null for a
missing section and `Leaves(null, …)` yields nothing; the structural lookups still throw. For M5,
the test asserts on the message, and the throw site uses `PublishTargetConfigurationKey` =
"Publish:Target". After the fixes, `dev build` is 0 warnings / 0 errors and
`KubernetesPublish_Given_` passes 9/9. The fix delta introduced no new issues.

## Issues

<!-- none -->
