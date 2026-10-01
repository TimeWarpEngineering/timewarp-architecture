# Round 1 — general
**Date:** 2026-10-01
**Scope reviewed:** commit 6e2b0e7f (Directory.Packages.props, aspire-app-host csproj/global-usings/program.cs, permission-evaluator-tests.cs, tw-blazor + tw-aggregate-pattern skills)

## Summary

All Aspire pins (SDK, Yarp, PostgreSQL, Testing, EF preview) move together to the 13.6 train; no stray 13.5.4 pins remain. `WithRepl()` is gated on `builder.Environment.IsDevelopment()` inside the existing `#if web` / `#if postgres` nesting, and the new `Microsoft.Extensions.Hosting` global using mirrors that nesting so every flag combination stays IDE0005-clean; the Design comment in program.cs was reconciled. The test gate (TCS with RunContinuationsAsynchronously, released after all checks are issued) removes the timing dependency without risk of hang — `ReleaseGets` is called before `Task.WhenAll`, and the store is used only by that test. Re-verified: AppHost project builds 0/0 (Release); permission-evaluator runfile 19/19 passes. No issues found.

## Issues

<!-- none -->
