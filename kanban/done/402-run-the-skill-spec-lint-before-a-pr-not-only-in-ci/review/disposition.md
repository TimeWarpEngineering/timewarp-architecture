# Disposition — task 402

**Date:** 2026-10-22
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer at effort 2 reviewed e653349df. The verb, the single vally pin, the CI call through the verb, the drift check, and the AGENTS.md row all match the task and the implementer restatement. The reviewer re-ran the gates:

- dev-cli-tests: 333/333.
- Wrong vally version: exit 1 with the pinned install.
- Pinned 0.19.0: 9/9 skills plus the eval spec, exit 0.
- Over-limit `SKILL.md` fixture (527 lines): exit 1 with vally's file-length message.

One nit was raised and accepted as wontfix. No bugs or suggestions.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | Windows npm `.cmd` shim is not found by the PATH probe or the launch. Out of scope for matching the Linux CI gate. Fails safe with the install message. | review orchestrator |

## Escalations

- None.
