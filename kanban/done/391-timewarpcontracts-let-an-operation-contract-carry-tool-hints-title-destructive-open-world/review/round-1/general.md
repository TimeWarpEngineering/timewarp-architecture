# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** `master...0c50a297a` (`source/`, `skills/`, `tests/`), against task.md `## Decision` and `## Goal (implementer restatement)`

## Summary

The change does what the Decision says. `OperationAttribute.Title` stores null for a blank value. `[Destructive]` and `[OpenWorld]` are tri-state attributes. `OperationDescriptor` carries `Title`, `DestructiveHint`, and `OpenWorldHint`. Both Roslyn readers copy the hints by metadata name and leave the route alone. Reader logic, record equality and hashing, and the incremental `Part` equality are correct. A hint on any partial declaration is read, because `GetAttributes()` merges all of them.

I re-ran the gates in this worktree:

- `dev build`: 0 warnings, 0 errors.
- Foundation contracts tests: 47 of 47 passed.
- Source generator tests: 146 of 146 passed.
- `dev check-version`: exit 0, `2.0.0-beta.24` against NuGet `2.0.0-beta.23`.
- `ganda repo audit`: passes.
- `dev lint-skills` did not run here, because `vally` is not on PATH. CI runs it.

The findings are in the docs that tell authors what a hint means, plus small gaps in the tests and the checklist. None changes runtime behavior.

## Issues

### Issue 1 — Severity: suggestion
- File: source/foundation/timewarp-contracts/destructive-attribute.cs:19
- Description: The destructive hint's docs disagree with each other and with MCP.
  - The `<summary>` says "may delete or overwrite data that cannot be recovered". The remarks (line 25) and readme.md:37-38 say `[Destructive(false)]` "only adds data".
  - MCP defines `destructiveHint` as destructive updates versus only additive updates. Recoverability is not part of that definition.
  - The readme example (readme.md:41-42) puts `[Destructive(false)]` on `rename_symbol`. A rename overwrites existing code, so it is not additive. The package readme therefore shows an operation that overwrites data as the model for "not destructive".
  - The docs also leave out two MCP rules. An omitted `destructiveHint` defaults to true, as `openWorldHint` does, which the docs do state. And `destructiveHint` only means something when `readOnlyHint` is false, so it is meaningless on an `IQuery`.
- Failure scenario: An author reads the `<summary>` and leaves `[Destructive]` off a delete that can be recovered, or copies the readme and marks a rename `[Destructive(false)]`. The harness then auto-approves a tool call that overwrites user data, which is the call this task exists to let the harness prompt for.
- Suggestion:
  - Reword the `<summary>` to the MCP sense: "may perform destructive updates (delete or overwrite) rather than only additive ones".
  - Use an additive operation in the readme example, or mark the rename `[Destructive]`.
  - In the attribute remarks, `readme.md`, and `neutral-operations.md`, say that MCP treats an omitted `destructiveHint` as true and reads it only when the tool is not read-only.
- Status: open

### Issue 2 — Severity: suggestion
- File: skills/tw-web-api-contracts/references/neutral-operations.md:66
- Description: The two generator readers ignore `[Destructive]` and `[OpenWorld]` unless the request also has an `[Operation]` with a name that is not blank (contracts-generator.cs:286-293, endpoint-metadata.cs `ReadOperation`). The Design regions record this. The skill reference and the package readme `## Tool hints` sections do not. A reflection reader, such as the Nuru follow-up, sees the attribute either way, so the two kinds of reader disagree on the same contract.
- Failure scenario: An author adds `[Destructive]` to a hosted request that has no `[Operation]` yet. TWA0035 is only info, so the build stays green. The generated partial has no `OperationDestructive`, while a reflection-built catalog reports `destructiveHint: true`.
- Suggestion: In both `## Tool hints` sections, state that the hints belong to an `[Operation]` request and that generators ignore them without one. Optionally, file a follow-up for an analyzer that flags a hint attribute without `[Operation]`. The repo prefers a build check to agreement from memory.
- Status: open

### Issue 3 — Severity: nit
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-tests.cs:917
- Description: Both Design regions say a whitespace operation name drops the title and the hints too. No test covers that case with hint attributes present, in either reader, and no test covers an explicit `[Destructive(true)]` through a reader. (Title escaping uses the shared `EscapeLiteral`, which `Should_Escape_Operation_Literals` covers, so that case is not a gap.)
- Failure scenario: A refactor that moves `ReadBoolHint` above the blank-name return emits `OperationDestructive` for a request that is not an operation, and no test fails.
- Suggestion: Add one test per reader that combines `[Operation("  ")]` with `[Destructive]` and asserts that no hint is emitted. Add one test for `[Destructive(true)]`.
- Status: open

### Issue 4 — Severity: nit
- File: kanban/to-do/391-timewarpcontracts-let-an-operation-contract-carry-tool-hints-title-destructive-open-world/task.md:59
- Description: `- [x] Release a TimeWarp.Contracts prerelease` is ticked, but nothing has been released. Results say NuGet does not have this build yet and that publishing waits for `dev release` after merge.
- Failure scenario: A later reader or a host node takes the ticked box as proof that the hints are on NuGet, and the Nuru follow-up bumps to a version that does not exist yet.
- Suggestion: Untick the box, or reword it to "Source at `2.0.0-beta.24`; publish via `dev release` after merge", until the release is cut.
- Status: open
