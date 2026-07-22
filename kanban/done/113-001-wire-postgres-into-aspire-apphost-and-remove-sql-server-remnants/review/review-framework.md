# Review framework — 113-001

- **Diff scope:** commit `7b99a3d2` (implementation) on branch dev; context commit `00c11d73`
  (NU1903 dep bump, one line, no review needed).
- **Roster / effort:** effort 1 — single general reviewer (default per orchestrate skill; no
  security-sensitive surface: infra wiring + dead-code removal, no auth/crypto/input paths).
- **Focus areas (from build agent's scrutiny notes + plan risks):**
  1. Connection-string precedence + skip-when-unconfigured contract in postgres-db-module.cs —
     correctness of the two-source resolution and the skip path's interaction with health/env
     checks and the hosted service.
  2. AppHost wiring: #if flag nesting correctness (postgres block before web, reference inside
     web), template-off validity (regions strip cleanly), TWA0007/0008/0010 compliance.
  3. Honest health checks — behavior change reviewed for correctness (unhealthy when DB absent
     is intended).
  4. Design region accuracy vs implemented behavior (agent-context-regions rule).
- **Rounds:** review/round-1/general.md → merged.md → evaluate.
- Orchestrator session: this session (main); build agent and plan agent were background
  teammates 2026-07-22.
