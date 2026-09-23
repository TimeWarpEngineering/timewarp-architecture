# Credential nickname, registration context, and fingerprint

## Description

Add-time identity for a credential plus a rename action, so identical-provider rows become
distinguishable without touching the sign-in path. Child of 248.

## Requirements

- **Nickname.** `Credential` (`source/libraries/timewarp-identity/credentials/credential.cs`) gains a
  mutable `Nickname` distinct from the provider `Label` (keep `Label` = AAGUID/provider name;
  today it is also overwritten by a caller-supplied label — separate the two). New endpoint
  `RenameCredential` (endpoint-centric contract per `tw-web-api-contracts`; `[ApiEndpoint]` +
  `[EndpointAuthorize]` with the same policy as RevokeCredential; validator: 1–64 chars, trimmed;
  IDOR rule as RevokeCredential — caller may only rename their own). The add flows
  (`AddPasskeyPrompt.razor`, `credentials-state.add-passkey.cs`, the register-passkey and
  agent-key flows) prompt for a nickname pre-filled with the provider name; `AddPasskey.Command.Label`
  becomes the nickname input (rename the property if that reads better; contracts test updated).
- **Registration context.** Capture once at registration: authenticator attachment
  (platform / cross-platform from the WebAuthn response / transports), and the browser + OS
  family from the request User-Agent (parsed server-side into two short strings, never the raw UA).
  Store on the credential (`RegisteredWith` or similar record: Attachment, Browser, Os). Agent keys
  record the client the key was registered from if the ceremony exposes it; else null. EF mapping in
  `credential-entity-type-configuration-infrastructure.cs` + a migration (postgres flag). In-memory
  store parity.
- **Fingerprint.** Expose a short, stable discriminator derived from the credential id (e.g. last
  8 hex of SHA-256 of the id bytes) on `GetCredentials.CredentialSummary` as `Fingerprint`; never the
  raw id or public key (the contract's Design region forbids material on the wire — keep that test).
- **UI.** `CredentialList.razor` row: nickname as the title (fallback provider label), provider +
  attachment + browser/OS as a secondary line, created date, fingerprint in monospace, a Rename
  action (inline edit) beside Revoke. Revoke confirmation restates: nickname, provider, created,
  fingerprint. Uses the shell notification region for outcomes (task 247 rules) — no page-local bars.
- **Tests.** Co-located Jaribu for RenameCredential (happy path, validation rejection, cannot
  rename another principal's credential = 404-shaped like revoke), GetCredentials round-trip with
  the new fields and the material-never-on-wire assertion, registration ceremony test asserting
  attachment/browser/OS captured, SPA test for the row content and inline rename. Existing
  identity suites stay green.
- Design regions reconciled (Credential, contracts, CredentialList). Gates: `dev build` 0/0,
  `dev test`, `dev template-smoke` (postgres flag off must still build: migration + mapping stay
  in the postgres-gated files).

## Checklist

- [x] Nickname field + RenameCredential endpoint + validator + tests
- [x] Add flows prompt for nickname (pre-filled provider name)
- [x] Registration context captured, stored, mapped, migrated
- [x] Fingerprint on the summary; no material on the wire
- [x] CredentialList row + inline rename + revoke confirmation text
- [x] Gates: build 0/0, test, template-smoke

## Fix loop (2026-09-23, cockpit) — PR #397 conflicts with master

Master merged task 246 (PR #396) after this branch forked. 246 renamed `CredentialList`'s
parameters `Delete*` → `Revoke*` (`RevokeLabel`, `RevokeDataQa`, `RevokeDisabled`,
`RevokeDisabledHint`, `RevokeDisabledHintDataQa`, `OnRevoke`), renamed the default data-qa
`DeletePasskey` → `RevokePasskey`, added `CanRevoke` on the Passkeys/Settings pages bound to
`CredentialsState.CanUnlink(ActiveCredentialCount)` with the hint "Add another passkey or agent
key before revoking this one.", and set the revoke status text to "Credential revoked.".
GitHub reports #397 CONFLICTING; overlapping files:

- `web-spa/features/identity/components/CredentialList.razor`
- `web-spa/features/identity/credentials-state/credentials-state.cs`
- `web-spa/features/identity/pages/passkeys-page/PasskeysPage.razor`
- `web-spa/features/application/pages/SettingsPage.razor` and `.razor.cs`

Required on THIS branch (no rebase, no squash): `git merge origin/master`, resolve so that BOTH
land — 246's Revoke naming and last-credential disable + hint, AND this task's row (nickname
title, context line, fingerprint, inline rename, two-step restating confirmation). The two-step
confirm's final button raises the `OnRevoke` callback (246's name), and the disabled state from
246 must also disable the first step of the confirm. Keep 246's new tests
(`credentials-state-revoke-guard-tests.cs`, the prerender facts in `protected-page-deep-link-tests.cs`
asserting `RevokePasskey` + hint) green alongside this task's `credential-list-render-tests.cs`.
Then gates IN THE FOREGROUND: `dev build` 0/0, `dev test`, `dev template-smoke`; commit the merge;
push to the existing PR branch. Also confirm CI actually runs on the pushed commit
(`gh pr checks 397`) — the branch showed no workflow runs at all before this loop; if still none
after the push, say so in Results.

## Notes

- Parent: 248. Sibling 248-002 (last used) depends on this landing first.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED
- Review follow-up (non-blocking, maintainer decision): adopt bUnit or a Playwright identity flow
  for interactive Blazor component coverage (review M7).

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
- Implemented: ganda task work, implementer (Claude Fable 5.1), 2026-09-23
- Reviewed: ganda task work, review oracle (Claude Fable 5.1) + general reviewer subagent (Claude Opus), 2026-09-23 — `review/`
- Fix loop (merge master / 246 conflicts): ganda task work, implementer (Claude Fable 5.1), 2026-09-23
- Re-reviewed (round 2, merge delta): ganda task work, review oracle (Claude Fable 5.1) + general reviewer subagent (Claude Sonnet), 2026-09-23 — `review/round-2/`

## Results

### What landed

- **Domain (`timewarp-identity`)**: `Credential.Label` is now the immutable provider name only;
  new mutable `Nickname` (1–64 after trim, `Rename`), new `RegisteredWith` record
  (`AuthenticatorAttachment` Platform/CrossPlatform/Unknown + `Browser`/`Os` family strings,
  captured at `Create`), new computed `Fingerprint` (`CredentialFingerprint`: last 8 hex of
  SHA-256(handle)). `Snapshot` copies the new state; in-memory store parity is by construction.
- **Contracts**: new `RenameCredential` (`POST api/identity/credentials/{id}/rename`, same
  policy/schemes as RevokeCredential, validator 1–64 trimmed). `GetCredentials.CredentialSummary`
  gains `Nickname`, `RegisteredWith`, `Fingerprint`. `AddPasskey` / `AddAgentKey` /
  `CompleteAgentKeyRegistration` `Label` → `Nickname`; `AddPasskey` and
  `CompletePasskeyRegistration` accept `AuthenticatorAttachment` + `Transports` hints and return
  `ProviderLabel` (+ `CredentialId` on Complete) for the nickname prompt.
- **Application/server**: `RenameCredential.Handler` (ownership → same 404 as revoke, retry loop),
  `RegistrationContext` (attachment precedence + User-Agent family classifier, raw UA never stored),
  `IRequestUserAgentAccessor` port + `HttpRequestUserAgentAccessor`; the InteractiveServer
  forwarding handler now copies User-Agent across the loopback hop.
- **Infrastructure**: EF mapping for `Nickname` + three private scalar columns
  (`RegisteredAttachment`/`RegisteredBrowser`/`RegisteredOs`), record + fingerprint ignored;
  migration `20260923063306_AddCredentialNicknameAndRegisteredWith` (postgres-gated tree).
- **SPA**: `web-authn.ts` returns `authenticatorAttachment` + `transports`; `CredentialsState`
  gains `RenameCredential`, `SetPendingNickname`, `ClearPendingNickname` and pending-nickname
  fields; `CredentialRowPresenter` (pure text rules); `CredentialList` row = nickname title,
  provider · attachment · browser/OS line, created, monospace fingerprint, inline Rename editor
  (auto-opens prefilled with the provider name for a just-added passkey), two-step revoke with a
  restating confirmation. Settings and Passkeys pages wired; `AddPasskeyPrompt` shows a
  "Name this passkey" form after its own CTA. Rename outcomes go to the shell notification region.
- **Agent CLI**: wire DTO field renamed to `Nickname`; `--label` option maps onto it.
- **Tests**: identity lib (`credential-nickname-tests.cs`), co-located
  `rename-credential-tests.cs` (9) and `registration-context-tests.cs` (9), integration
  `credential-rename-tests.cs` (cookie + bearer happy path, oversize 400, cross-principal and
  unknown id return identical 404 bodies), `credential-add-tests.cs` registration-context +
  fingerprint capture, contracts round-trips for the new shapes (fingerprint on the wire,
  handle/material still never), EF model mapping assertions, SPA `credential-row-presenter-tests.cs`.

### Gates (run 2026-09-23 in the claim worktree)

- `ganda repo audit` — passes all checks.
- `dev build` — 0 warnings / 0 errors.
- `dev test` — every suite green (web-jaribu aggregator 195, web-server-integration 248,
  web-spa-integration 46, timewarp-identity 227, contracts 44, infrastructure 56, …).
- `dev template-smoke` — SmokeDefault, SmokeNoPostgres, SmokeNoApi all OK; tiers 1–3 passed.
- `dev check-version` — source 2.0.0-beta.20 is already ahead of NuGet 2.0.0-beta.19; no bump needed.

### How to validate

**Smoke**

```bash
./bin/dev build
dotnet run source/container-apps/web/features/identity/rename-credential/rename-credential-tests.cs
dotnet run source/container-apps/web/features/identity/registration-context-tests.cs
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Credential
```

Then `dev run`, sign in with a passkey, open **Settings**, click **Create a passkey** and
complete the browser ceremony.

**Expect**

- Build 0/0; the two runfiles report 9/9 and 9/9; the filtered integration run reports all
  `Credential*` classes passed (includes `CredentialRename_` and the new
  `Captures_Registration_Context_And_Fingerprint…` fact).
- On Settings the new row opens an inline nickname editor prefilled with the provider name
  (e.g. "Proton Pass"); Save shows a shell message bar "Nickname saved." and the row title becomes
  the nickname. The row's second line reads `Provider · Built-in|Roaming · Browser on OS`, followed
  by "Created …" and an 8-hex monospace fingerprint. Clicking **Revoke** shows a confirmation that
  restates the nickname, provider, created stamp and fingerprint; only **Confirm revoke** revokes.
  With a single active credential both **Revoke** and the confirm step are disabled and the row
  shows "Add another passkey or agent key before revoking this one." (task 246).
- `GET /api/identity/credentials` JSON contains `nickname`, `registeredWith`, `fingerprint` and
  never `handle` / `publicMaterial` / a raw User-Agent string.

### Notes for reviewers

- Nickname prompt timing: the provider name is only known after the ceremony (AAGUID parsed
  server-side), so the prompt is the inline rename editor opened on the new row, prefilled from
  `AddPasskey.Response.ProviderLabel` — one mechanism serves add-time naming and later renames.
- Attachment/transports are client-asserted display hints, never security signals.
- Task 247 has not landed; only the NEW outcomes (rename) route to the shell region. The
  pre-existing page-local success/error bars on Settings/Passkeys are 247's scope and untouched.
- The EF snapshot diff reorders `RolePermissionGrant` (tool output ordering); no schema change there.

### Fix loop results (2026-09-23) — merge origin/master (task 246)

- `git merge origin/master` (no rebase, no squash) — four conflicts resolved by hand:
  `CredentialList.razor`, `SettingsPage.razor`, `SettingsPage.razor.cs`, `PasskeysPage.razor`.
- **Both land:** 246's `Revoke*` parameter names (`RevokeLabel`, `RevokeDataQa`, `RevokeDisabled`,
  `RevokeDisabledHint`, `RevokeDisabledHintDataQa`, `OnRevoke`), default data-qa `RevokePasskey`,
  the `CanRevoke` binding + last-credential hint on Settings and Passkeys; AND this task's row
  (nickname title, context line, fingerprint, inline rename, two-step restating confirmation).
  The final confirm button raises `OnRevoke`; `RevokeDisabled` disables BOTH the first step and
  the confirm button; the confirm button's data-qa is `{RevokeDataQa}Confirm` and its text is
  "Confirm revoke" / "Confirm unlink".
- 246's new `credentials-spa-test-application.cs` constructed `CredentialSummary` with the
  pre-248 shape — updated to the 9-arg constructor (nickname, RegisteredWith, fingerprint).
- `credential-list-render-tests.cs`: `DeletePasskey` → `RevokePasskey`; new fact
  `Revoke_Disabled_Disables_First_Step_And_Shows_Hint` pins the 246 guard on the two-step row.
- Design regions reconciled (CredentialList, SettingsPage.razor.cs).
- **Gates (foreground, this worktree, after the merge):** `dev build` 0/0 · `ganda repo audit`
  clean · `dev test` all 21 suites green (web-server-integration 251 + 1 pre-existing manual
  skip, web-spa-integration 55 incl. 246's revoke-guard tests and the deep-link `RevokePasskey`
  + hint facts, web-jaribu 195, timewarp-identity 228) · `dev template-smoke` SmokeDefault,
  SmokeNoPostgres, SmokeNoApi OK.
- **CI on the pushed merge commit `3aeed9b1`:** workflow run 35833367252 (pull_request trigger)
  completed **success** — detect-paths, ci, template-smoke all green (Lint skill specs skipped
  by path filter). PR #397 reports MERGEABLE; the earlier "no workflow runs" state was because
  the previous pushes never produced a run — this push did.

### Review disposition (tw-implementation-review, 2026-09-23)

- **Rounds / roster / effort:** 2 rounds, `general` only (effort 1) in each. Round 1 = full
  branch diff; round 2 = re-review of the merge-master fix loop (merge commit `3aeed9b1`, task 246
  conflicts) plus carry-forward of M1–M7.
- **Final counts:** bug 0 · suggestion 4 fixed · nit 2 fixed + 1 wontfix · **0 open**. Round 2
  raised no new findings; M1–M6 fixes verified intact after the merge.
- **Disposition:** `accepted-exceptions` — M7 (interactive click coverage for the inline editor /
  two-step revoke and the rename handler's state effects) deferred: needs bUnit or a Playwright
  flow, neither in the repo; adding a test dependency is a maintainer decision. Non-blocking.
- **Round-2 verification (reviewer + oracle spot-check):** `RevokeDisabled` gates both the
  first-step Revoke button and the Confirm button; Confirm raises `OnRevoke`; hint renders under
  `RevokeDisabledHintDataQa`; no `Delete*` residue or conflict markers; `CanRevoke` bound on
  Settings and Passkeys; 9-arg `CredentialSummary` construction in the SPA test application
  matches the contract.
- **Gates re-run by the review oracle after round 2:** `dev build` 0/0 · `web-spa-integration-tests`
  55/55. CI on PR #397 head `d7088bf6` was still running at disposition time (template-smoke and
  detect-paths already passed; `ci` pending); the earlier run on `3aeed9b1` was fully green.
- **Fixes landed on this task (review commit):** migration data step moving legacy agent-key
  `Label` → `Nickname` (M1); single owner for the pending nickname editor —
  `PendingNicknameOwnedByPrompt` / `PendingListRenameCredentialId` / `ClaimPendingNicknameForPrompt`
  so Settings never shows two editors (M2); auto-opened editor keeps its draft on a failed rename
  (M3); `credential-list-render-tests.cs` (M4); `Nickname_and_registered_with_round_trip_through_update`
  store-contract case on both fixtures (M5); `label` → `nickname` wire break documented on both
  agent-key contracts (M6).
- **Gates after fixes:** `dev build` 0/0 · `ganda repo audit` clean · timewarp-identity 228/228 ·
  web-infrastructure (real Postgres via `Migrate()`, verified not soft-skipped) 57/57 ·
  web-spa-integration 50/50.
- **Paths:** `review/review-framework.md` (rounds 1–2 scope), `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`,
  `review/disposition.md`.
