# Review framework — 107

- Diff scope: commit `52ff73e2` (generator + TWA0017/0018 + AppHost/yarp consumption + tests).
- Roster/effort: effort 1 — single general reviewer; orchestrator independently re-ran build,
  sourcegen (49), aspire (7/7). No auth/crypto surface (routing carve-outs only; TWA0017 guards
  shadowing).
- Focus: generator correctness (collapse edge cases, attribute matching by simple name,
  cross-assembly scan determinism/incrementality), TWA0017 shadow semantics (false
  negatives?), template-off + package-mode shapes, standalone-yarp LoadFromMemory merge and its
  UNTESTED status (build-verified only — is that acceptable or does it need a follow-up task?),
  Design-region accuracy, behavior-delta safety (/api/Roles auth posture; GetCurrentUser
  consumers).
