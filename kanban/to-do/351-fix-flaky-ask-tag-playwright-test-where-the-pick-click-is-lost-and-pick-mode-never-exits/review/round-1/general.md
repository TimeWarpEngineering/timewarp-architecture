# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** branch vs master: ask-tag.ts, AskTagJsModule, AskTagState (StartPick, CancelPick,
SessionChanged), AgentAsk.razor, ask-tag integration tests, Playwright sign-in test, Pkcs pin.

## Summary

The change makes the "picking" line a real readiness signal. It arms synchronously in `Start`, raises
`IsPicking` from the `armed` callback, and names every pick with a Guid token, so a cancel that
overtakes its start is refused. The C# token handling is sound. Every exit path clears `PickToken`
first and `IsPicking` only after the browser has left pick mode. Tag is disabled on `IsPickPending`,
and dispose cancels on it. The integration tests cover arming and cancel-while-arming. One gap
remains in the JS refusal memory.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/source/features/ask-tag.ts:55
- Description: `cancelledToken` remembers only one cancelled token. Each handler awaits its own
  `import()` before invoking, so calls from separate handlers can reach the browser out of order (the
  task's race 2). Sequence: StartPick A (Start in flight), then Cancel A, StartPick B, Cancel B,
  StartPick C (each cancel clears PickToken and re-enables Tag). If A's Start lands after Cancel B's
  CancelPick has overwritten `cancelledToken`, A is no longer refused. Its `active?.finish(null)`
  silently ends C's live pick, and `armed()` then cancels A. Without a C, the cost is a brief
  overlay flash. It is narrow, but it is the same class of reordering this task fixes.
- Suggestion: keep a set of tokens cancelled before their Start. Add a token only when no session for
  it ever started. Delete it when Start refuses, so the set stays bounded.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:49
- Description: the reflowed Design comment line runs well past the surrounding ~100-column wrap.
- Suggestion: rewrap.
- Status: open
