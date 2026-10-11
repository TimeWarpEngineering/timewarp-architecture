# CI: regenerate generated output and fail on any diff

## Description

Generated artifacts drift when a human or agent edits the output (or a non-deterministic generator) without re-running the source of truth. Borrow Anatomy's pattern: CI regenerates the output and fails the build on any difference (`git diff --exit-code`).

**Scope (only these two families):**

1. **Server-offer generation** from tasks 280–282 (`[Offerable]` / offer records emitted by the contracts generator).
2. **YARP ingress route generation** from task 107 (`IngressRoutePrefixGenerator` / web-contracts `[ApiRoute]` prefixes consumed by the AppHost).

Do not expand to every generator in the repo.

## Requirements

1. **Determinism first.** Before wiring CI, make both generators produce byte-stable output across runs on the same inputs:
   - Stable ordering (sorted prefixes / offer names / members — never "order of discovery").
   - No timestamps, random ids, machine names, or other non-reproducible tokens in emitted text.
   - Document the ordering rules in the generator Design regions.
2. **Materialize or snapshot.** Choose a concrete regenerate surface the CI can run (checked-in golden files, a `dev`/`ganda` regenerate command that writes files, or Verify/snapshot files the CI regenerates). Record the choice in Results. The important part is: regenerate → `git diff --exit-code` (or equivalent) fails the job when anything changed.
3. **CI step.** Add a workflow / pipeline step that:
   - Builds / runs the regenerate path for the two families above.
   - Fails on any unstaged or unexpected diff (`git diff --exit-code`, possibly scoped to the generated paths).
4. **Local doc.** One short note in the relevant AGENTS.md / generator docs: how to regenerate locally and what a CI failure means (re-run regenerate and commit, or fix the generator).
5. **Tests.** Unit coverage that the generators emit deterministic, ordered output for a fixed fixture set (repeat-run equality).

**Out of scope:** other analyzers/generators (page, FastEndpoints, TypedId, etc.); a 2× time check-in; Anatomy itself.

## Acceptance criteria

- Both scoped generators are deterministic on a fixed fixture (documented ordering; no timestamps).
- CI regenerates their output and fails on any diff.
- A deliberate one-character change to a golden/generated artifact (or skipping regenerate after a contract change) fails CI; regenerating and committing clears it.
- Docs say how to regenerate locally.
- Build is warning-free; tests green.

## Checklist

- [ ] Audit current offer + YARP generators for non-determinism (ordering, timestamps)
- [ ] Fix determinism; document ordering in Design regions
- [ ] Choose regenerate surface (golden files / regenerate command / snapshots) and record why
- [ ] Add CI step: regenerate + `git diff --exit-code` (scoped)
- [ ] Local regenerate docs
- [ ] Determinism unit tests on fixtures
- [ ] One PR; merge via `ganda pr merge`

## Session

- Created: 785761 (2026-10-11)

## Notes

Source: ideas borrowed from github.com/wheresryan22/anatomy, approved by the Skeptic review on 2026-10-12.

Skeptic verdict (2026-10-12): GREEN for this item. Cheap, standard, catches real drift. Scope to server offers and YARP routes; make generators deterministic first.

Related done work: task 107 (YARP ingress route list from web-contracts), tasks 280–282 (server-offer compile-time agreement + Offerable generation + availability flags).

Filing only. Do not claim-for-work or launch until Steven asks.

## Results

### How to validate
