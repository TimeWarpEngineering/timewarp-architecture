# Review framework — task 447

## Budget (by-diff)

- Lines changed: 175
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-30
**Host task:** kanban/in-progress/447-ask-rejection-does-not-poison-the-turn-with-retry-loop/
**Diff scope:** branch task/447 vs master; implement commit b32495fc5 (product files only):
- `source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs`
- `tests/container-apps/web/web-spa-integration-tests/features/application/ask-rejection-tests.cs` (new)
- `tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-screenshot-tests.cs`
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Results`. A rejected Ask
approval left an approval request/response pair in the history on every later turn (MEAI marks the
rejected call InformationalOnly, so FunctionInvokingChatClient stops stripping the pair). RelayChatClient
mapped each to an empty Turn, the CompleteAgentChat validator returned 400, AgentContext went to Error,
Retry replayed the same history. Fix: RelayChatClient sends only messages the model reads.
**Effort:** 2 (general)
**Reviewer roster:** general (sonnet subagent, read-only) + orchestrator verification (Fable 5.1)
**Session IDs:** review oracle session recorded on task.md `## Session`

## Round 2 scope (2026-10-30)

**Diff scope:** fix-pass delta only, commit cadb4b0df (round-1 base cc160a6ee):
- `source/container-apps/web/features/agent-chat/complete-agent-chat/complete-agent-chat-contracts.cs`
- `source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs`
- `tests/container-apps/web/web-spa-integration-tests/features/application/ask-rejection-tests.cs`
**Effort:** 1 (general), per the Budget block above (175 lines changed)
**Reviewer roster:** general (sonnet subagent aecad7dafd978a4bc, read-only) + orchestrator verification (Fable 5.1)
**Purpose:** re-verify M1 and M3 against the delta; scan the delta for new defects.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
