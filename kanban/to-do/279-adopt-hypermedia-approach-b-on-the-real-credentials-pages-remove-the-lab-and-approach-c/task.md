# Adopt hypermedia approach B on the real Credentials pages; remove the lab and approach C

## Description

Steve decided on 2026-10-05, after reviewing task 275's evaluation (PR #430, merged), to **adopt
approach B**: server responses name catalog actions with arguments. The client runs them through
the action catalog and shows them as buttons and as Ctrl-K contextual rows. **C is rejected.** It
bypassed the domain actions and left `CredentialsState` stale, which conflicts with TimeWarp.State's
client-owned state and subscriptions, and its only allow-list was "same origin", so a response
could aim the user's token at any route in the app.

This task makes B real on the Credentials surfaces and removes the evaluation scaffolding. Read
task 275's "B vs C comparison" (in `kanban/done/275-hypermedia/task.md`) first.

### What 275 left on master

- **Keep:**
  - Identity's `CredentialRules` (`credential-rules-application.cs`: `CanRevoke`,
    `HoldsMicrosoft365`, `CanLinkMicrosoft365`), shared by `RevokeCredential.Handler` and
    `EntraTicketProcessor`;
  - the Ctrl-K context hook (`ICommandPaletteContextSource`, `CommandPaletteContext`, the
    contextual row kind, `RequiresInput`, the `Open` append);
  - the JSON → args binder (`ContextualActionArguments`).
- **Lab, to be removed:**
  - server: `features/hypermedia-lab/` (`get-credential-offers`, `get-credential-commands`,
    `lab-credential-snapshot-application.cs`);
  - SPA: `web-spa/features/hypermedia-lab/` (state, page, context source, rows,
    `app-relative-href.cs`, `followed-link-request.cs`);
  - their tests, and the NavMenu entry.
- **Client-side copies of server rules, to be removed:**
  - `CredentialsState.CanUnlink` / `ActiveCredentialCount` / `CanLinkMicrosoft365`
    (`credentials-state.cs`, `credentials-state.link-microsoft-365.cs`);
  - their use in `SettingsPage.razor(.cs)`, `PasskeysPage.razor` and `CredentialList.razor`.

## Requirements

1. **Server offers on the real contract.** The credentials read the Settings and Passkeys pages
   already use, `GetCredentials` or a sibling decided here and recorded, returns the **offered
   actions** per credential and page-level (Revoke, Rename, Link Microsoft 365, Add passkey where
   applicable) as `{ name, label, args }`.
   - The offers are computed from `CredentialRules` on the server.
   - Names come from `OfferedActionNames` constants, and a test pins that every emitted name
     resolves in the SPA `IActionCatalog`.
   - Contract house rules apply: serialization round-trips, mock factory, and an update to the
     existing round-trip tests.
2. **Client renders and runs offers through B.**
   - Buttons on Settings and Passkeys come from the offers in `CredentialsState`, not from
     client-computed `CanUnlink` / `CanLinkMicrosoft365`.
   - Running an offer goes through the catalog (`Find` → bind args → `Execute`), using the real
     `Credentials.*` actions with their notifications and `TrackAction`.
   - The follow-up refresh comes from the caller or runner, never a handler dispatching (TWS0002).
   - Delete the client-side rule copies and their tests. Update the Design regions that described
     them, including the "mirrors RevokeCredential.Handler" note.
3. **Ctrl-K:** the Settings and Passkeys pages contribute contextual rows through the existing
   context hook. Rows that need input, such as Rename's nickname, stay page-only, as in 275.
4. **Close 275's review M4.** Before `Execute`, check that a contextual or offered action's
   catalog entry has Visibility Human or Both and that its Permissions pass `IAuthorizationService`,
   **or** check it against an explicit offerable-names list. Choose one and record why. Unknown
   names, failed binding and failed checks are refused with a notification.
5. **Remove the lab:** delete the server and SPA lab slices, approach C (`FollowCommand`,
   `FollowedLinkRequest`, `AppRelativeHref`, the `Ignored` response type, and any C-specific
   generator workaround), the lab tests and the NavMenu or registry entry. No `/HypermediaLab`
   route remains.
6. **Optional, record either way:** consider a generator or analyzer that ties server offer
   argument names to the target action's constructor parameters at compile time; 275 noted that
   gap. File it as a follow-up if it isn't done here.
7. **Skill:** a short section in the owning SPA skill (`tw-blazor` or wherever state and actions
   are documented) describing the pattern: "server offers catalog actions; the client runs them
   through the catalog; never compute validity client-side when the server can offer it". Skills
   are public: write the rule and reasoning only.

## Checklist

- [x] Real credentials contract carries offers from `CredentialRules`; name-resolution test
- [x] Settings and Passkeys render and run offers through the catalog; rule copies deleted
- [x] Ctrl-K contextual rows on Settings and Passkeys
- [x] M4: Visibility/Permissions (or offerable-names) check before Execute; fail closed
- [x] Lab + approach C removed (server, SPA, tests, nav); no `/HypermediaLab`
- [x] Compile-time arg-name agreement: done or follow-up filed (recorded as a follow-up in Notes; not filed from the walk, see Notes)
- [x] Skill section; Purpose/Design regions reconciled
- [x] Tests:
      - offers match the server rules (one credential → no Revoke; two → Revoke; Microsoft 365
        offered only when allowed);
      - running an offer runs the real action;
      - a non-offered action can't be run;
      - the M4 check refuses hidden or unpermitted entries
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [x] Do **not** start an AppHost; record the browser check as not performed
- [x] Implementation review (disposition: accepted-exceptions); host `open-pr`

## Notes

- Not pursued: server-driven state, either Blazor Server rendering with TimeWarp.State on the
  server, or "C plus store slices" from a server-side store. It was discussed on 2026-10-05 as a
  possible future timewarp-state design question. It is not part of this task.
- Known 275 coverage gap: Microsoft 365 linking is only tested in the not-offered case, because
  the test server has Entra off. Close it if the test host can enable Entra; otherwise record it.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

### Decisions (task 279)

- **Offers ride on `GetCredentials`, not a sibling read.** Settings and Passkeys already load it, and
  the offers describe exactly those rows. One round trip means the list and its actions always come
  from the same snapshot. `GetCredentials.Response` gains `Offers`. The shape is `OfferedAction
  { name, label, subject, arguments }` in `features/identity/offered-action-contracts.cs`, with the
  `OfferedActionNames` constants. It moves to a shared contracts tier when a second slice offers
  actions.
- **Server computation.** `CredentialOffers.For` (Identity Application) applies `CredentialRules`:
  - Rename for every active credential;
  - Revoke while more than one credential of any type is active;
  - Link Microsoft 365 (page-level) while `EntraSignInOffer` says Microsoft 365 is offered and none
    is linked.
  `EntraSignInOffer` is the one copy of the "offered" rule, now shared with `GetEntraSignInOffered`.
- **Add passkey is not offered.** No server rule gates it, so an offer would carry no information.
  Its buttons stay static.
- **Agents get the same offers.** Their catalog visibility applies; Link Microsoft 365 is a human-only
  ceremony.
- **M4: check the catalog's Visibility and Permissions, not an offerable-names list.** Before
  `Execute`, `CommandPaletteContext.RefusalAsync` requires the entry to be Human or Both and every
  `Permission` to pass `IAuthorizationService`. These are the same metadata and policies the static
  roster uses. Reasons:
  - the catalog already declares who may run each action, so a second list would be one more thing
    to keep in agreement;
  - an agent surface (task 271) can apply the same check from the same metadata.
  For this, `RevokeCredential` and `RenameCredential` changed from Visibility Agent to **Both**. The
  static roster still excludes them, because they have required parameters.
- **Follow-up refresh.** `FetchCredentials` is now cataloged (Agent, parameterless) as every offer
  row's `FollowUpTarget`. The runner sequences it, so handlers still never dispatch.
- **Duplicate Ctrl-K row.** On Settings, the offered Link Microsoft 365 row replaces the static
  `Credentials: Link Microsoft 365` command, which other pages still list. `Open` drops static
  Command rows whose Target the page contributes.
- **Follow-up, not filed from this walk:** move `ContextualActionArguments` into TimeWarp.State, and
  add a generator or analyzer that ties server offer argument names (`OfferedActionNames.CredentialIdArgument`)
  to the target action's constructor parameters at compile time. The server and the SPA are separate
  compilations, so this needs a shared manifest or a cross-project check. A web-spa test pins the
  names at run time today. It was not filed because `ganda kanban create` would claim a second
  worktree in the middle of this walk. **The cockpit should file it** (timewarp-state and/or this repo).
- **Microsoft 365 coverage gap (from 275).**
  - Closed host-free: `CredentialOffers_For_` covers the "offered" side, including the
    revoked-Entra case.
  - Closed end to end: the in-proc host starts with Entra off, and `GetCredentialsOffers_Returns_`
    turns it on per test (toggle-and-restore, as in protected-page-deep-link-tests) to cover
    "offered, not linked" (Link offered) and "offered, active account linked" (no Link), besides
    "not offered".
  - SPA: the offered Link row runs the real `LinkMicrosoft365` navigation.

## Session

- Created: 2026-10-05 (cockpit, per Steve's B decision on 275)
- 2026-10-05: implement oracle (headless, Claude Opus 5.5).
  - Deleted the lab (server, SPA, tests, NavMenu, `_Imports`).
  - Moved offers onto `GetCredentials`; added the M4 gate.
  - Rewired Settings and Passkeys.
  - Added the skill section.
  - First `dev test` caught a regression of mine: `EntraSignInOffer` short-circuited before the store
    read, which skipped seed-on-read. Fixed; all suites green.

- 2026-10-05: review oracle (headless, Claude Opus 5.5).
  - Effort 3: general, tests and security reviewers in round 1; round 2 was a re-verification.
  - Fixed 9 findings on this id. Disposition is accepted-exceptions.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-05T07:01:05Z

## Results

**Delivered**
- **Server.** `GetCredentials.Response.Offers` (`OfferedAction`, `OfferedActionNames`), computed by
  `CredentialOffers.For` from `CredentialRules` plus `EntraSignInOffer`. The mock factory carries
  offers.
- **SPA state and pages.**
  - `CredentialsState.Offers` / `FindOffer` / `IsOffered` hold the offers as `CredentialOffer`
    string records, which are clone-safe.
  - Settings and Passkeys bind `CredentialList.IsRevokeOffered` / `IsRenameOffered` and show Link
    only when offered.
  - Clicks go through `CredentialOfferRows.RunAsync` → `CommandPaletteRunner.RunContextualAsync`:
    still offered → known entry → Visibility/Permissions (M4) → bind → `Execute` → `FetchCredentials`
    follow-up.
- **Ctrl-K.** `CredentialsContextSource` contributes rows on `/Settings` (passkeys, Microsoft 365
  rows, and the page-level Link) and on `/Passkeys` (passkey rows only). Rename rows stay
  button-only.
- **Removed.**
  - `CredentialsState.CanUnlink` / `ActiveCredentialCount` / `CanLinkMicrosoft365` and their tests
    (`credentials-state-revoke-guard-tests.cs`, and the Settings formula tests).
  - The whole hypermedia lab: B and C, `FollowCommand`, `FollowedLinkRequest`, `AppRelativeHref`,
    `Ignored`, the lab tests and the NavMenu "Labs" entry. `git grep HypermediaLab` over
    source/tests is empty, apart from this decision trail.
- **Docs.**
  - New section in the `tw-blazor` skill: "Server-offered actions".
  - Design regions reconciled: CredentialRules, CredentialsState and its Fetch/Revoke/Rename/Link
    actions, CredentialList, the Settings and Passkeys pages, the palette context/runner/open, the
    binder, and the protected-page tests.

**Gates (2026-10-05, this worktree)**
- `dev build`: 0 warnings, 0 errors.
- `dev test`: every suite passed. web-server-integration 285 passed plus the always-skipped
  `RunForever`; web-spa-integration 144; web-contracts 47; web-jaribu 227.
- `dev template-smoke`: SUCCEEDED.
- `ganda repo audit`: passes all checks.
- `dev check-version`: new version.
- **Not performed:** a browser check. No AppHost was started, as this task required.

### How to validate

Smoke:

```bash
./bin/dev build
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class CredentialOffers
cd ../web-server-integration-tests && dotnet test -c Release -- --filter-class Offers
cd ../web-server-integration-tests && dotnet test -c Release -- --filter-class ProtectedPageDeepLink
cd ../web-contracts-tests && dotnet test -c Release
git grep -n "HypermediaLab\|CanUnlink\|ActiveCredentialCount" -- source tests
```

Expect:
- Build 0/0.
- SPA `CredentialOffers` 15/15:
  - every offered name resolves to a Human/Both catalog entry, and Rename's unbound parameter is
    `nickname`;
  - the offered Revoke runs the real action ("Credential revoked."), then the follow-up fetch drops
    Revoke;
  - Rename runs with the typed nickname ("Nickname saved.");
  - input cannot override `credentialId`;
  - a non-offered or forged Revoke is refused, and nothing is sent;
  - an unknown name and bad arguments are refused;
  - M4: an Agent-only entry and an unpermitted principal are refused;
  - rows appear on Settings (3 Revoke rows with an Entra account) and Passkeys (2), and none on
    `/Counter`;
  - the offered Link replaces the static command on Settings and navigates to the challenge.
- Server `Offers` 9/9:
  - the rule table, including Microsoft 365 offered/linked/revoked;
  - end to end: one credential → no Revoke; two → Revoke on both; a real revoke drops it; Entra off
    → no Link.
- `ProtectedPageDeepLink` passes: the prerendered Revoke/Unlink hints come from the server's offers.
- Contracts 47/47, including the `GetCredentials` offers round-trip.
- `git grep` returns nothing. It is case-sensitive on purpose: the server rule's `activeCredentialCount` parameter is expected and is not matched.

Optional manual check (not performed here): run `dev run` and sign in with two passkeys. On
`/Settings`, revoke one and watch Revoke disable with its hint. Press Ctrl-K on `/Settings`: there
are "Credentials: Revoke · …" rows and no Rename rows.

### Implementation review

- **Rounds:** 2. **Roster:** general, tests and security in round 1, at effort 3 (by-diff budget). Round 2 was a general re-verification.
- **Final counts:**
  - bug: 0
  - suggestion: 5 fixed
  - nit: 4 fixed and 2 wontfix
  - open: 0
- **Disposition:** `accepted-exceptions`. Two nits are wontfix:
  - M3: the Link follow-up fetch is harmless;
  - M11: the SPA accepts any permitted catalog action a server offer names, which is the recorded M4 decision.
- **Fixes:**
  - Design-region accuracy (Ctrl-K Link row on /Settings; the AddPasskeyPrompt exemption from the offer runner);
  - the SPA scripted BFF calls the real `CredentialOffers.For`;
  - new tests: API-level Link Microsoft 365 offered/linked, server offers contradicting the count, and the unauthenticated M4 refusal;
  - stronger cross-page refusal and mock round-trip assertions.
- **Re-run after the fixes:**
  - `dev build`: 0 errors;
  - web-spa-integration: 146/146;
  - web-server-integration: 287 passed, plus the 1 always-skipped test;
  - web-contracts: 47/47;
  - `ganda repo audit`: passes.
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/{general,tests,security,merged}.md`
  - `review/round-2/merged.md`
  - `review/disposition.md`
