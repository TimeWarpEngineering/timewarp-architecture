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

## Session

- Implementation: grok session (2026-08-06)


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
