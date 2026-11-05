# Question for Steve: generate SPA handler code, or analyzers alone?

Asked 2026-11-05 by the cockpit session (66368815-cdd5-46be-bde8-a09d94e1cdc8) after the
464 proposal passed three review rounds (PR #646). The full evidence and the options as code are in
`proposal.md`. This file records the question as it was put to Steve, so the decision has a trail.

## What the inventory found

- Roles has 4 server-calling action sets with about 12 mechanical lines between them. Two build
  their request from local draft state, which cannot be generated.
- Across the SPA, 5 of 41 handlers pass the contract request straight through.
- Whole action sets cannot be generated today: TimeWarp.State's dispatch method, the palette
  roster, and page routes come from syntax-only generators on the host compilation. Mediator
  registration is **not** a blocker (it walks marked references). See the 4a correction in
  `proposal.md`, including the Nuru `8027071a` record of the same limitation.
- Hazard: if seeding role drafts from the fetch were an optional hook and nobody wrote it, Save
  would strip every permission from every non-admin role. No test covers that path.
- Forms and validation: nothing to generate. The form binds the contract and reuses its validator.

## The options

- **A** analyzers alone. Four checks, each flagging real drift on master today: contract with
  no SPA surface (GetRole, UpdateRole, DeleteRole); `[Destructive]` agreeing with `ConfirmAsync`
  both ways (two ClearXaiApiKey contracts); `[CatalogAction]` permission and description agreeing
  with the contract (CreateRole); contract with no mock (silent fallback to the real API).
- **B** generate handler bodies into hand-written partial shells, with required extended-partial
  hooks for seeding and request building. Saves about 12 lines in Roles; costs up to 16 empty
  hook bodies SPA-wide.
- **C** the old draft's whole-set generator. Needs a metadata walk in TimeWarp.State's generators
  first (another repository).
- **D** independent of the rest: one result slot per contract call on the base `ApiHandler`,
  replacing 9 hand-written success flags and ids and the RoleForm last-created-id workaround.

## Cockpit recommendation

A, with D as its own small task. Same reasoning as 476: twelve lines is not a generator, the
data-loss hook is the silent-compile failure this family exists to prevent, and all four
analyzers catch real drift now. D is a base-class change, not generation, and removes a known
workaround.

Follow-ups regardless: `[Destructive]` on the two ClearXaiApiKey contracts, CreateRole's catalog
description mismatch, a test for Save with seeded drafts.

## Decision

**Parked (Steve, 2026-11-06).** Steve's response to the question: the proposal priced option C
against TimeWarp.State's generators as they are, and we own that repository. State's ActionSet
generator was hand-written when incremental generators were hard; Nuru and Mediator now carry
much larger pipelines. Re-evaluate State's source generation first, then price C honestly.

Filed: timewarp-state task 140, "Research: re-evaluate TimeWarp.State source generation against
Nuru and Mediator", written without a preferred answer. This task stays in in-progress until 140
reports; then the A / B / C / D question is asked again with 140's result as input.
