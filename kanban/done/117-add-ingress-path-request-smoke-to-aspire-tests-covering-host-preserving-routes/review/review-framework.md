# Review framework — 117

- Diff scope: the ingress-smoke commit (test file + one Design-region sentence).
- Roster/effort: effort 1 — single general reviewer (small test-only surface; negative proof
  already documented by implementer and re-run by orchestrator).
- Focus: readiness-gate semantics (does the retry ONLY swallow connection-level failures — could
  it ever mask a real regression?), fixture lifetime/disposal, parallel-AppHost safety claims,
  assertion quality (body proofs vs bare 200s), xUnit conventions, Purpose region.
- Rounds: round-1/general.md → merged → disposition.
