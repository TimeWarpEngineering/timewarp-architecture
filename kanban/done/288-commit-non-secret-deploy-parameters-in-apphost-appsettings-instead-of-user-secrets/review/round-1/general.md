# Round 1 — general
**Date:** 2026-10-08
**Scope reviewed:** aa529c441 vs master

## Summary
The change is sound. I generated the packed template (kanban excluded) as `Contoso.Shop` and got `contoso-shop` for all three identity parameters in the AppHost appsettings.json, with `registry-endpoint` staying `localhost:5001`. I found no other rewrite of the bare `timewarp-architecture` in the generated tree (the generated readme.md, csproj files and workflow.yml keep it). dev-cli-tests passes (152/152). ReverseProxy was dead in the AppHost: only yarp's own program.cs and appsettings.Development.json read that section, and the AppHost program.cs mentions only the `Yarp.ReverseProxy.Transforms` namespace. Resolution order, source labels, case-insensitive JSON parsing and the missing-parameter text are correct and tested. Two minor items remain.

## Issues

### Issue 1 — Severity: suggestion
- File: .template.config/template.json:140
- Description: The spec asked for a dedicated placeholder token. The implementation instead replaces the bare string `timewarp-architecture`, limited by `onlyIf` `after` matchers keyed to the exact text `"k8s-namespace": "` (and the two sibling keys). It works today, and the smoke asserts catch a breakage loudly. But the rewrite silently depends on the exact JSON spacing of that one file. The `onlyIf` matchers apply to every text file in the template, so a future file containing one of those key strings followed by the monorepo name would also be rewritten.
- Suggestion: Either accept the deviation and record the rationale in the task (the committed value stays a real, working value in the monorepo), or switch to a unique token. A token would make the monorepo's own committed values non-deployable, which is why the current form is defensible.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:248
- Description: The edited comment line "(namespace and release name committed in appsettings.json Parameters, not here — see Design). No dashboard: ..." is now far past the file's wrapped comment width. It is also slightly inaccurate: registry parameters are value-less in code too.
- Suggestion: Re-wrap the comment and say "namespace, release name, registry endpoint and repository".
- Status: open
