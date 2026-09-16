# Review framework — task 230

**Date:** 2026-09-16
**Host task:** kanban/in-progress/230-prevent-and-repair-passkey-vs-microsoft-365-account-forks-bootstrap-account-choice-and-add-existing-passkey-merge/
**Diff scope:** branch `task/230-prevent-and-repair-passkey-vs-microsoft-365-accoun` vs merge-base `origin/master` (`ee5d951f`). Product commit `2229e3b8` feat(identity): prevent Entra bootstrap forks and merge via existing passkey.
**Plan / brief:** See `task.md` Requirements. Two deliverables: (A) unknown-handle Entra bootstrap parks claims and redirects to `/Login/Microsoft365/Choose` instead of minting a principal; Create vs I-already-have (passkey assert then attach Entra). Sync-hit and `AllowBootstrap` off unchanged. (B) `Credential.ReparentTo` / `Principal.MergeInto` / `IPrincipalStore.MergePrincipalAsync`; Settings "Add an existing passkey" merge ceremony; link-mode merge when the Entra handle is owned by another active unmerged principal. Merge proof is a real authentication. Out of scope: multi-tenant, admin-forced merge, un-merge, profile merge beyond DisplayName/trust tier.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0aaa1-ad94-7c02-a5b8-221bcb93ca2f (2026-09-16). General reviewer (round 1): grok session 01a0aaa3-bdc5-72a0-ac43-4dd754d4b4cf (2026-09-16).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2

**Date:** 2026-09-16
**Scope:** Re-verify M1–M4 against the post-fix uncommitted delta; scan that delta for new defects. Do not clobber round 1.
**Roster:** general
**Session IDs:** General reviewer (round 2): grok session 01a0aab4-5926-7271-aba5-8483d910279b (2026-09-16).

## Round 3

**Date:** 2026-09-16
**Scope:** Re-verify M5 (Jaribu wrappers for `Update_rejects_PrincipalId_reparent`) against the wrapper delta. Carry M1–M4 as fixed. Do not clobber prior rounds.
**Roster:** general
**Session IDs:** General reviewer (round 3): grok session 01a0aab8-a523-7d13-9bbb-50282b9cba99 (2026-09-16).
