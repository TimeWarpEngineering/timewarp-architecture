# Consolidate api-server-integration-tests into two lanes

## Description

Stop paying both boot paths in one assembly (parent 145; kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md §4: 32.69s test / 52.65s wall for
8 tests). Split per the two-lane model. Depends on 145-002 pattern (and 145-003's closed-box
Jaribu shape).

## Requirements

1. In-proc lane classes (handler tests, ApiTestServerApplication) → co-located Jaribu
   runfiles in api features (dedupe against the existing get-weather-forecasts-tests.cs
   exemplar — don't keep two copies of the same coverage; delete the Fixie twin).
2. Closed-box lane classes (endpoint-over-Aspire, open-api-document-tests — keep its Design
   region rationale: process isolation fixes FastEndpoints discovery pollution) → Jaribu
   suite-shaped with SetupOnce-owned DistributedApplication (145-003 pattern).
3. ApiServerTestConvention + Fixie wiring deleted; assembly either becomes the closed-box
   suite only, or dissolves — triage decides, document.
4. Record before/after wall-clock in Results (145-008 gate data).

## Checklist

- [ ] Lane triage documented; duplicated coverage deduped
- [ ] In-proc classes co-located; closed-box classes Jaribu+Aspire
- [ ] Fixie wiring removed; before/after wall-clock recorded
- [ ] dev build 0/0; full dev test; template-smoke; kanban committed
