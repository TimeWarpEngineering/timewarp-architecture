# Jaribu upstream MTP session hooks (option E) + C-share session fixtures

## Description

PROMOTED (Steve, 2026-08-02): complete Jaribu's fixture lifetime model. The 143 data gate is
met — measured across the epic-145 migrations, every multi-class closed-box suite pays one
full Aspire boot per class (~15-18s each; SPA suite: 6 boots, ~90s of its ~109s wall), and
the cost curve is linear in class count for this template and every app generated from it.
Today Jaribu has NO way to share an expensive fixture across classes; that hole invites the
process-static/undisposed-Lazy bug class the epic just eliminated. We own the framework;
the seam exists (empty CreateTestSessionAsync/CloseTestSessionAsync in
timewarp-jaribu-testing-platform). Standing rule applies: getting it right outranks
move-cost — this is lifetime-model completion, not an optimization.

## Requirements

1. **Upstream (timewarp-jaribu, its own kanban task there):** session-scoped fixture API on
   the MTP seam, designed to task-029 rigor (explicit registration mirroring
   RegisterTests<T> — e.g. RegisterSessionFixture<T> where T : IAsyncDisposable-capable
   factory; NO IClassFixture-style DI, NO field-scanning magic; fail-fast validation;
   discovery-session guard so hooks never fire on --list-tests). Behavior under standalone
   `dotnet run` (no MTP session): fixture factory still resolvable per-class on first use —
   single-file-first stays primary; session scope is an optimization layer, never the
   authoring primitive (143 findings §3 judgment point 3).
2. **Consumer contract (timewarp-testing):** C-share done right — a session-owned shared
   fixture wrapper (create-on-first-use, disposed by the session hook, NOT refcounted, NOT
   process-static) composing with C-create: classes still call the factory; the factory
   consults the session fixture when one is registered, else creates per-class. Zero behavior
   change for suites that don't opt in.
3. **Exemplar:** SPA suite opts in — one DistributedApplication across its classes; measure
   before/after (expect ~109s → ~35-45s); the quarantined weather class must still not
   trigger a boot (preserve the 145-006 skip-aware behavior).
4. **Docs:** tw-feature-placement + AGENTS.md testing section: when to use session fixtures
   (expensive closed-box only) vs per-class C-create (default); the anti-pattern warning
   (never process statics).
5. **Gates:** full dev test; template-smoke ×3; audit; upstream Jaribu suite green; new
   Jaribu version pinned forward here (never backward-pin).

## Checklist

- [ ] Upstream design + implementation task created in timewarp-jaribu (029-rigor), shipped
- [ ] timewarp-testing session-fixture contract composing with C-create
- [ ] SPA suite exemplar opted in; before/after wall-clock recorded
- [ ] Docs updated (skill + AGENTS.md)
- [ ] Gates green; Jaribu pin bumped forward; kanban committed

## Notes

- Data provenance: 145-004 (~24s web, faster than Fixie), 145-005 (~32s api), 145-006
  (~109s SPA, 6 boots structural). Skip-count caveat: timewarp-jaribu#22 (MTP double-counts
  [Skip]) — use true counts in comparisons.
- Related upstream: jaribu#19 (shipped, class hooks), #20 (docs), #22 (skip double-count).
