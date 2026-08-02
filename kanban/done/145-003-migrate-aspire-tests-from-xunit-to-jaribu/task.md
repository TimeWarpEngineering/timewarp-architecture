# Migrate aspire-tests from xUnit to Jaribu

## Description

Kill the third framework (parent 145; findings §2). Smallest migration; proves SetupOnce as
the IClassFixture/IAsyncLifetime replacement for the closed-box Aspire lane.

## Requirements

1. ingress-smoke-tests → Jaribu SetupOnce/CleanUpOnce DistributedApplication; Shouldly; closed-box.
2. DELETE integration-test1.cs (template scaffold — do not port).
3. csproj: Jaribu MTP + global.json; drop xunit; remove CPM pins if last consumer.
4. Suite stays suite-shaped under tests/.

## Checklist

- [x] Ingress tests green under Jaribu (bare + dev test MTP) — 5 HTTP + 1 prefix unit = 6
- [x] integration-test1.cs deleted; xunit/coverlet/Test.Sdk CPM pins removed (last consumers)
- [x] dev build 0/0; full dev test green; review clean; kanban committed

## Session

- Orchestration 2026-07-31: implement + full dev test + disposition clean

## Results

### Summary

aspire-tests is Jaribu MTP (zero xUnit in the monorepo). SetupOnce owns the AppHost graph with
health-gating + ingress reachability polling preserved. integration-test1 deleted (duplicate
scaffold).

### Changes

| Item | Detail |
|------|--------|
| `ingress-smoke-tests.cs` | Jaribu static class + SetupOnce/CleanUpOnce; Shouldly |
| `GeneratedIngressRoutes_Given_` | Ported unit check (no app) |
| `integration-test1.cs` | **Deleted** |
| `aspire-tests.csproj` | TimeWarp.Jaribu.TestingPlatform + Shouldly; no xunit |
| `global.json` | SDK 10.0.301 + MTP runner |
| `Directory.Packages.props` | Removed xunit, xunit.runner.visualstudio, coverlet.collector, Microsoft.NET.Test.Sdk |
| `AGENTS.md` | aspire-tests noted as Jaribu MTP |

### Verification

| Gate | Result |
|------|--------|
| bare `dotnet test` (project dir) | **6/6** |
| full `dev test` (incl. MTP aspire-tests) | **completed successfully** |
| solution build | **0/0** |
| xUnit remaining | **none** |

### Review

Effort 1, round 1, **clean** — `review/`
