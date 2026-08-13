# Review framework — task 196

**Date:** 2026-08-14
**Host task:** kanban/in-progress/196-analyzer-ban-direct-mediatorsend-in-spa-client-code--enforce-generated-actionset-dispatch/
**Diff scope:** commits `84ef1fd5..32b68eda` on branch dev (4 implementation commits: c0e7a0c6 web-spa call-site fixes, 20e95046 TWA0022 analyzer + wiring, aa1d6b88 analyzer tests, 32b68eda docs)
**Plan / brief:** notes/implementation-plan.md — new TWA0022 DiagnosticAnalyzer banning direct mediator `Send` in SPA client code (gated on `build_property.UsingMicrosoftNETSdkBlazorWebAssembly`, razor-generated trees analyzed, other `.g.cs` exempt), seven web-spa call-site fixes via generated ActionSet methods, wrapper deletion, tests, docs.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator = this Claude session; builder = twa0022-builder (Claude subagent); reviewer = spawned Claude subagent round 1

## Gate evidence already verified by orchestrator (do not re-run)

- `dev build` 0 warnings / 0 errors
- Analyzer suite 114/114 passed; web-spa-integration 15 passed / 1 skipped
- Enforcement sanity check: reintroduced `Mediator.Send` in StyleGuidePage.razor → build failed
  with `error TWA0022` at StyleGuidePage.razor(38,11); reverted
- `dev test` full sweep and `dev template-smoke` in progress at framework-writing time

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
