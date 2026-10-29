# Round 2 — merged findings
**Date:** 2026-10-30
**Sources:** general (sonnet subagent aecad7dafd978a4bc), orchestrator verification (Fable 5.1 review oracle)
**Scope:** fix-pass delta, commit cadb4b0df (round-1 base cc160a6ee); product files:
`complete-agent-chat-contracts.cs`, `relay-chat-client.cs`, `ask-rejection-tests.cs`

## Orchestrator verification

- Gates re-run in this worktree after the fix pass: `dev build` (runfile) 0 warnings / 0 errors;
  `ganda repo audit` exit 0, the only non-pass being the pre-existing kanban-landed advisory about
  other tasks' claims (none on 447); web-spa `--filter-tag Unit` 430/430; `--filter-class AskRejection`
  4/4. `dev template-smoke` not re-run here (implementer reported success in round 1; it is the PR
  CI gate and the delta changes no template symbols).
- M1 re-verified: `ReachesModel` is gone; `ToTurns` keeps `(message, ToTurn(message))` pairs on
  `!IsEmpty(turn)` (relay-chat-client.cs:118-126); the validator calls the same public static
  (complete-agent-chat-contracts.cs:53, :167). One rule, no second copy.
- M3 re-verified: Design note clause present (relay-chat-client.cs:28-33); `lastUser` computed over the
  filtered `sent` list (:128). Traced `Relay_Sends_A_Picture_Taken_Before_A_Rejected_Call_In_The_Same_Turn`:
  the approval response (history index 4) is dropped, so `lastUser` = 0, the picture result at index 2 is
  collected, and `runEnds` places the picture turn at `turns[3]`. Under the pre-filter order the picture
  would be skipped and the test fails. Not vacuous.
- `PictureResult` with `[property: JsonIgnore]` serializes to `{"Summary":...}` so `ToolResult` is
  non-empty and the picture travels via `IToolResultImage`, same shape as `CatalogAgentCallResult`.
- Goal restatement still matches the task Description and Checklist. No misread.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs:118
- Description: (round 1) `ReachesModel` restated `ToTurn` + validator `IsEmpty` by hand.
- Source: general, orchestrator
- Disposition notes: fixed in cadb4b0df. `CompleteAgentChat.IsEmpty` is the single public rule; the
  validator and the relay both call it. Edge kinds pinned by
  `Relay_Leaves_Out_Messages_That_Map_To_An_Empty_Turn`. Verified by general and orchestrator in round 2.

### M2 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-rejection-tests.cs:46
- Description: (round 1) Unit test borrows `CaptureSpa` / `CaptureJsRuntime` from the Integration class.
- Source: general
- Disposition notes: wontfix (review oracle, round 1, unchanged). Helpers belong with the screenshot proof
  that owns them; relocate when a third consumer appears.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs:28
- Description: (round 1) Design note did not say `lastUser` is computed after the filter; no test pinned it.
- Source: general
- Disposition notes: fixed in cadb4b0df. Design clause added; pinned by
  `Relay_Sends_A_Picture_Taken_Before_A_Rejected_Call_In_The_Same_Turn`. Verified in round 2.

## New findings

None. The general reviewer and the orchestrator each found no new defect on the delta.

## Duplicates / conflicts

- None in this round.
