# Round 2 — general
**Date:** 2026-07-27
**Scope reviewed:** post-fix commits (Routes.razor IAssemblyMarker; Directory.Build.targets split include)

## Summary

Re-verified M1/M2 fixes. Routes.razor uses `IAssemblyMarker`; GenerateAssemblyMarker write is separate from IncludeGeneratedAssemblyMarker which always adds Compile when enabled. Full `dev build` 0/0 after fixes. No new issues.

## Issues

No issues.
