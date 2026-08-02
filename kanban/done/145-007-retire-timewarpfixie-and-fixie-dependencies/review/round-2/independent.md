# Round 2 — independent verification (145-007)
**Date:** 2026-08-02
**Reviewer:** independent agent (orchestrator session), fresh worktree + fresh dev-cli

## Verdict: functional claims CONFIRMED (strongest evidence of the epic); doc sweep completed by fold-in

- **Exact per-suite parity: all 11 suites match pre-migration counts exactly, sum 518/518**;
  full dev test 0 failed (every suite in the repo).
- dev build 0/0; audit 23/23; template-smoke ×3 all SUCCEEDED, zero flakiness, generated apps
  zero Fixie; SmokeNoApi exclusions correct each run.
- MTP wiring uniform (18/18 project-local global.json, root clean); [NotTest] gone;
  timewarp-testing clean in all generated apps; Testcontainers exception preserved (ran real,
  39/39); RoslynTestVerifier adapter sane (102 + 59 exact); test-command.cs MTP-only clean;
  TimeWarpTestingConvention deletion (routed from 145-006) executed.

## Issues — all six FIXED by orchestrator fold-in (same day)

1. (bug) AGENTS.md:36 still taught `dotnet fixie` → replaced with truthful MTP invocation
   (cd + dotnet test; csproj-path form unsupported on .NET 10) + selection guidance.
2. (bug) how-to-filter-tests-by-name.md was Fixie-only → rewritten for MTP reality
   (--list-tests / --filter-uid; upstream link).
3. (bug) how-to-filter-tests-by-tags.md was Fixie-only → rewritten ([TestTag] +
   JARIBU_FILTER_TAG standalone; MTP asymmetry documented).
4. (nit) fixie.console removed from .config/dotnet-tools.json.
5. (nit) .vscode/launch.json deleted (8 dead pre-kebab Fixie configs).
6. (nit) "dual Fixie/Jaribu fixtures" comment → "dual InMemory/EF fixtures".

**Empirical findings feeding the docs (orchestrator-verified):** Jaribu MTP host supports ONLY
--filter-uid; JARIBU_FILTER_TAG is honored standalone (0 run with bogus tag) but IGNORED under
MTP (26/26 ran) → upstream filed: timewarp-jaribu#23 (restores the single-test selection DX
lost with `dotnet fixie --tests`). Bonus SDK bug noted: `dotnet test -- --help` FailFast-crashes
the 10.0.302 IPC bridge (ask the built test-host dll directly).

Post-fold-in gates: dev build 0/0; `dotnet fixie` sweep zero live hits; audit 23/23.
