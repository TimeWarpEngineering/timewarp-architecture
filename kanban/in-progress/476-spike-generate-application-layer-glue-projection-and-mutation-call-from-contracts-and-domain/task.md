# Spike: generate application-layer glue (projection and mutation call) from contracts and domain

## Description

Proposal only, no implementation. After the analyzer (474) proves that Roles (473) and
FeedbackItem (475) agree with their contracts by name, decide whether the hand-written glue in the
handlers can be generated in `web-application`, which sees both sides: the entity-to-response
projection (`new Response(role.Id.Value, role.Name, ...)`) and the command-to-mutation call
(`role.SetPermissions(command.PermissionIds)`). This is the niche AutoMapper filled at runtime,
done at compile time and only where names already agree; where they drift, the analyzer says so
and the mapping stays hand-written.

## Goal (implementer restatement)

Decide whether the hand-written glue between contracts and aggregates in `web-application`
handlers should be generated at compile time. The glue is the entity-to-response projection and
the command-to-mutation call. This task writes a proposal and changes no product code.

Acceptance:

- Every Roles handler (7) and every Feedback handler (13) is inventoried. Each one says which glue
  is mechanical and which carries an application rule.
- The proposed generated shape is written as code: what a handler becomes, where the required hooks
  go (extended partial methods with an access modifier, so a missing hook is CS8795), and what stays
  hand-written.
- No option reads validator bodies. Every option uses metadata only.
- Steve's decision (generate, or keep the analyzer alone) is recorded here. Implementation tasks
  are filed only if he says yes. The implementer does not make this decision.

## Requirements

- Inventory the glue in every Roles and Feedback handler after 473 and 475: what is mechanical,
  what carries an application rule (protected core, last admin, status transitions).
- Propose the generated shape as code in the kitchen: what a handler becomes, where required
  hooks go (extended partial methods with an access modifier, so an unwritten hook is CS8795, not
  silent), and what stays hand-written.
- Decide and record: worth generating, or the analyzer alone is enough. Steve decides on the
  proposal; split implementation tasks only if yes.
- No generator reads validator bodies; metadata only.

## Checklist

- [x] Glue inventory for Roles and Feedback handlers (`proposal.md` sections 2–3)
- [x] Proposed generated shape as code (`proposal.md` section 6, options A–D)
- [x] Steve's decision recorded (option A, analyzer alone); no implementation tasks split
- [x] Decision folded into `tw-aggregate-pattern` ("Projection and mutation calls stay hand-written")
- [x] Follow-ups filed and published: 477, 478, 479

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)
- 2026-11-05 implement (ganda task work): read all 20 handlers, `FeedbackContractMap`, the Role
  aggregate, the Roles and Feedback contracts, and the 461, 474, and 475 results. Wrote
  `proposal.md`. Product code is unchanged. The walk is parked as Blocked until Steve decides.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session e70ced08-4ec9-4693-b067-71e605acccba, max-turns 500, Blocked — 2026-11-05T05:58:34Z
- 2026-11-05 resumed implement (ganda task work) after Steve recorded decision A. Added the
  "Projection and mutation calls stay hand-written" section to `skills/tw-aggregate-pattern/SKILL.md`.
  Filed and published follow-ups 477 (remove Riok.Mapperly), 478 (agent-scope order in
  `Role.AgentScopes`), and 479 (shared Roles and attachment projections, named arguments).

## Decision

Steve, 2026-11-05: **Option A, the analyzer alone.** No projection or handler generator now.

Reasoning: only 3 mutation calls across Roles and Feedback are mechanical, all in Roles; mapping by
name has a real leak case (`GetFeedback.Response.Activity` must be the redacted filer thread) that
compiles clean when the opt-out is forgotten; TWA0039–TWA0043 already close the agreement problem.
Revisit option B once the Order aggregate (backlog 118) shows the projection shape a third time.

Fold-in on this task (no implementation tasks split; none needed):

- Record the decision in `tw-aggregate-pattern` (projection stays hand-written, one shared
  projection per aggregate with named arguments; the analyzer proves agreement).
- File follow-ups: remove the unused `Riok.Mapperly` reference from `web-application`; move
  agent-scope ordering into `Role.AgentScopes` so GetRole, GetRoles, and SetRoleAgentScopes stop
  ordering by hand; share one `GetRole`/`GetRoles` projection and one attachment projection
  (named arguments) to close the section 4 gaps.

## Results

- **Decision:** option A, the analyzer alone (Steve, 2026-11-05). No generator, no Mapperly.
- **Skill:** `skills/tw-aggregate-pattern/SKILL.md` gains "Projection and mutation calls stay
  hand-written": one projection per aggregate and response shape in one static method, named
  arguments, name agreement is not the value (the filer-thread redaction case), ordering belongs to
  the root, mutation calls pass the principal and the clock.
- **Follow-ups (to-do, published):** 477 remove the unused `Riok.Mapperly` reference; 478 order agent
  scopes in `Role.AgentScopes` so GetRole, GetRoles, and SetRoleAgentScopes stop reordering; 479
  share one GetRole/GetRoles projection and one attachment projection, with named arguments.
- **Kitchen:** `proposal.md` holds the inventory, the analyzer-gap evidence, options A–D as code,
  the list of what stays hand-written, and the question.
- **Inventory counts:**
  - Roles: 11 projected member assignments, 10 of them in two copies of one projection (GetRole and
    GetRoles). 3 mutation calls are purely mechanical (Create, UpdateDetails, SetPermissions).
  - Feedback: about 40 projected member assignments. There are 0 mechanical mutation calls, because
    every `FeedbackItem` mutation takes the principal and the clock.
- **Findings that shape the decision:**
  - TWA0041 proves a projection *can* be written. It does not check the handler's expression.
    Swapped positional arguments of the same type, and dropped optional constructor arguments,
    compile cleanly today (`GetRole.Response`, `FeedbackAdminView`).
  - `GetFeedback.Response.Activity` agrees by name with `FeedbackItem.Activity`, but the correct
    value is the redacted filer thread. Any projection made by name leaks internal notes unless the
    member opts out. Leaving the opt-out off compiles. Tests catch it today
    (`feedback-admin-tests.cs:263` and `:420`).
  - Two command responses do not match their root by name: `SetRolePermissions.Response.PermissionIds`
    and `SetRoleAgentScopes.Response.Scopes`.
  - `Riok.Mapperly` 4.3.1 is referenced by `web-application` and nothing uses it.
  - `web-application` does not reference the generator assembly (`timewarp-architecture-analyzers`).
  - "Last admin" is in Principals (`set-principal-roles`), not in Roles or Feedback.
- **Gates:** no product code changed, so build, test, and template-smoke do not apply. The skill
  edit is checked with `dev lint-skills`.
  `ganda repo audit` exits 0 (35 passed). It reports 2 advisory warnings that were there before this
  task: `kanban-landed` (live claims on other tasks) and `source-file-line-cap` (EF migration
  Designer and snapshot files, plus one kanban research runfile). Neither one is in this task's diff.

### How to validate

- **Smoke:** open `kanban/in-progress/476-*/proposal.md`. Check it against
  `git grep -n "new Response(\|new RoleDto(\|new Item(" -- source/container-apps/web/features/admin/roles source/container-apps/web/features/feedback`
  and `git grep -n -i mapperly -- ':!kanban'`.
- **Expect:**
  - Every projection site the grep prints is in the inventory tables (sections 2–3).
  - The Mapperly grep shows only `Directory.Packages.props` and `web-application.csproj`, with no `[Mapper]`.
  - Section 8 asks one question, and `## Decision` in task.md records the answer (option A).
  - `skills/tw-aggregate-pattern/SKILL.md` has the "Projection and mutation calls stay hand-written"
    section, and `dev lint-skills` passes.
  - `ganda kanban show 477`, `478`, and `479` print to-do tasks whose specs name the files above.

## Notes

Follows 461, 473, 474, 475. Steve 2026-11-05: "after we get the analyzer done, see if we can
generate the glue in the application layer."
