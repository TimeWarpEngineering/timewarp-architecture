# Round 2 — general
**Date:** 2026-07-26
**Scope reviewed:** post-fix delta (slnx `#if (false)` dual-use, scan roots, Design note)

## Summary

Re-verified M1–M3 against the post-fix tree. Platform projects restored under `<!--#if (false) -->` for monorepo membership with always-strip-on-generate; `source/Directory.Build.props` and `tests/Directory.Build.props` added to pre-scan files; Design notes stale `bin/dev`. `dotnet run tools/dev-cli/dev.cs -- template-smoke` SUCCEEDED (both matrices). No new defects.

## Issues

_(none)_
