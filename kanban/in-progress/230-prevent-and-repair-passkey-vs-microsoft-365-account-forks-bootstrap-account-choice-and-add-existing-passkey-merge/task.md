# Prevent and repair passkey vs Microsoft 365 account forks: bootstrap account choice and add-existing-passkey merge

## Description

Scenario (Steve, 2026-09-16): a person registers a passkey (principal A). Later they click
"Continue with Microsoft 365"; nothing ties the token to A, bootstrap creates principal B.
Now A owns the passkey, B owns the Microsoft 365 account, and nothing can join them: from B,
"Create a passkey" makes a new passkey; from A, "Link Microsoft 365" is 409 because B owns the
handle. Two deliverables:

1. **Prevent** at the moment the fork would happen: an unknown Microsoft 365 account must not
   silently create a principal. Offer a choice.
2. **Repair**: "Add an existing passkey" on Settings — assert with the passkey registered under
   another principal; the server re-parents it and merges that principal into the current one.

Decisions locked in the cockpit discussion:
- Keep bootstrap. It is already a site setting (`EntraAllowBootstrap`). Crunchit runs with it
  **off** (staff are pre-synced; first sign-in is a sync-hit). The template keeps it **on**.
- A passkey cannot be re-bound by the authenticator, but the server can accept an assertion and
  re-parent the credential row. Passkey authentication already looks up by credential handle
  only (`complete-passkey-authentication-contracts.cs`: UserHandle intentionally unused), so the
  authenticator continuing to report A's user handle is harmless. Keep it that way.
- Merge proof is a real authentication of the other account (passkey assertion, or Microsoft 365
  sign-in for the mirror case). That makes merge exactly as strong as sign-in; no email matching,
  no admin-forced merge in this task.

## Requirements

### A. Bootstrap account choice (prevention)

- When the Entra ticket has a valid, trusted token whose handle is unknown and
  `EntraAllowBootstrap` is on, do **not** create a principal immediately. Park the validated
  claims server-side (short-lived, single-use, e.g. 10 min, keyed by an opaque id in a Secure
  HttpOnly cookie or the OIDC `AuthenticationProperties`) and redirect to a SPA page
  `/Login/Microsoft365/Choose` with two actions:
  - **Create a new account** → completes the original bootstrap (Principal.Create + EntraAccount +
    identity-session), same as today.
  - **I already have an account** → runs the passkey authentication ceremony; on success,
    attaches the parked Entra claims as an `EntraAccount` credential on **that** principal (link
    semantics, 409 if the handle is somehow owned elsewhere) and signs in.
  Parked claims expire; the choose page shows "session expired, sign in with Microsoft 365 again".
- Sync-hit (handle already owned) and link mode are unchanged. `AllowBootstrap` off → refuse as
  today; the choose page is never reached.
- Integration tests through the stub authority (`entra-oidc-handler-round-trip-tests.cs`
  pattern): unknown handle → 302 to choose page (no principal created); create → principal +
  credential + session; already-have → passkey assert → credential on existing principal.

### B. Add an existing passkey (repair / merge)

- Library (`TimeWarp.Identity`):
  - `Credential.ReparentTo(PrincipalId target)` (or store-level `ReparentCredentialAsync`) —
    Type/Handle stay immutable; `PrincipalId` changes once; version bump; EF update.
  - `Principal.MergeInto(PrincipalId target)`: marks the source retired with `MergedIntoPrincipalId`
    (new nullable property); `IsActive` false; cannot sign in; store `Update` respects version.
    Add a migration (`identity.principals.merged_into_principal_id`).
  - `IPrincipalStore.MergePrincipalAsync(source, target)` executing: re-parent all **active**
    credentials of source → target (revoked ones stay on source for audit), target trust tier =
    max(source, target), target `DisplayName` kept unless empty, source `MergeInto(target)`.
    In-memory and EF implementations; EF in one transaction. Tests for both stores incl.
    concurrency conflict.
- Ceremony (web features/identity, `add-existing-passkey/`): authenticated (identity-session)
  start → issues a WebAuthn assertion challenge (reuse `StartPasskeyAuthentication` machinery
  scoped to "merge"); complete → verify assertion → credential found by handle belongs to
  principal S ≠ current C → if S is active and not merged, `MergePrincipalAsync(S, C)`; if the
  credential already belongs to C → 409 `Already on this account`; if S is merged/quarantined →
  403. Emits an audit log line (both ids, credential id).
- Mirror case (signed in via passkey on A, want B's Microsoft 365): "Link Microsoft 365" on A
  currently 409s because B owns it. Change: when the link ticket's handle is owned by another
  **active, unmerged** principal B, complete the merge B → A (the Entra sign-in just performed
  is the proof of B) instead of 409. Keep 409 only when the handle's owner is the current
  principal (already linked) or merged/quarantined. Document in the ticket processor Design
  region.
- SPA Settings: "Add an existing passkey" next to "Create a passkey" (both under Passkeys). On
  success refresh credentials and show "Merged account: N credential(s) moved". After 229 lands
  the Microsoft 365 card rules apply to the merged result.
- Tests: ceremony start/complete happy path; wrong principal cases (409/403); merge moves
  credentials and retires source; retired source cannot authenticate (passkey login on a
  credential now owned by target signs in as target; a stale session cookie for the retired
  principal is rejected on next request).

### Out of scope

- Multi-tenant, admin-forced merges, un-merge, profile data merge beyond DisplayName/trust tier.

## Depends on

- 229

## Checklist

- [x] A: parked claims + `/Login/Microsoft365/Choose` + create / already-have flows + tests
- [x] B: `ReparentTo` / `MergeInto` / `MergePrincipalAsync` (in-memory + EF + migration) + tests
- [x] B: add-existing-passkey ceremony + Settings CTA + tests
- [x] B: link-mode merge (mirror case) replaces the cross-principal 409 + tests
- [x] Docs: `auth.md` (choose page, add existing passkey, merge semantics), library `overview.md`,
      Design regions
- [x] `dotnet test -- --filter-class Entra` / passkey suites green; `dev build` 0/0;
      `ganda repo audit` clean; `dev template-smoke` passes
- [x] Results and How to validate (manual: passkey account → M365 bootstrap → choose "already have"
      → one principal; fork on purpose → Add existing passkey → merged)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok 4.6 session 01a0aa88-000a-7040-8602-50e8888f9c83 (2026-09-16)
- Review oracle: Grok 4.6 session 01a0aaa1-ad94-7c02-a5b8-221bcb93ca2f (2026-09-16)
- Review general round 1: Grok session 01a0aaa3-bdc5-72a0-ac43-4dd754d4b4cf (2026-09-16)
- Review general round 2: Grok session 01a0aab4-5926-7271-aba5-8483d910279b (2026-09-16)
- Review general round 3: Grok session 01a0aab8-a523-7d13-9bbb-50282b9cba99 (2026-09-16)

## Notes

- Files: `source/libraries/timewarp-identity/{principals/principal.cs,credentials/credential.cs,persistence/*}`,
  `source/container-apps/web/features/identity/entra-ticket-processor-application.cs`
  (bootstrap create ~L213; sync-hit ~L168/193), `complete-passkey-authentication/*`,
  `start-passkey-authentication/*`, `add-passkey/*`, `challenge-entra/*`,
  `web-spa/features/identity/pages/login-page/*`, `web-spa/features/application/pages/SettingsPage.razor`,
  `web-spa/features/identity/credentials-state/*`, postgres migrations.
- Prior: 104-003/005 (passkey ceremonies, multi-credential), 219-002 (bootstrap/link/sync-hit),
  227 (single tenant), 229 (card rules). RFC 219 fork 1 (bootstrap-capable) stays locked; this
  task adds the choice step and the repair, it does not remove bootstrap.
- Size: expect the implementer to hit the turn budget; re-dispatch resumes from the worktree.

## Results

Unknown-handle Microsoft 365 bootstrap no longer mints a principal. Validated claims are parked
(10 min, single-use, HttpOnly `.Tw.EntraChoice`) and the visitor is redirected to
`/Login/Microsoft365/Choose`: **Create a new account** runs the original bootstrap mint;
**I already have an account** asserts a passkey and attaches the parked EntraAccount onto that
principal. Sync-hit and `AllowBootstrap` off are unchanged.

Repair: Settings **Add an existing passkey** issues a Merge-scoped WebAuthn assertion; on success
`MergePrincipalAsync` re-parents active credentials and retires the source. Link Microsoft 365
merges when the handle is owned by another active unmerged principal (Entra sign-in is the proof).
409 remains for already-on-this-account; 403 for merged/quarantined sources; 229 one-Entra-per-principal
still 409s a second handle.

**Files (product):** `Credential.ReparentTo`, `Principal.MergeInto` / `MergedIntoPrincipalId`,
`IPrincipalStore.MergePrincipalAsync` (in-memory + EF, migration
`20260916200000_AddPrincipalMergedIntoPrincipalId`), parked-claims store + choose contracts
(`get-entra-bootstrap-choice`, `complete-entra-bootstrap-create`, `complete-entra-bootstrap-existing`),
`start-add-existing-passkey` / `complete-add-existing-passkey`, Settings CTA, `ChooseMicrosoft365Page`,
`auth.md`, library `overview.md`.

**Decisions:** keep bootstrap (choice step only). Passkey lookup stays handle-only. Merge proof is a
real authentication. 229 card rules apply after merge. Same-handle link of an already-owned Entra
is 409 `Already on this account` (task 230), not 229's idempotent success.

**Tests:** Entra filter 69/69 (includes revoked-handle link 403); AddExisting 6/6; Passkey 23/23;
Merge library tests green; Credentials contract 13/13 in-memory and EF (includes
`Update_rejects_PrincipalId_reparent`). `dotnet run tools/dev-cli/dev.cs -- build` 0/0.
`ganda repo audit` passes (2 advisory warnings: memsearch hooks, vscode peacock — pre-existing).
`dev template-smoke` SUCCEEDED.

### How to validate

**Automated**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra
# expect: all passed (unknown-handle 302 to /Login/Microsoft365/Choose, create mints principal+session,
#         link foreign handle merges, revoked foreign handle 403 without merge, already-on-this-account 409)

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class AddExisting
# expect: merge moves credentials and retires source; passkey login on moved credential is the
#         target principal; stale source cookie is unauthenticated; 409 already on this account;
#         403 quarantined/merged source; choose-existing attaches Entra to the passkey principal

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Passkey
# expect: all passed

cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class Merge
# expect: all passed (re-parent active creds, trust max, concurrency conflict)

dotnet run tools/dev-cli/dev.cs -- build
# expect: Build completed successfully, 0 Warning(s) 0 Error(s)
```

**Manual smoke**

1. Create a passkey account on `/Login` (principal A). Sign out.
2. Click **Continue with Microsoft 365** with a tenant user that is not yet linked.
   - Expect: redirect to `/Login/Microsoft365/Choose`, no second principal yet.
3. Click **I already have an account** and assert A's passkey.
   - Expect: signed in as A; Settings shows that Microsoft 365 under the same account.
4. (Fork repair) If two principals already exist, sign in as B, Settings → **Add an existing passkey**,
   assert A's passkey.
   - Expect: success bar `Merged account: N credential(s) moved`; A's passkey listed on B;
     signing in with that passkey authenticates as B.

**Depends on:** Entra enabled (`dev entra setup` + Admin Authentication AllowBootstrap on) for
steps 2–3. Isolated tests use the stub authority / software authenticator.

**Not in scope:** live hardware WebAuthn, admin-forced merge, un-merge, multi-tenant.

### Review disposition

- **Outcome:** clean
- **Rounds:** 3
- **Effort / roster:** 1, general only
- **Final counts:** bug 0/3/0, suggestion 0/2/0, nit 0/0/0 (open/fixed/wontfix)
- **Fixes on this id:** M1 revoked Entra link no longer merges; M2 choose-create notifies identity-session; M3 UpdateCredentialAsync rejects PrincipalId re-parent; M4 OnValidatePrincipal signs out stale cookie; M5 Jaribu wrappers for the re-parent contract test
- **Wontfix / escalations:** none
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/round-2/general.md`
  - `review/round-2/merged.md`
  - `review/round-3/general.md`
  - `review/round-3/merged.md`
  - `review/disposition.md`
