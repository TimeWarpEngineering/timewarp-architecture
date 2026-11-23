# Review framework

## Budget (by-diff)

- Lines changed: 2174
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 453

**Date:** 2026-11-24
**Host task:** kanban/in-progress/453-add-voice-input-to-ask-ai-section/
**Diff scope:** branch `task/453-add-voice-input-to-ask-ai-section` vs `master` (merge base), commits de962ada7, 157d6b379, 653a1e1c9
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: Ask composer microphone, browser capture in `ask-ai.ts` / `AskAiJsModule`, server forwards the clip to xAI `POST /v1/stt`, transcript inserted at the caret without sending, idle / recording / transcribing / error-with-retry states, Playwright coverage.
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (claude, model claude-opus-5-5), ganda task work review node, round 1

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
