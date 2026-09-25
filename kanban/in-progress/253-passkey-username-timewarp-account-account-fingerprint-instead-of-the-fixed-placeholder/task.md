# Passkey username: TimeWarp account · <account fingerprint> instead of the fixed placeholder

## Description

Every passkey created on the site carries the WebAuthn `user.name` / `user.displayName`
"TimeWarp user" (hard-coded `PlaceholderUserName` in
`source/container-apps/web/features/identity/start-passkey-registration/start-passkey-registration-handler-application.cs:28`,
passed to `WebAuthnRegistration.BuildOptionsJson`). The authenticator stores that value at
creation and it can never be edited afterwards (Proton Pass lets users edit its Title, not the
Username). Result: every account shows up in every password manager as the same "TimeWarp user",
so two accounts on one device are indistinguishable at sign-in.

Decision (Steve, 2026-09-24): username = `TimeWarp account · <account fingerprint>`, where the
**account** fingerprint is one value per principal (the same on every passkey of that account).
It is NOT the per-credential `Fingerprint` from 248-001 (that is derived from the credential id,
which the authenticator only produces during the ceremony, and it differs per passkey).
No new required signup field.

## Requirements

- **Account fingerprint.** Define it once in `timewarp-identity` (e.g. `PrincipalFingerprint.Compute(PrincipalId)`
  = last 8 hex of SHA-256 of the principal id bytes, same shape as `CredentialFingerprint`).
  Display-safe, never the raw id. Expose it on the current-session / profile response the SPA
  already reads (pick the one Settings uses for "Signed in"; record which).
- **Signed-in add passkey.** The add-passkey start path (the signed-in registration flow used by
  `AddPasskey` / Settings "Create a passkey") sends `user.name` = `user.displayName` =
  `TimeWarp account · <account fingerprint>` for the caller's principal.
- **New account.** The principal is currently minted at completion
  (`complete-passkey-registration-handler-application.cs:102`, `Principal.Create(PrincipalKind.Human)`).
  Pre-allocate the principal id at **start**: generate it in `StartPasskeyRegistration`, keep it with
  the issued challenge in the WebAuthn challenge store (`i-webauthn-challenge-store.cs` /
  `in-memory-challenge-store-core.cs` — extend the stored ceremony state, do not add a second
  store), send the username derived from it, and have Complete create the principal with that id
  (add a `Principal.Create` overload taking an id, keep the invariants). A challenge that is never
  completed allocates nothing persistent. Keep the existing account-resolution rule (credential
  handle, not name) unchanged; the pre-allocated id must not be trusted from the client — it lives
  only server-side with the challenge.
- Replace `PlaceholderUserName` everywhere; reconcile the Design regions (start + complete
  registration handlers, challenge store) — the "never persisted / fixed placeholder" text changes.
- **Settings.** Show the account fingerprint next to the signed-in identity so it matches the
  password-manager entry (e.g. "Signed in · TimeWarp account · 7f3a9c21").
- **Existing passkeys** keep "TimeWarp user" (immutable at the authenticator). Note it in Results.
- **Tests.** Start options carry the derived name for new and signed-in flows; the principal
  created at Complete has the pre-allocated id (so its fingerprint matches the name sent);
  two new accounts get different names; a second passkey on one account gets the same name as the
  first; an expired/unused challenge leaves no principal; tampering (client-supplied id) is
  impossible because the id is not on the wire. Contract round-trip for the new field.
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`.
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host) — task
  worktrees share the master user-secrets id. Record the manual Proton Pass check as not performed.

## Checklist

- [x] `PrincipalFingerprint` + exposure on the session/profile response
- [x] Signed-in add passkey sends the account name
- [x] New account: principal id pre-allocated at start, kept with the challenge, used at complete
- [x] `PlaceholderUserName` removed; Design regions reconciled
- [x] Settings shows the account fingerprint
- [x] Tests
- [x] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Results

- **Account fingerprint:** `TimeWarp.Identity.PrincipalFingerprint.Compute(PrincipalId)`
  (`source/libraries/timewarp-identity/principals/principal-fingerprint.cs`) = last 8 lowercase hex
  of SHA-256 over the id's 16 Guid bytes — same shape as `CredentialFingerprint`, one per principal.
  Exposed as `GetCurrentSession.Response.AccountFingerprint` (derived in the ctor from `PrincipalId`,
  null when unauthenticated). **Chosen response: GetCurrentSession** — Settings had no "Signed in"
  line reading either response; the session read is what the SPA auth state already makes, and
  GetProfile is a product slice. The SPA `IdentitySessionAuthenticationStateProvider` projects it as
  claim `timewarp:account_fingerprint`; the hosted prerender provider derives the same claim from the
  cookie principal's `timewarp:principal_id`.
- **Name formatter:** `PasskeyAccountName` (identity contracts) → `"TimeWarp account · <fingerprint>"`,
  shared by server (user.name = user.displayName) and Settings.
- **Signed-in add passkey:** `StartPasskeyRegistration.Command.ForCurrentAccount` (new optional bool).
  Settings "Create a passkey" (`CredentialsState.AddPasskey`) sets it; the handler reads the caller from
  the session cookie (`IBrowserSessionService`) and names the passkey for that account; no session → 401
  before any challenge is issued. The flag cannot pick another account — the id always comes from the session.
- **New account:** Start (default, `ForCurrentAccount=false`) pre-allocates `PrincipalId.New()` and keeps it
  with the challenge — `IWebAuthnChallengeStore.Issue(type, pendingPrincipalId)` / `TryConsume(type,
  challenge, out pendingPrincipalId)` (the existing entry in `InMemoryChallengeStoreCore` gained the field;
  no second store). `PasskeyRegistrationCeremony.Materials.PendingPrincipalId` carries it to Complete, which
  calls the new `Principal.Create(kind, id)` overload (same invariants + empty-id guard). The id is never on
  the wire. Complete refuses a challenge with no pending id (one started with `ForCurrentAccount`) with the
  uniform 400 ChallengeInvalid before minting anything, so a new account never carries another account's name.
  Account resolution (credential handle) unchanged; AddPasskey ignores the pending id.
- `PlaceholderUserName` removed. Design regions reconciled: start/complete registration handlers, AddPasskey
  contract + handler, registration ceremony, challenge store port/in-memory/core, Principal, GetCurrentSession,
  hosted auth-state provider, SPA auth-state provider, CredentialsState.AddPasskey, SettingsPage.
- **Settings:** heads the page with `Signed in · TimeWarp account · <fingerprint>` (`data-qa="SignedInAccount"`),
  rendered in prerender too. Mock auth has no claim → line absent.
- **Existing passkeys keep "TimeWarp user"** — the authenticator stores user.name at creation and the relying
  party cannot change it. Users who want the new name must create a new passkey (and may revoke the old one).
- **Tests:** library — `PrincipalFingerprint_` (shape, stability, SHA-256 layout, distinct, no raw id, empty
  rejected; `Principal.Create(kind, id)` uses the id and keeps invariants) + challenge-store `Pending_Principal_Id`
  (round trip, null when not recorded, one-time, expired/wrong-type yield no id). Integration
  `PasskeyAccountName_` (6): new-account name = fingerprint of the principal Complete mints; two new accounts
  differ; signed-in Start names the caller and a second passkey gets the same name (+ session AccountFingerprint
  matches); ForCurrentAccount without session 401; current-account challenge used for Complete → 400 and no
  principal; unused challenges leave no principal. Contracts — GetCurrentSession AccountFingerprint round trip;
  Start/Complete registration commands carry no PrincipalId/Guid property (no wire field to tamper with);
  ForCurrentAccount round trip. Settings prerender HTML shows `SignedInAccount` with the fingerprint
  (`ProtectedPageDeepLink_`).
- **Gates:** `dev build` 0 warnings / 0 errors; `dev test` 21 projects, 1428 passed / 0 failed / 1 skipped
  (pre-existing skip); `dev template-smoke` SUCCEEDED; `ganda repo audit` passes. `dev` run via
  `dotnet run tools/dev-cli/dev.cs --` (worktree had no built `bin/dev`).
- **No AppHost started.** Manual Proton Pass check **not performed** (needs a browser + running app).
- `TimeWarp.Identity` public API grew (`PrincipalFingerprint`, `Principal.Create(kind, id)`, challenge-store
  overloads); source `<Version>` 2.0.0-beta.20 is already ahead of the latest release (v2.0.0-beta.19), so no bump.

- **Review (task-work review oracle, effort 1, roster: general):** 1 round; final counts bug 0 · suggestion 0 open / 1 wontfix · nit 0.
  Disposition **accepted-exceptions** — M1 (AddPasskey does not refuse a new-account challenge, so a
  non-SPA or pre-deploy cached client could store a phantom fingerprint in that passkey's name) is wontfix:
  cosmetic, attach target is always the caller, enforcing would break cached pre-deploy SPA bundles.
  Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class PasskeyAccountName_
cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class PrincipalFingerprint_
cd tests/container-apps/web/web-contracts-tests && dotnet test -c Release
```

**Expect:** all pass (6 / 9 / 46). Manual (on a machine where running the app is allowed): sign up with a
passkey → the password manager saves the username `TimeWarp account · xxxxxxxx`; Settings shows
`Signed in · TimeWarp account · xxxxxxxx` with the same 8 hex; "Create a passkey" there saves a second entry
with the identical username; a different account shows a different fingerprint.

## Notes

- Origin: Proton Pass save dialog screenshot, 2026-09-24.
- Related: 248-001 (per-credential fingerprint), 250 (Entra account hint).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-24)
- Implemented (ganda task work implement node, 2026-09-24): see Results.
- Reviewed (ganda task work review oracle, Claude Opus 5.5, effort 1 / general, 2026-09-24): accepted-exceptions — see `review/`.

## Fix loop (2026-09-25, cockpit) — close review M1

Steve reversed the M1 wontfix: this is a template with no deployed pre-change clients, so the
"cached old SPA bundle" rationale does not apply. Required on this branch:

- `AddPasskey` must refuse a challenge that was issued for a NEW account (one that carries a
  pending principal id) with the same uniform 400 ChallengeInvalid used by Complete for the
  mirror case, before any credential is written. After this, a passkey's stored username always
  names the account it is attached to.
- Update review M1 to fixed (round-2 merged/disposition), reconcile the AddPasskey Design region,
  and add an integration test: new-account challenge used for AddPasskey → 400, no credential added.
- Gates in the FOREGROUND: `dev build` 0/0, `dev test`, `dev template-smoke`. Push to PR #404.
- Do NOT start an AppHost.
