# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** same as framework

## Summary

The change reads `__EFMigrationsHistory` through the run's own exec path before and after a quiet psql run
(`-q`, `client_min_messages=warning`), and reports applied / up to date / failure. A missing table (SQLSTATE 42P01
through `VERBOSITY=sqlstate`) counts as a fresh database. `--verbose` keeps the transcript. ACA streams and captures the
bundle's output (`RunAndCaptureAsync` streams to the console and captures, per the Amuru docs) and reports from its
`Applying migration '…'` lines. The wrap-the-tool rule holds: no DO-line parsing. Risk is low. The one gap is in the ACA
failure report.

Verified: no remaining `PsqlCommand` references; the history probe has a timeout (`ProbeTimeout`, 20s); an unreadable
history yields a non-fatal "unknown" line, not a crash; the header, the prompt and the firewall finally are unchanged.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/services/deploy-operate.cs (BuildBundleReport, failure branch)
- Description: On a failed bundle run, the ACA report prints only `Failed while applying <last>`. EF applies each
  migration in its own transaction, so the earlier `Applying migration` ids did commit. The report hides them, while the
  psql path's failure report lists what was applied. Not symmetric with the requirement ("what changed").
- Suggestion: On failure, list `applying[..^1]` as `Applied N migration(s):`, then `Failed while applying <last>`. Add a test.
- Status: open
