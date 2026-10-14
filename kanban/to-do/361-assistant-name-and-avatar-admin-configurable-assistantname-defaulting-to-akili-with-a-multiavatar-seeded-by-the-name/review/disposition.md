# Disposition — task 361

**Date:** 2026-10-14
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Effort 3 (by-diff budget), roster: general. Round 1 raised 10 findings (2 bug, 4 suggestion, 4 nit):
the "Ask" literals left in the settings menu and readiness probe, an agent-driven rename that relabelled
nothing, the instructions capturing a stale name, identity load ordering, avatar regeneration per call,
gaps in the Unicode plain-text rule, validator/aggregate drift, and test robustness. All were fixed in
bc1066749. The rename refresh became a post-pipeline notification handler
(`AgentSurfaceState.AssistantIdentityRefreshHandler`) because TWS0002 forbids dispatching from action
handlers. Round 2 confirmed M1–M10 fixed and raised 3 findings on the fix delta (1 suggestion: astral
format characters / lone surrogates passing the plain-text rule; 2 nits: ZWJ trade-off undocumented,
Design region wrap). All were fixed in d15c3f227, and the orchestrator checked the rune-based rule
directly.

## Exception log (if accepted-exceptions)

None.

## Escalations

- None. Note: during round-1 fixes the web-spa-integration-tests suite's own Aspire.Hosting.Testing
  host (ephemeral postgres) was started once. Its Integration-tagged results were not captured. Unit
  (153/153) and Playwright (15/15, in-proc hosts) passed.
