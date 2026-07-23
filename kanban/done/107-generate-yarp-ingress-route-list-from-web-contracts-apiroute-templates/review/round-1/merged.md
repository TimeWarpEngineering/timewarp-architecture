# Round 1 — merged (single general reviewer + orchestrator verification)

No blockers. 2 medium, 2 low; behavior deltas traced to real contracts and confirmed safe.

| id | sev | status | disposition |
|----|-----|--------|-------------|
| G1 | medium | fixed | TWA0019: configured-assembly-not-found now build-breaking (silent-empty trap closed); test added (`a5a17557`) |
| G2 | low | fixed | ClientOnlyContract checked on outer AND nested; tests realigned to real contract shape |
| G3 | medium | accepted+follow-up | standalone-yarp LoadFromMemory build-verified only — ship per reviewer verdict (AppHost is the verified public chain); runtime smoke filed as task 120 |
| G4 | low | accepted | casing-dependent output only if a segment appears in two cases (none today; TWA-collapse is case-insensitive deduped) |

Clean: TWA0017/0018 semantics (exact + deeper-foreign shadows; reserved never false-trips api/*),
template guarding + dual-mode attach, TWA0008/0010 hygiene, global-namespace + always-emit,
determinism, /api/Roles 401-not-404 correctness, /api/GetCurrentUser no-server-consumer proof.
