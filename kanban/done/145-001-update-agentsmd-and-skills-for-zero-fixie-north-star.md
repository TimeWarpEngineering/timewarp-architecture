# Update AGENTS.md and skills for zero-Fixie north star

## Description

State the locked north star (parent 145; decision record
`kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md` §6)
in the docs **before** migrations start, so no agent extends Fixie/xUnit in the meantime.

## Requirements

1. AGENTS.md Stack/testing bullets: replace "host-level `tests/` suites stay Fixie +
   Shouldly" and "migrate last or never" with the decided policy — single-framework Jaribu
   target; existing Fixie suites migrate per epic 145 (suite-shrinking hybrid topology:
   slice-shaped tests co-locate, host-level remainder stays suite-shaped); aspire-tests xUnit
   is a known deviation being removed (145-003); do NOT extend Fixie or xUnit.
2. AGENTS.md: two-lane Aspire statement (in-proc lane for DI-substitution/pipeline;
   closed-box lane for topology/process isolation; fixed ports live in the in-proc lane only).
3. skills/tw-feature-placement: C-create fixture model note in the co-located preamble
   section (per-class SetupOnce creates its OWN graph via HostGraphFactory — 145-002 — and
   CleanUpOnce disposes it; never share hosts via process statics; Testcontainers-postgres
   Lazy is the documented no-dispose exception).
4. Reconcile the adopting-jaribu migration-policy wording wherever else it appears
   (documentation/developer/standards, tw-jaribu pointer note).

## Checklist

- [x] AGENTS.md testing/stack + enforcement wording updated (incl. Definition of Done)
- [x] tw-feature-placement C-create note added
- [x] Standards docs reconciled (test-structure, integration-testing, how-to overview,
      file-naming, tw-web-api-contracts skill + examples)
- [x] ganda repo audit clean; kanban committed
- [x] dev build: docs-only change; not required for markdown — skipped full solution build

## Plan (Phase 2–3)

Docs-only fold-in of locked §6: AGENTS Stack bullets; Aspire two-lane; C-create in
tw-feature-placement; update human docs that still said “we use Fixie”; leave historical
analysis RFCs under skills/*/analysis untouched.

## Session

- Orchestration 2026-07-31: plan + implement + results (docs-only; effort-1 self-check)

## Results

### Summary

Policy docs now state single-framework Jaribu north star, hybrid migration, C-create host
rules, and two-lane Aspire **before** suite migration tasks (145-002+).

### Files

| Path | Change |
|------|--------|
| `AGENTS.md` | Tests north star; do-not-extend Fixie/xUnit; hybrid + epic 145; Aspire two-lane; DoD Jaribu |
| `skills/tw-feature-placement/SKILL.md` | C-create HostGraphFactory / CleanUpOnce / Testcontainers exception |
| `skills/tw-web-api-contracts/SKILL.md` + `references/examples.md` | Prefer co-located Jaribu for contract tests |
| `documentation/test-structure.md` | North star + legacy Fixie naming note |
| `documentation/developer/conceptual/testing/integration-testing.md` | Jaribu + two lanes |
| `documentation/developer/how-to-guides/overview.md` | Testing guides blurb |
| `documentation/developer/standards/file-naming.md` | C-create + epic 145 pointer |

### Verification

- `ganda repo audit` — clean (ran after edits)
- No product code / csproj changes

### Review

Docs-only policy fold-in; no separate `review/` kitchen (no code diff). Content cross-checked
against task 143 §6 locked decision.
