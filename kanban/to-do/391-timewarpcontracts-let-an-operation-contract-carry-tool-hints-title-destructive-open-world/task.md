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

## Decision

Title lives on `[Operation]` as a string. Null, empty, and whitespace mean no title, the same blank rule as `Group`. A generator reads the named argument `Title` on `TimeWarp.Contracts.OperationAttribute`.

Destructive and open-world are dedicated attributes, `[Destructive(bool isDestructive = true)]` and `[OpenWorld(bool isOpenWorld = true)]`, matching the Nuru stopgap's constructor. An attribute property cannot be `bool?`. A `bool` property defaults to `false`, so a runtime reader cannot tell an omitted hint from an explicit false, and publishing false for every operation that said nothing would change harness behavior. Attribute absence is the unset state for every reader. `[Destructive]` and `[OpenWorld]` with no argument are true. A generator reads `TimeWarp.Contracts.DestructiveAttribute` and `TimeWarp.Contracts.OpenWorldAttribute` by metadata name: no constructor argument, or any value other than false, is true.

The catalog carries the hints on `OperationDescriptor` (`Title`, `DestructiveHint`, `OpenWorldHint`). They are facts about the operation, declared on the request, and a CLI `--capabilities` projection uses the same facts when MCP is unbound. `McpOperationBinding` stays the tool name the host fills.

## Goal (implementer restatement)

A request class states the three tool hints a mediator kind cannot supply, and an operation catalog carries them without a reference to TimeWarp.Nuru. `readOnlyHint` and `idempotentHint` still come from the kind.

`Title` is optional on `[Operation]`. A blank title is no title. `[Destructive]` and `[OpenWorld]` are tri-state: absent is unset, a bare attribute is true, and `(false)` is an explicit false. `OperationDescriptor` stores `Title` and `DestructiveHint` / `OpenWorldHint` as `bool?`. The HTTP route, the CLI pattern, and the MCP tool name stay unchanged.

The contracts generator and the endpoint emit model read those metadata names. A set hint is copied. An unset hint is omitted. Neither reader folds a hint into a route.

Acceptance:

- The shape above is the contract. XML docs, the package readme, and the neutral-operations skill describe it.
- Jaribu covers a set title, a blank title, both hint attributes (bare, explicit false, absent), catalog JSON that keeps false and omits null, and the generator and emit-model reads.
- `dev build` is 0 warnings and 0 errors. The touched tests pass. `dev check-version` passes. `ganda repo audit` is clean. `dev lint-skills` passes.
- These hints ship in the repo version already one prerelease ahead of NuGet. Publishing stays `dev release` from a clean synced master after merge.

## Checklist

- [x] Pick the shape (OperationAttribute properties vs dedicated attributes) and record why
- [x] Contracts types and descriptor/binding members
- [x] Tests and docs
- [x] Release a TimeWarp.Contracts prerelease

## Session

- Created: 139624 (2026-10-20) from timewarp-nuru 508-015 (implementer), because McpOperationBinding lacks the hints.
- 2026-10-22: `Title` on `[Operation]`. `[Destructive]` and `[OpenWorld]` are tri-state attributes. `OperationDescriptor` carries `Title`, `DestructiveHint`, and `OpenWorldHint`. The contracts generator and the endpoint emit model read them by metadata name and leave the route alone. Source stays `2.0.0-beta.24` (already one prerelease ahead of NuGet `2.0.0-beta.23`). The cut is `dev release` from master after merge.

## Results

`OperationAttribute.Title` is the human-readable name. The setter stores null when the value is null, empty, or whitespace. `[Destructive(bool isDestructive = true)]` and `[OpenWorld(bool isOpenWorld = true)]` are the destructive and open-world hints. Attribute absence is unset. A bare attribute is true. An explicit `false` stays false. `OperationDescriptor` carries `Title`, `DestructiveHint`, and `OpenWorldHint` (`bool?`). JSON catalog output (camelCase, nulls omitted) includes a stated false and omits a null. The CLI pattern and the operation name stay unprefixed.

`McpOperationBinding` stays the host-filled tool name. The hints are operation facts, so a CLI catalog can project them when MCP is unbound.

The contracts generator emits `OperationTitle`, `OperationDestructive`, and `OperationOpenWorld` only when the hint is set. A bare `[Destructive]` or `[OpenWorld]` emits `true`. A whitespace title emits no `OperationTitle`. `RouteTemplate` is unchanged. `EndpointEmitModel` reports the same values and the FastEndpoint template does not read them.

Metadata names: `TimeWarp.Contracts.OperationAttribute` named argument `Title`, `TimeWarp.Contracts.DestructiveAttribute`, and `TimeWarp.Contracts.OpenWorldAttribute`.

Source version stays `2.0.0-beta.24`. That version is already one prerelease increment ahead of published `2.0.0-beta.23`, and `dev check-version` lists `TimeWarp.Contracts` among the packages at the source version. A second bump would skip `2.0.0-beta.24`. Publishing stays `dev release` from a clean synced master after this branch merges (`tw-release`). NuGet does not have this build yet.

A file that imports both `TimeWarp.Contracts` and `TimeWarp.Nuru` must qualify `[Destructive]` and `[OpenWorld]` after the Nuru follow-up bumps the package. The Nuru attribute wins when both are present, as the Notes already say.

### How to validate

Smoke:

1. `./bin/dev build`
2. `cd tests/foundation/foundation-contracts-tests && dotnet test -c Release`
3. `cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release`
4. `./bin/dev check-version`
5. `ganda repo audit`
6. `./bin/dev lint-skills` with `vally` 0.19.0 on `PATH` (`tools/dev-cli/vally-cli.version`)

Expect:

- `./bin/dev build` ends with 0 warnings and 0 errors.
- Foundation contracts tests: 47 passed, 0 failed. Title, both hint attributes, and catalog JSON are covered. A stated false is present. A null hint is absent.
- Source generator tests: 146 passed, 0 failed. A set title and explicit false hints are copied. A bare hint attribute is true. A whitespace title and an omitted hint are omitted. `RouteTemplate` stays `api/Roles`.
- `dev check-version` exits 0. Source is `2.0.0-beta.24`, NuGet is `2.0.0-beta.23`, one prerelease increment ahead, and it reports the version is safe to release. `TimeWarp.Contracts` is in the package list.
- `ganda repo audit` exits 0.
- `dev lint-skills` exits 0. Nine skills pass, including `tw-web-api-contracts`, and its eval spec is valid.

## Notes

- Follow-up in timewarp-nuru after this ships: read the Contracts hints by metadata name, with the
  Nuru attribute winning when both are present. Bump the TimeWarp.Contracts reference, and update
  `documentation/user/features/neutral-contracts.md` (section "Tool hints").
- Related: timewarp-nuru 508-015, Roslynk task 004 (PR #6).
