# Round 2 — general
**Date:** 2026-11-16
**Scope reviewed:** commit `15da1a135` since round-1 base `d57de9b6a64813333955adf0f255f59b6b160721`. Product delta is the FastEndpoints pin comment in `Directory.Packages.props` and the two OpenApi comments in `source/foundation/foundation-server/foundation-server.csproj`. Kitchen updates in the same commit were read for the goal restatement and the fix-pass gate record. Gates were not re-run.

## Summary

The fix pass rewrites the three comments so they state the GA dependency facts from round 1: FastEndpoints.OpenApi 9.0.0 (net11.0) depends on Microsoft.AspNetCore.OpenApi 11.0.0, which depends on Microsoft.OpenApi `[3.11.0, 4.0.0)`, and the pins are 11.0.0 and 3.11.0. The direct package references stay. `3.10.2` and `rc.2` are gone from `Directory.Packages.props`, `Directory.Build.props`, `msbuild/`, and foundation-server. The fix-pass record covers this comment-only change with `./bin/dev build`, `./bin/dev test -q` (23/23), and `ganda repo audit`. No new defects on the delta.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: Directory.Packages.props:158
- Description: Round 1: the rewritten FastEndpoints comment still described the RC3 `Microsoft.OpenApi` range `[3.10.2, 4.0.0)`, and `foundation-server.csproj` still said the OpenApi references lift an rc.2 / 3.10.2 floor. The post-fix comments state the GA nuspec facts, and `Microsoft.OpenApi` is pinned at 3.11.0 (`Directory.Packages.props:165`). `foundation-server.csproj:10` and `:12` match.
- Suggestion: none remaining.
- Status: fixed

## Issues

<!-- No new issues. -->
