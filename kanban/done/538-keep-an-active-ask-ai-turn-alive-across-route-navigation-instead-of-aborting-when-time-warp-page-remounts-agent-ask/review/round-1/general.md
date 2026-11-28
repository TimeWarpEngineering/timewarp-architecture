# Round 1 — general
**Date:** 2026-11-28
**Scope reviewed:** branch vs `master`: `ask-agent-session.cs` (new), `AgentAsk.razor`,
`catalog-agent-functions.cs`, `ask-diagnostics.cs`, `program.cs`, the suppression baseline, the
new `ask-agent-session-tests.cs` + doubles, and the three test hosts that gained the
`AskAgentSession` registration. `TimeWarpPage.razor` read for the `IsPanelOpen` gate around
`<AgentAsk />`.

## Summary

The live `UIAgent`, `AgentContext`, tag/screenshot/invoking client chain, and
`CatalogAgentFunctions` now live in a circuit-scoped `AskAgentSession`. `AgentAsk.Dispose` drops
only its status subscription, session listener, UI binding (token-guarded against the incoming
panel), and voice clock, and calls `Release` only when `IsPanelOpen` is already false. Since
`TimeWarpPage` renders `<AgentAsk />` only while `IsPanelOpen`, close → unmount → release holds,
and a route remount keeps the context. The mid-turn rebuild deferral, generation-change
abandonment, readiness release, and scope disposal carry over unchanged. The off-page →
on-page approval case returns a distinct `ArrivalNeedsApprovalError` and does not run, and
Automatic mode runs the prefill. The three new integration tests cover every requirement-4
bullet (remount keeps the same context, the thread commits one navigate plus the reply, the idle
rebuild wraps `Prefill.Role.CreateRole`, the second turn carries the history, and new
conversation, close, and scope disposal each dispose). The recorded gates (build, four targeted
test files, audit) cover the change. Risk is low. The findings below are a small lifecycle
regression and polish.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/ask-agent-session.cs:321
- Description: `Release()` does not stop a build that is already in progress. If the person
  closes the panel while `EnsureAsync` is awaiting the auth state, tool selection, site map, or
  `RestoreAsync`, `Release` runs, and then the build resumes and installs `Agent`/`Conversation`/
  `Functions` on the session with no panel mounted. The old component freed a build that finished
  after `Dispose` ("A build that finishes after the panel was disposed … frees what it created").
  The session only checks `Disposed`, which is circuit-scope teardown, not close. The new Design
  line "The panel calls Release when it unmounts because the person closed it" implies that a
  closed panel holds no agent. This case breaks that. Impact is small: the held agent is idle, is
  reused on the next open when path, mode, and generation still match, and is replaced on a
  generation or readiness change or freed with the circuit.
- Suggestion: bump a release epoch in `Release()`, capture it at the start of the build, and free
  the new objects (the existing `finally` path) when the epoch changed across an await.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:27
- Description: the rewritten Design comment joins two sentences with three spaces on one
  over-long line ("…reasoning block.   The panel asks AskAgentSession to rebuild…").
- Suggestion: reflow that paragraph to the surrounding width.
- Status: open

### Issue 3 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-agent-session-tests.cs:173
- Description: the local `wrappedOffPage` holds the unwrapped off-page function, and the next
  line asserts that (`ShouldNotBeOfType<ApprovalRequiredAIFunction>`). The name says the
  opposite of what the test proves.
- Suggestion: rename it to `unwrappedOffPage` or `offPagePrefill`.
- Status: open

### Issue 4 — Severity: nit
- File: kanban/in-progress/538-…/evidence/home.png
- Description: `home.png` and `agent-ask.png` are byte-identical (`cmp` reports no difference),
  so the second file adds nothing. Both show the signed-out panel. That is fine for a
  behavior-only change whose proof is the integration test, but the readme lists them as two
  views.
- Suggestion: drop the duplicate, or replace it with a signed-in view of the panel on
  `/Admin/Roles/New` after a navigate turn.
- Status: open
