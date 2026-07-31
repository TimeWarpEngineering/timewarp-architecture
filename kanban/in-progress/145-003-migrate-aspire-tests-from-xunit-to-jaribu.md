# Migrate aspire-tests from xUnit to Jaribu

## Description

Kill the third framework (parent 145; kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md §2). Smallest migration; proves SetupOnce as the
IClassFixture/IAsyncLifetime replacement for the closed-box Aspire lane.

## Requirements

1. ingress-smoke-tests.cs → Jaribu: IngressAppFixture becomes SetupOnce-created,
   CleanUpOnce-disposed DistributedApplication (keep health-gating web→api→ingress AND the
   reachability polling — the DCP proxy race is real); xUnit asserts → Shouldly; keep
   closed-box zero-mock semantics.
2. DELETE integration-test1.cs (template scaffold, one 26s test duplicating coverage — delete,
   don't port; note in Results).
3. csproj: drop xunit/xunit.runner.visualstudio; wire Jaribu MTP like the family aggregators
   (TimeWarp.Jaribu.TestingPlatform, TestingPlatformDotnetTestSupport, project-local
   global.json MIRRORING root sdk pin — the timewarp-jaribu#20 landmine; dev test detects via
   global.json). Remove xunit CPM pins if this was the last consumer (check first).
4. Suite stays suite-shaped under tests/ (topology tests — hybrid policy).

## Checklist

- [ ] 5 ingress tests green under Jaribu via dev test (MTP path) and bare dotnet test
- [ ] integration-test1.cs deleted; xunit references/pins removed if last consumer
- [ ] dev build 0/0; full dev test green; kanban committed
