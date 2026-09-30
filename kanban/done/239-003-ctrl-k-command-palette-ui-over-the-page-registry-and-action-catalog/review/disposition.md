# Disposition — task 239-003

**Date:** 2026-09-30
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

One general reviewer at effort 3 found 2 bugs, both in the JS focus/listener lifecycle: a repeat Ctrl-K lost the
focus-return target, and a registration that finished after the page was disposed leaked the document listener.
It also found 4 suggestions and 2 nits (issue 4 was split into M4 and M8). Everything was fixed on this task id
except M8. M8 is the focus trap and the panel-padding click on the shared `ModalContainer`, and is wontfix here:
it is cross-modal a11y, not palette UI. `dev build` was 0/0 and the palette suite passed 23/23 after the fixes.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M8 | suggestion | Focus trap / panel-padding focus loss live in the shared `ModalContainer` used by every modal. Fixing them is a cross-modal a11y change outside this palette UI task. Backdrop click and Esc recover. Follow-up candidate under parent 239. | orchestrator (review oracle) |

## Escalations

- None.
