# Round 1 — general
**Date:** 2026-09-12
**Scope reviewed:** branch vs origin/master (commit ec10e254); M30/M32/M33/M34/M36 product files and surrounding CorsPolicy/NoWarn/verify-samples call sites

## Summary

The branch closes M30/M32/M33/M34/M36 as claimed: `verify-samples` honestly treats an empty `samples/` (only `.gitkeep`) as exit 0, fails on a missing directory or failed build, and builds loose `.cs` via `DotNet.Build().WithProject` (validated as a real `dotnet build file.cs` path). NoWarn lists are curated with trailing reasons; `global-suppressions.cs` is gone after real CA1052/CA2000/CA1720 fixes; grpc-server consumes `CorsPolicy.Any.Apply(..., Grpc-*)` like web/api; task 211 exists and is linked from the toggle Design region. Risk is low. `ExamplePolicy` still overrides only the 1-arg `Apply`, so `Example.Apply(services, headers)` still hits the base throw — unused by current call sites and acceptable for this change given the fail-loud Enumeration contract.

## Issues
