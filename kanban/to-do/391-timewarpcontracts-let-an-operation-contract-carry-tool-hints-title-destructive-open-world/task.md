# TimeWarp.Contracts: let an Operation contract carry tool hints (title, destructive, open-world)

## Description

A harness decides which tool calls need a permission prompt and which it may approve automatically from
MCP tool annotations. Two of them (`readOnlyHint`, `idempotentHint`) come from the mediator kind. The
others (`title`, `destructiveHint`, `openWorldHint`) cannot be derived, so the contract has to state them.
TimeWarp.Contracts 2.0.0-beta.23 has no place for them: `McpOperationBinding`
(`source/foundation/timewarp-contracts/mcp-operation-binding.cs`) carries `ToolName` only, and
`OperationAttribute` has `Name`, `Description` and `Group`.

timewarp-nuru 508-015 (McpHost schema fidelity, 2026-10-20) shipped a stopgap. Nuru reads
`[System.ComponentModel.DisplayName]` for the title and its own `TimeWarp.Nuru` attributes `[Destructive(bool)]`
and `[OpenWorld(bool)]` from the request class, and `NuruMcpHostOptions.ToolAnnotations` overrides them per
tool. A contract therefore has to reference TimeWarp.Nuru to state these hints. This task gives them a
transport-neutral home.

## Requirements

- A contract-side way to declare the hints on the request class. A generator must be able to read it by
  metadata name. Either:
  - `OperationAttribute` named properties: `Title` (string), plus `Destructive` and `OpenWorld`. These
    two must stay tri-state, because an unset hint is not the same as `false`. Note that attribute
    properties cannot be `bool?`. A generator can tell whether a named argument was set.
  - Or dedicated attributes in TimeWarp.Contracts, matching Nuru's `[Destructive(bool isDestructive = true)]` and
    `[OpenWorld(bool isOpenWorld = true)]`.
- Mirror the hints on `OperationDescriptor` or `McpOperationBinding` (`Title`, `DestructiveHint` bool?,
  `OpenWorldHint` bool?), so an `OperationCatalog` carries them.
- XML docs and docs.

## Checklist

- [ ] Pick the shape (OperationAttribute properties vs dedicated attributes) and record why
- [ ] Contracts types and descriptor/binding members
- [ ] Tests and docs
- [ ] Release a TimeWarp.Contracts prerelease

## Session

- Created: 139624 (2026-10-20) from timewarp-nuru 508-015 (implementer), because McpOperationBinding lacks the hints.

## Notes

- Follow-up in timewarp-nuru after this ships: read the Contracts hints by metadata name, with the
  Nuru attribute winning when both are present. Bump the TimeWarp.Contracts reference, and update
  `documentation/user/features/neutral-contracts.md` (section "Tool hints").
- Related: timewarp-nuru 508-015, Roslynk task 004 (PR #6).
