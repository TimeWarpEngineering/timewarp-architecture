# Settings credentials via TimeWarp.State not page fields

## Description

Settings passkey list/create/revoke used private page fields + `PasskeyCeremonyClient` HTTP.
Every SPA → backend call must go through TimeWarp.State (COPIC / ProfileState rule).
Rehome GetCredentials / AddPasskey / RevokeCredential into `CredentialsState` ActionSets;
Settings only dispatches and binds state.

Also: untracked SSR stack-overflow on authenticated /Settings was buried in a test Design note
(not this task’s full fix unless it falls out of the restructure).

## Requirements

- `CredentialsState` holds passkey/credential list (not page-local `List<>`)
- Fetch / Add / Revoke HTTP only inside State handlers (`DefaultApiHandler` or `BaseHandler` + ApiService)
- SettingsPage does not call `PasskeyCeremonyClient` for list/add/revoke HTTP
- Clear credentials on sign-out with profile/auth clear
- Ceremony client keeps WebAuthn-only helpers used by Login if still needed; Settings path through State

## Checklist

- [x] CredentialsState + Fetch/Add/Revoke/Clear ActionSets
- [x] SettingsPage binds State only
- [x] Ceremony client: remove Settings HTTP surface (list/add/revoke)
- [x] Sign-out / auth listener clears CredentialsState
- [x] `dev build` clean for touched projects
- [ ] Board close: `ganda kanban done 169` so origin-home matches shipped product
- [ ] Kanban-only PR; STOP (do not merge)

## Session

- Implementation: grok session (2026-08-06)
- Cockpit: Grok close request (2026-08-26) — product already on origin/master; remaining is board close

## Notes

Product already shipped on origin/master as `7cfc208a` (`fix(web): Settings credentials through TimeWarp.State (169)`). No dedicated PR; later Settings SSR stack-overflow was **183** (already done). Do not reopen 161.

Remaining work is **board hygiene only** on this same task id:

- Do **not** change Settings, CredentialsState, PasskeyCeremonyClient, or other product files
- Do **not** create a sibling close/hygiene task
- Keep existing Results; add a short board-close line + How to validate for the kitchen move (keep the product smoke steps)
- `ganda kanban done 169`, commit the kitchen move
- `tw-pr` / `gh pr create` with explicit `--head` and `--base`; STOP; do not merge

## Results

### What changed
- Added `CredentialsState` with FetchCredentials / AddPasskey / RevokeCredential / ClearCredentials ActionSets.
- SettingsPage binds state only (no private Passkeys list, no ceremony HTTP).
- PasskeyCeremonyClient no longer exposes list/add/revoke HTTP (Login/demo ceremony only).
- Sign-out + AuthenticationStateListener clear CredentialsState.

### How to validate
Smoke:
1. Sign in → open /Settings → passkeys list loads (or empty message).
2. Create a passkey → list refreshes; toast/message on success/failure.
3. Delete a passkey → list refreshes.
4. Sign out → re-sign-in → list reloads (no stale list from prior principal).

Automated: `dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Debug` (0/0).
