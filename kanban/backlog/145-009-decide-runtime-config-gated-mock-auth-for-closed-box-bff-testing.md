# Decide runtime-config-gated mock auth for closed-box BFF testing

## Description

BACKLOG: product-code DECISION, optional, not required for zero-Fixie (parent 145; kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md §4).
MockAccessTokenProvider is compile-time (#if MOCK_AUTHENTICATION — web-spa program.cs:56-58),
so Aspire's env/config levers cannot substitute it — which keeps the BFF suite in the in-proc
lane permanently. If Steve wants closed-box BFF tests (real processes through the ingress
with mock auth), the mock must become a runtime-config-gated registration in Program.cs.

## Definition of Ready (to elaborate before promoting)

- Steve's call on whether closed-box BFF testing is wanted at all
- Security posture: how a runtime flag is fail-closed in production (template ships to
  greenfield apps — a leaked mock-auth flag is a real risk; likely Development-environment
  hard-gate + TWA analyzer)
