# Round 1 — merged findings
**Date:** 2026-10-30
**Sources:** general (sonnet subagent), orchestrator verification (Fable 5.1 review oracle)

## Orchestrator verification

- Root cause re-verified against the decompiled Microsoft.Extensions.AI 10.11.0 assembly:
  `FunctionInvokingChatClient.GenerateRejectedFunctionResults` sets the rejected call's
  `InformationalOnly = true`, and the approval pair strip predicate requires `!InformationalOnly`.
  The Design-region note in relay-chat-client.cs is accurate.
- Gates re-run in this worktree: `dev build` 0 warnings / 0 errors; `ganda repo audit` passes with
  only the pre-existing kanban-landed advisory about other tasks' claims;
  `dotnet run -- --filter-class AskRejection` 2/2 pass. `dev template-smoke` was not re-run here
  (implementer reported success; it is the PR CI gate).
- Goal restatement matches the task Description and Checklist. No misread.
- `ReachesModel` agrees with the validator's `IsEmpty` for every content kind MEAI produces today
  (whitespace text, reasoning-only, non-image DataContent, image-only user message all checked).

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 1 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs:165
- Description: `ReachesModel` is a hand-written restatement of "`ToTurn` produces a turn the
  validator's `IsEmpty` accepts" (`ToTurn` at line 219; `IsEmpty` at
  complete-agent-chat-contracts.cs:233). Three predicates must agree by memory. If `ToTurn` later
  maps a new content kind and `ReachesModel` is not updated, that message is silently dropped; if
  `ToTurn` stops mapping a kind `ReachesModel` still accepts, the empty-turn 400 this task fixes
  returns. AGENTS.md hard rule: when two things must agree, generate one from the other or add a
  build check.
- Suggestion: Derive the filter from the produced turn. Build `(message, turn)` pairs with
  `ToTurn` and keep a pair only when the turn is non-empty by the contract's own rule (expose the
  emptiness rule on the contract, for example an internal `Turn` helper the validator also calls,
  or an equivalent single predicate). Run `lastUser` and the picture-turn loop over the kept pairs.
  Add a case for the edge kinds (reasoning-only assistant message, whitespace-only text, non-image
  user DataContent, image-only user message) so a drift fails a test.
- Source: general, orchestrator (raised independently by both)
- Disposition notes: implemented in fix pass (implement-fix-2, 2026-10-30). `CompleteAgentChat.IsEmpty`
  is public and the validator calls it; `ToTurns` keeps `(message, ToTurn(message))` pairs that are
  not empty, and `ReachesModel` is gone. Edge kinds pinned by
  `Relay_Leaves_Out_Messages_That_Map_To_An_Empty_Turn`.

### M2 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-rejection-tests.cs:27
- Description: The Unit-tagged `AskRejection_Should` borrows `CaptureSpa` and `CaptureJsRuntime`
  from the Integration-tagged `CatalogAgent_Should` (widened private → internal) and duplicates the
  `"Screenshot.CaptureScreenshot"` literal.
- Suggestion: Move the two helpers to `infrastructure/` as top-level internal types.
- Source: general
- Disposition notes: wontfix (review oracle). The helpers are the catalog screenshot fixture
  (real catalog, WebMCP dispatcher, store, CaptureJsRuntime browser) and belong with the proof that
  owns them. The Unit run passes without the Integration `SetupOnce`, so the coupling is to the
  nested types only, and the Design notes on both files record the sharing. Relocate when a third
  consumer appears.

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs:114
- Description: Filtering before `FindLastIndex` changes what `lastUser` means: it was the last
  user-role message (which could be a surviving approval-response message), it is now the person's
  last message. For a turn with a rejection this keeps tool pictures from calls made earlier in that
  turn, where before they fell before the approval-response message and were dropped. This is more
  correct and matches the Design text, but the task 447 Design note does not say so and no test pins it.
- Suggestion: Add one clause to the Design note (`lastUser` is computed after filtering, so an
  approval response never counts as the person's message). Optionally pin it with a case that puts
  a tool-image result before a surviving rejected pair and asserts the picture turn is sent.
- Source: general
- Disposition notes: implemented in fix pass (implement-fix-2, 2026-10-30). Design note clause added;
  pinned by `Relay_Sends_A_Picture_Taken_Before_A_Rejected_Call_In_The_Same_Turn`.

## Duplicates / conflicts

- The orchestrator's duplicate-predicate concern and the general reviewer's Issue 1 are the same
  finding; collapsed into M1 with both sources.
