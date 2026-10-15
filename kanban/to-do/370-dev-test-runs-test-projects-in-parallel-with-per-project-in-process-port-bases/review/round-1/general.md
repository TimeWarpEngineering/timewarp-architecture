# Round 1 — general
**Date:** 2026-10-15
**Scope reviewed:** a13a95cb7 vs master — tools/dev-cli/endpoints/test-command.cs, tools/dev-cli/services/{test-parallelism,dotnet-test-arguments}.cs, dev-cli-tests, HostGraphFactory wording, co-located-jaribu-runfiles reference, ask-surface Playwright fix.

## Summary

`dev test` now runs projects concurrently with one port base per worker, a serial build phase
and `--no-build --no-restore` tests. Port-base math is sound: override bases are validated by
`InProcTestPorts.ResolveBase` (≤ 65535 − PortSpan) so `SlotLimit` is ≥ 1; derived bases walk the
13-slot ring without sharing a port (pinned by tests). The semaphore pair cannot deadlock (group cap
is taken before a worker, released after). The one defect is in scheduling: the stated start order
does not hold once the integration cap is reached.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/endpoints/test-command.cs:190
- Description: `RunAllAsync` starts every project as a task at once; each waits on its group cap,
  then on the worker semaphore. Once the integration cap (3) is full, the 4th+ integration projects
  wait on the cap while every fast project has already queued on `workers`. When an integration
  project finishes it releases its worker first (inner finally) — a queued fast project takes it —
  and only then the cap, so the next integration project joins the *back* of the worker queue behind
  all fast projects. On a 4-core runner running the whole suite the longest projects start last,
  contradicting the Design ("playwright first, then integration, then fast … fast projects fill free
  workers") and the in-code comment ("playwright and integration projects tend to start first").
  CI per-group jobs (366) are unaffected; local whole-suite runs get a longer tail.
- Suggestion: replace the two semaphores + ConcurrentQueue with a single dispatcher loop: while a
  worker (port base) is free, start the first pending project in `Order` whose group cap has room;
  `await Task.WhenAny` on running projects, return the base, repeat. Start order then holds exactly.
- Status: open
