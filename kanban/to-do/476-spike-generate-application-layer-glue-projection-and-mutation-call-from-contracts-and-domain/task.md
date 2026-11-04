# Spike: generate application-layer glue (projection and mutation call) from contracts and domain

## Description

Proposal only, no implementation. After the analyzer (474) proves that Roles (473) and
FeedbackItem (475) agree with their contracts by name, decide whether the hand-written glue in the
handlers can be generated in `web-application`, which sees both sides: the entity-to-response
projection (`new Response(role.Id.Value, role.Name, ...)`) and the command-to-mutation call
(`role.SetPermissions(command.PermissionIds)`). This is the niche AutoMapper filled at runtime,
done at compile time and only where names already agree; where they drift, the analyzer says so
and the mapping stays hand-written.

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

- [ ] Glue inventory for Roles and Feedback handlers
- [ ] Proposed generated shape as code
- [ ] Steve's decision recorded; implementation tasks split if yes

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)

## Notes

Follows 461, 473, 474, 475. Steve 2026-11-05: "after we get the analyzer done, see if we can
generate the glue in the application layer."
