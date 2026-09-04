# Review framework — task 205

**Date:** 2026-09-04
**Host task:** kanban/in-progress/205-progressive-profile-and-agent-human-handoff-after-more-domain-exists/
**Diff scope:** `origin/feature/overnight...HEAD` (3 commits: kitchen resume, product feat `6a184f82`, Results `f4c7ea6b`). 69 files, +3177/−104. Not vs `origin/master` (this branch is stacked on overnight).
**Plan / brief:** Hang deferred 104-024 / 104-025 product on existing domain: optional progressive profile (`Features.Profiles`) and optional Agent↔Human link + portable humanUx (`Features.AgentLinks`). Identity kernel stays credentials/trust. Profile must never gate passkey/agent-key/session/token. Paid service must not require a human link.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle Grok 4.6 (2026-09-04); round-1 general reviewer Grok 4.5; round-2 general re-review Grok 4.5

## Round 2 note

Round 1 frozen. Fix delta: filtered unique open-link index (M1), Request validation test (M2), gate-test IAgentHumanLinkStore pins (M3). M4 remains wontfix (mock factory opt-in). Re-verify M1–M3 against the post-fix tree; scan the fix delta for new defects. Do not clobber `review/round-1/`.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Requirements to check

1. Optional display name / email **after** principal exists; never a gate on passkey register, agent-key register, session, or token.
2. Contract/endpoint style of the template (`[ApiEndpoint]` + exactly one of `[EndpointAuthorize]` / `[EndpointAllowAnonymous]`; TimeWarp.Mediator; FluentValidation on the mediator).
3. Optional Agent ↔ Human link, minimal approve/deny, portable humanUx JSON an agent can show its human.
4. humanUx schema in a Design region / sample JSON.
5. **Not** required for paid service (`InvokeMeteredCapability` must not take `IAgentHumanLinkStore`).
6. Placement documented (Identity vs Features.Profiles vs Features.AgentLinks).
7. Slice isolation TWA0009; axis-1 filename grammar; Jaribu co-located tests; Shouldly only.

## Product surfaces in scope

- `source/container-apps/web/features/profile/**`
- `source/container-apps/web/features/agent-links/**`
- SPA: `ProfilePage`, `AgentLinksPage`, profile/agent-links state
- Identity permission seeds + gate tests
- EF migration `20260904134226_AddProfileEmailAndAgentHumanLinks`
- `documentation/developer/how-to-guides/how-to-progressive-profile-and-agent-human-link.md`
- Template-smoke expected web-jaribu count 104 → 125
