# Round 1 — merged findings
**Date:** 2026-10-22
**Sources:** general (oracle and an independent sub-agent read, merged in `general.md`)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/foundation/timewarp-contracts/destructive-attribute.cs:19 (also readme.md:37-42, neutral-operations.md:66-76)
- Description: The destructive hint's docs disagree with each other and with MCP's "destructive versus only additive updates". The `<summary>` says "data that cannot be recovered", while the remarks say "only adds data". The readme marks `rename_symbol`, which overwrites code, as `[Destructive(false)]`. The docs also leave out that MCP treats an omitted `destructiveHint` as true and reads it only when the tool is not read-only.
- Suggestion: Reword the `<summary>` to the MCP sense, use an additive operation in the example or mark the rename `[Destructive]`, and document the default-true rule and the not-read-only rule.
- Source: general (oracle and sub-agent)
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: skills/tw-web-api-contracts/references/neutral-operations.md:66 (also source/foundation/timewarp-contracts/readme.md:26)
- Description: The generators ignore the hint attributes unless the request has an `[Operation]` with a name that is not blank. A reflection reader sees them either way. This is recorded only in the Design regions, not in the author-facing `## Tool hints` sections.
- Suggestion: Document the rule in both sections. An analyzer follow-up is optional.
- Source: general (sub-agent; verified by oracle)
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-tests.cs:917 (also endpoint-operation-metadata-tests.cs)
- Description: No test covers a whitespace operation name together with hint attributes, in either reader, and no test covers an explicit `[Destructive(true)]`.
- Suggestion: Add one test per reader, plus one test for the explicit-true case.
- Source: general (sub-agent; narrowed by oracle, because title escaping is already covered)
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: kanban/to-do/391-timewarpcontracts-let-an-operation-contract-carry-tool-hints-title-destructive-open-world/task.md:59
- Description: The checklist item "Release a TimeWarp.Contracts prerelease" is ticked, but no release has been cut.
- Suggestion: Untick it, or reword it to the after-merge `dev release` step.
- Source: general (oracle)
- Disposition notes:

## Duplicates / conflicts

- The sub-agent's items 2 and 3 (the summary wording and the rename example) and the destructive part of its item 1 (the MCP default and the not-read-only rule) are collapsed into M1.
- The sub-agent's item 6 (`get => field;` is redundant) was dropped. It matches the existing `Group` accessor style.
