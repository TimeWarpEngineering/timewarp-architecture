# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** commit 13ed481f9

## Summary

The change meets the spec. One committed value (`Parameters:ingress-port`) feeds the AppHost publish
default and `dev open`. A scoped `dotnet new` port generator rewrites it, and template-smoke checks
it. The main issue is shipped text: `tools/dev-cli` and the AppHost ship in every generated app, but
their help text and Design regions still name this repo's port (36371) and the private `ganda` tool.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/endpoints/open-command.cs:38,47 (also the Design regions in program.cs:165, constants.cs:51, deploy-operate.cs:20, open-command.cs:16)
- Description: `tools/dev-cli` is not excluded from the template, so a generated app's `dev open --help` says "36371 in this repo", which is wrong for that app. The `onlyIf` scope rewrites only the appsettings value. program.cs also cites `ganda generate-port`, and ganda is private.
- Suggestion: Name the committed AppHost appsettings.json parameter instead of a number. Drop the ganda reference from shipped comments.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/endpoints/open-command.cs:189
- Description: The compose path validates only `--port`. A value from `INGRESS_PORT` or the `ingress-port` parameter (for example `http`) goes into the URL without a check. The kubernetes path checks it through ChooseForwardPort.
- Suggestion: Run ParsePort on the chosen value with its source as origin.
- Status: open

### Issue 3 — Severity: nit
- File: tools/dev-cli/endpoints/template-smoke-command.cs:543
- Description: With fewer than two recorded generations, it prints "Generated ingress-ports differ per app", which is a claim it did not check.
- Suggestion: Print a separate message saying the per-app check was skipped.
- Status: open
