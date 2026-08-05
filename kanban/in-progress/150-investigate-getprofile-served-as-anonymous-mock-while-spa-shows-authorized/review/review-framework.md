# Review framework — task 150

**Date:** 2026-08-05
**Host task:** kanban/in-progress/150-investigate-getprofile-served-as-anonymous-mock-while-spa-shows-authorized/
**Diff scope:** commit `6bd81f13` vs parent `c8ae9def` (4 files: get-profile handler/contracts/co-located tests + new `tests/container-apps/web/web-server-integration-tests/features/profile/get-profile-session-tests.cs`)
**Plan / brief:** Swap GetProfile.Handler from `ICurrentUserService` (reads a `"UserId"` claim no scheme emits) to `ICurrentPrincipalAccessor` (`timewarp:principal_id`), so authenticated passkey sessions get the store-backed profile instead of the anonymous contract mock. Add in-proc passkey-session regression test proven to fail pre-fix. Plan in task.md Notes.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator: Claude (this session); implementer subagent: ac2199ef00fc719aa (sonnet)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Verify the implementer's gate claims independently (build 0/0; co-located runfile 10/10; the 16 integration-suite failures claimed pre-existing at `c8ae9def`)
