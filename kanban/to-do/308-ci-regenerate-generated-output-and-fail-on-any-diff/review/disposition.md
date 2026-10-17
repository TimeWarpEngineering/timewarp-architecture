# Disposition — task 308

**Date:** 2026-10-17
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

The ingress generator is byte-stable, with ordering documented in its Design region. The checked-in
golden matches both host emits byte for byte. `dev regenerate-generated` plus the `fast`-job
`git diff --exit-code` / porcelain check meets the drift gate. Generator tests pass 15/15, dev-cli
tests pass 4/4, and `ganda repo audit` is clean. The server-offer family has nothing to regenerate
(task 282), and the change records that rather than inventing a generator. One nit was raised and
accepted as wontfix.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | The CI drift failure prints a raw diff with no remedy echo. The gate is correct, and the remedy is in the step comment, AGENTS.md, and the command Design region. Polish is not worth a fix pass. | review oracle |

## Escalations

- None.
