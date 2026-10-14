# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** branch vs master (all 27 files)

## Summary

The change lowers every committed host log config to an Information baseline with EF Core SQL at
Warning, keeps Debug to the app's own namespaces in Development, and guards it with Unit facts plus
publish-suite assertions. Risk is low: config-only behaviour changes, deleted
`appsettings.Kubernetes_Docker.json` files have no references, `TimeWarp.Architecture` category
names follow `sourceName` rename into generated apps, and the `TimeWarp.Architecture.Mail`
Information pin matches `DevelopmentEmailSender`'s namespace. The ACA assertion's quoted `'Debug'`
form matches the existing `Publish_Should_NotRunAnyAppAsDevelopmentOrTesting` convention.
Verified claims: no `LogTo` / `EnableSensitiveDataLogging` in `source/`; no remaining
`Kubernetes_Docker` references; publish helpers (`ServiceEnvironment`, `EnvironmentEntries`) exist.

## Issues

### Issue 1 — Severity: nit
- File: tests/container-apps/aspire/aspire-tests/logging-baseline-guard-tests.cs:137
- Description: `Gate_Sensitive_Data_Logging_On_Development` is a file-level text check; any
  `IsDevelopment()` anywhere in the same file satisfies it, even if the
  `EnableSensitiveDataLogging(` call is not inside that branch.
- Suggestion: a Roslyn analyzer would be exact. Today there are zero call sites, and the spec
  asked for the check "if cheap", so the text check is a proportionate tripwire.
- Status: open
