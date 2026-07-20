# Review framework — task 104-029

**Date:** 2026-07-20
**Host task:** kanban/in-progress/104-029-agent-identity-demo-cli-walking-the-key-registration-and-token-ceremony/
**Diff scope:** new `tools/agent-identity-cli/**`, `tests/tools/agent-identity-cli-tests/**`, template exclude, 104-017 pointer
**Plan / brief:** Nuru multi-file runfile CLI for agent keygen/register/token/whoami/demo; library pin `AgentKeyProof.BuildSignedData`; default server https://localhost:63611
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrate-task 104-029 (2026-07-20)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
