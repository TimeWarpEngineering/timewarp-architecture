# Review framework — task 527

**Date:** 2026-11-26
**Host task:** kanban/in-progress/527-expose-web-server-externally-on-aca-targets-when-yarp-is-off/
**Diff scope:** Round 2 reviews fix commit `575761ebb` (docs for M1 and M2: `skills/tw-deploy/SKILL.md`, `references/azure-container-apps.md`, `references/azure-container-apps-neon.md`). Round 1 reviewed the branch vs merge-base `e7a75497d` (master), product commit `46209caed`.
**Plan / brief:** With `--yarp false`, publish targets `aca` and `aca-neon` make web-server the one external container app, allow `web-server.<environment default domain>` as a passkey RP ID, and point `dev deploy` smoke and `dev open` at that app. Compose, Kubernetes, run mode, and yarp-on stay as they are.
**Effort:** 1 for round 2 (general). Round 1 was effort 2.
**Reviewer roster:** general
**Session IDs:** review oracle (grok), round 1, 2026-11-26, session `01a213cf-6b25-7be1-a81a-a6cee28d804d`. Round 2 session `01a213db-6840-7c21-a2f8-208335593a6c` (grok), 2026-11-26. Implementer session `921b3c23-b8be-4d17-94ac-f2d0de39bfef` (claude), recorded on `task.md`.

## Budget (by-diff)

- Lines changed: 169
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Goal check

The implementer restatement matches the original requirements. No misread recorded under `## Learnings`.
