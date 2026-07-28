# Review framework — task 129

**Date:** 2026-07-28
**Host task:** kanban/in-progress/129-…/
**Diff scope:** commits `001f0ad0` (stage 0 machinery), `2c175b16` (stage 1a analyzer),
`4b1e4fa9` (stage 1b api migration), `30bbdd06` (stage 2 grpc migration) — plus the related
inline fix `c2b3f4a2` (!api test exclude, pre-existing bug found by stage-0's manual check).
**Plan / brief:** task.md (spec + Checkpoint record — ALL maintainer rulings recorded there) +
stage-0-design.md (approved design). Deviations already adjudicated in-flight: the 6a
asymmetric split (ISuperheroService stays -contracts, client-consumed; IHelloService takes
-application) — consumer evidence derived independently twice.
**Effort:** 1 (general, empirical) + orchestrator gate verification
**Reviewer roster:** general
**Session IDs:** orchestrator Claude Fable; design/implementer/reviewer Claude Sonnet subagents.

## Gate status (orchestrator-verified)

Post-stage-2 full battery: `dev build` 0/0 · `dev test` 15 projects 0 failed ·
`dev template-smoke` (runfile path) both matrices OK. Per-stage batteries were also green
(stage-0/1 reports; planted-file guard proofs ran for api features tree AND grpc platform tree).

## Ground rules

- Read-only on product code; write only under `review/round-1/`
- Severity bug|suggestion|nit; zero issues valid
- Empirical spot-check required (repo standard for machinery changes)
- Re-verify falsifiable claims (byte-identical props, Families constants sync, consumer graph,
  namespace choices, template.json non-changes)
