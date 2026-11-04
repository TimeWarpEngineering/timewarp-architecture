# Fix empty OpenAPI summary for contracts whose XML summary sits on the outer class

## Description

Found during task 461. The FastEndpoint generator (`source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-emitter.cs`,
collector `fast-endpoint-collector.cs`) emits an empty `<summary>` and `<remarks>` for
`SetRolePermissions`. It reads the XML `<summary>` on the inner request class, but the Roles
contracts put the summary on the outer static class (`/// <summary>Replace stored permission ids
for a role.</summary>` on `SetRolePermissions`), and it ignores `[Operation(Description = ...)]`.
The generated endpoint therefore has no OpenAPI description.

## Requirements

- Resolution order for the endpoint summary: XML `<summary>` on the request class; else
  `[Operation(Description = ...)]`; else XML `<summary>` on the containing static class; else empty.
  Record the order in the emitter's Design region.
- Cover every contract in `source/container-apps/web/features` and the template-smoke output: no
  generated endpoint with an empty summary where any of the three sources exists.
- Generator test in `tests/analyzers/` for each source and for the precedence.
- Gates: `dev build` 0 warnings, generator tests, `dev template-smoke` (generated apps compile the
  same generator), `ganda repo audit`.

## Checklist

- [ ] Precedence implemented and recorded in the Design region
- [ ] Tests for each source and for precedence
- [ ] No empty summaries in the repo's generated endpoints
- [ ] Gates green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)

## Notes

Follows 461 (objective bug in `rfc/rfc.md`). Independent of 473–476.
