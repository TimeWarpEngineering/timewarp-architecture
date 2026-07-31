# Jaribu upstream MTP session hooks (option E, data-gated)

## Description

BACKLOG BY DESIGN (parent 145; kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md §3, review M1): run-scope host sharing (C-share) and its
teardown seam (Jaribu's empty CreateTestSessionAsync/CloseTestSessionAsync MTP methods —
option E) are deferred UNTIL the aggregate wall-clock data from 145-004/005/006 Results shows
per-class C-create boots are an unacceptable CI cost. Do not promote to to-do without that
data attached.

## Definition of Ready (to elaborate before promoting)

- Attach measured aggregator wall-clock from 145-004/005/006
- Design per 029 rigor in timewarp-jaribu (API shape: session hooks + discovery-session
  guard; pairs WITH a C-share teardown contract in timewarp-testing)
- Decide TWA-side C-share semantics (refcount vs session-owned) in the same design
