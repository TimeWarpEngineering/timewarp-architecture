# Contract-agreement analyzer: aggregate satisfies its operation group's contracts

## Description

Resolution of task 461: no entity generator. Instead, a build check (AGENTS.md: "when two things
must agree, generate one from the other or add a build check") that the hand-written aggregate
has what its contracts want. It runs where both sides are visible: `web-application` references
`web-contracts` and `web-domain`; metadata (attributes, members, types) is enough. Contracts stay
UI-first, the domain stays domain-first, the analyzer proves they agree.

## Requirements

- New convention analyzer, next TWA id, in `timewarp-architecture-convention-analyzers`. It must
  load in `web-application` (check that project references the convention analyzers; add if not).
- Linking: an aggregate root is linked to an `[Operation]` group by a small attribute on the
  root, `[SatisfiesOperations("admin.roles")]` (name open), or by convention from the slice
  namespace when unambiguous; record the rule. Profile and Role are the two roots at first.
- Checks, each its own diagnostic with a clear message:
  1. Every member of the group's shared details interface (the interface both create and update
     commands implement) exists on the root with a compatible type.
  2. Every command in the group whose name starts with `Set` and has one list member has a
     mutation `Set<Name>` on the root taking a collection of that element type.
  3. Every query response in the group is projectable: each response member maps to a root
     member by name and compatible type, or the response is marked as computed.
  4. A group with a root but a command that matches none of the above: warning, with
     `[NotAggregate]` on the operation as the opt-out.
- Member name match is exact. Drift is an error, not a silent mapping (this is the point).
- Tests in `tests/analyzers/` with Roles and Profile shapes, including each failure.
- `AnalyzerReleases.Unshipped.md`, and a paragraph in `tw-aggregate-pattern` and
  `tw-web-api-contracts` naming the rule.
- Gates: `dev build` 0 warnings with the analyzer on and Roles (473) and Profile passing, analyzer
  tests, `dev template-smoke`, `ganda repo audit`. Version bump (analyzer package ships).

## Checklist

- [ ] Analyzer with the four checks and tests
- [ ] Loads in web-application; Roles and Profile pass
- [ ] AnalyzerReleases and skills updated
- [ ] Gates green, version bumped

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)

## Notes

Follows 461, after 473. Task 475 brings FeedbackItem under it; task 476 decides whether the
projection and mutation call can then be generated.
