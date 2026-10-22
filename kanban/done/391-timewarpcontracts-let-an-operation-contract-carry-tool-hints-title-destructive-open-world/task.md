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

A request states the three tool hints a mediator kind cannot supply, and the operation catalog carries them. The contract does not reference TimeWarp.Nuru. `readOnlyHint` and `idempotentHint` still come from the kind.

`Title` is an optional named argument on `[Operation]`. Null, empty, and whitespace mean no title. `[Destructive]` and `[OpenWorld]` are tri-state: the attribute's absence is unset, a bare attribute is true, and `(false)` is an explicit false. `OperationDescriptor` stores `Title` plus `DestructiveHint` and `OpenWorldHint` as `bool?`. The HTTP route, the CLI pattern, and the MCP tool name stay as they are.

Destructive means a delete or an overwrite, as opposed to an update that only adds data. MCP clients treat an omitted `destructiveHint` as true, and they read that hint only when the tool is not read-only. The catalog still stores null for an omitted attribute and does not fill in the client default. A `[Destructive(false)]` fixture names an operation that only appends, such as `notes.append`. A fixture that states destructive true is a `Command` with a non-GET verb, such as a purge on `HttpVerb.Delete`.

Both generator readers copy a set hint and omit an unset one. They also ignore the hint attributes unless `[Operation]` has a name that is not blank. A reflection reader still sees the attributes. The author-facing tool-hint sections say so.

Acceptance:

- XML docs, the package readme, and `neutral-operations.md` describe that shape, the MCP default and not-read-only rule, and the generator's blank-name rule.
- Jaribu covers a set title, a blank title, both hint attributes (bare, explicit true, explicit false, absent), a whitespace operation name together with hint attributes in each reader, and catalog JSON that keeps false and omits null.
- Those fixtures agree with that meaning. An explicit false sits on an additive operation. An explicit true sits on a command with a non-GET verb.
- `dev build` is 0 warnings and 0 errors. The touched tests pass. `dev check-version` passes. `ganda repo audit` is clean. `dev lint-skills` passes.
- Source stays `2.0.0-beta.24`, one prerelease ahead of NuGet. Publishing is `dev release` from a clean synced master after merge. The checklist does not claim that release is already cut.

## Checklist

- [x] Pick the shape (OperationAttribute properties vs dedicated attributes) and record why
- [x] Contracts types and descriptor/binding members
- [x] Tests and docs
- [ ] Publish TimeWarp.Contracts with `dev release` after this branch merges. Source is already `2.0.0-beta.24`

## Session

- Created: 139624 (2026-10-20) from timewarp-nuru 508-015 (implementer), because McpOperationBinding lacks the hints.
- 2026-10-22: `Title` on `[Operation]`. `[Destructive]` and `[OpenWorld]` are tri-state attributes. `OperationDescriptor` carries `Title`, `DestructiveHint`, and `OpenWorldHint`. The contracts generator and the endpoint emit model read them by metadata name and leave the route alone. Source stays `2.0.0-beta.24` (already one prerelease ahead of NuGet `2.0.0-beta.23`). The cut is `dev release` from master after merge.
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-22T13:24:43Z
- Review oracle round 1 (2026-10-22): effort 3. Roster: general, which is the review oracle (claude-opus-5-5) plus an independent sub-agent read (sonnet, `abb85a851a96d8e80`). Findings: 0 bug, 2 suggestion (M1, M2), and 2 nit (M3, M4), all open (`review/round-1/merged.md`). I re-ran the gates: `dev build` gave 0 warnings and 0 errors, the tests passed 47/47 and 146/146, and `check-version` and `ganda repo audit` both passed. `dev lint-skills` did not run, because `vally` is not on PATH. The implementer restatement matches the task and the Decision, so there is no misread. Disposition waits on a fix pass.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-22T13:31:43Z
- Fix pass (implement-fix-2, 2026-10-22): M1 aligns the destructive hint with MCP (a delete or overwrite, rather than an update that only adds data), uses an append for `[Destructive(false)]`, and states that an omitted `destructiveHint` is true and is read only when the tool is not read-only. M2 records that generators ignore the hint attributes unless `[Operation]` has a non-blank name. M3 adds a whitespace-name-plus-hints test in each reader and an explicit `[Destructive(true)]` test. M4 leaves the NuGet publish unchecked until `dev release` after merge.
- Review oracle round 2 (2026-10-22): effort 1, roster general (claude-opus-5-5). I reviewed the fix delta `598e03177..13dd8cef6`. Round 1 M1–M4 are fixed. One new nit, M5, is open: two test fixtures contradict the documented destructive meaning (`review/round-2/merged.md`). I re-ran the gates: `dev build` gave 0 warnings and 0 errors, the tests passed 47/47 and 149/149, and `ganda repo audit` passed. `dev lint-skills` did not run, because `vally` is not on PATH. The restatement matches the task and the round 1 findings, so there is no misread. Disposition waits on a fix pass for M5.
- Fix pass (implement-fix-3, 2026-10-22): M5 aligns the fixtures with that meaning. `[Destructive(false)]` sits on `notes.append`, which only adds data. A stated destructive hint, including the explicit-true case and the blank-name omission case, sits on a `Command` with `HttpVerb.Delete` or `HttpVerb.Post`. The route stays `api/Roles`.
- Review oracle round 3 (2026-10-22): effort 1, roster general (claude-opus-5-5), session not reported. I reviewed the fix delta `86f340e42..d87c02ded`. M5 is fixed and I found nothing new. Tests pass 47/47 and 149/149 on `d87c02ded`. The restatement's added fixture criterion comes from the round 2 finding and fits the task, so there is no misread. Disposition: clean.

## Results

`OperationAttribute.Title` is the human-readable name. The setter stores null when the value is null, empty, or whitespace. `[Destructive(bool isDestructive = true)]` and `[OpenWorld(bool isOpenWorld = true)]` are the destructive and open-world hints. Attribute absence is unset. A bare attribute is true. An explicit `false` stays false. `OperationDescriptor` carries `Title`, `DestructiveHint`, and `OpenWorldHint` (`bool?`). JSON catalog output (camelCase, nulls omitted) includes a stated false and omits a null. The CLI pattern and the operation name stay unprefixed.

`McpOperationBinding` stays the host-filled tool name. The hints are operation facts, so a CLI catalog can project them when MCP is unbound.

The contracts generator emits `OperationTitle`, `OperationDestructive`, and `OperationOpenWorld` only when the hint is set. A bare `[Destructive]` or `[OpenWorld]` emits `true`. A whitespace title emits no `OperationTitle`. `RouteTemplate` is unchanged. `EndpointEmitModel` reports the same values and the FastEndpoint template does not read them.

Metadata names: `TimeWarp.Contracts.OperationAttribute` named argument `Title`, `TimeWarp.Contracts.DestructiveAttribute`, and `TimeWarp.Contracts.OpenWorldAttribute`.

Source version stays `2.0.0-beta.24`. That version is already one prerelease increment ahead of published `2.0.0-beta.23`, and `dev check-version` lists `TimeWarp.Contracts` among the packages at the source version. A second bump would skip `2.0.0-beta.24`. Publishing stays `dev release` from a clean synced master after this branch merges (`tw-release`). NuGet does not have this build yet.

A file that imports both `TimeWarp.Contracts` and `TimeWarp.Nuru` must qualify `[Destructive]` and `[OpenWorld]` after the Nuru follow-up bumps the package. The Nuru attribute wins when both are present, as the Notes already say.

The destructive hint means a delete or an overwrite, as opposed to an update that only adds data. The package readme's `[Destructive(false)]` example appends a note, and the catalog and both generator readers use that same `notes.append` operation for an explicit false. MCP clients treat an omitted `destructiveHint` as true and read it only when the tool is not read-only. The catalog still stores null and does not fill in that client default. Generators copy `[Destructive]` and `[OpenWorld]` only when `[Operation]` has a name that is not blank. A reflection reader sees the attributes either way. An explicit `[Destructive(true)]` emits `OperationDestructive = true` from a `Command` on `HttpVerb.Delete`. A bare destructive hint uses the same shape. A whitespace operation name emits no hint members, and that fixture is a delete command as well.

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
- Foundation contracts tests: 47 passed, 0 failed. Title, both hint attributes, and catalog JSON are covered. The stated false is `notes.append`. A null hint is absent.
- Source generator tests: 149 passed, 0 failed. A set title and explicit false hints are copied from `notes.append` on `HttpVerb.Post`. A bare hint attribute and an explicit `[Destructive(true)]` are true, both on a `Command` with `HttpVerb.Delete`. A whitespace title and an omitted hint are omitted. A whitespace operation name together with hint attributes emits no hint members in either reader. `RouteTemplate` stays `api/Roles`.
- `dev check-version` exits 0. Source is `2.0.0-beta.24`, NuGet is `2.0.0-beta.23`, one prerelease increment ahead, and it reports the version is safe to release. `TimeWarp.Contracts` is in the package list.
- `ganda repo audit` exits 0.
- `dev lint-skills` exits 0 with vally `0.19.0`. Nine skills pass, including `tw-web-api-contracts`, and its eval spec is valid.

### Review disposition

- Outcome: **clean**. 3 rounds, effort 1, roster general (claude-opus-5-5). Round 1 also had an independent sonnet read.
- Final counts: bug 0. Suggestions: 2 fixed (M1, M2). Nits: 3 fixed (M3, M4, M5). Nothing is open and nothing is wontfix.
- Paths: `review/review-framework.md`, `review/round-3/merged.md`, `review/disposition.md`.

## Review

- Round 1 findings M1, M2, M3, and M4 are addressed on this branch.
- Round 2 finding M5 is addressed on this branch.
- Round 3 verified M5 and found nothing new. Disposition is clean (`review/disposition.md`).
- Round 3: Done
## Notes

- Follow-up in timewarp-nuru after this ships: read the Contracts hints by metadata name, with the
  Nuru attribute winning when both are present. Bump the TimeWarp.Contracts reference, and update
  `documentation/user/features/neutral-contracts.md` (section "Tool hints").
- Related: timewarp-nuru 508-015, Roslynk task 004 (PR #6).
