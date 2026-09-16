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

- [ ] A: parked claims + `/Login/Microsoft365/Choose` + create / already-have flows + tests
- [ ] B: `ReparentTo` / `MergeInto` / `MergePrincipalAsync` (in-memory + EF + migration) + tests
- [ ] B: add-existing-passkey ceremony + Settings CTA + tests
- [ ] B: link-mode merge (mirror case) replaces the cross-principal 409 + tests
- [ ] Docs: `auth.md` (choose page, add existing passkey, merge semantics), library `overview.md`,
      Design regions
- [ ] `dotnet test -- --filter-class Entra` / passkey suites green; `dev build` 0/0;
      `ganda repo audit` clean; `dev template-smoke` passes
- [ ] Results and How to validate (manual: passkey account → M365 bootstrap → choose "already have"
      → one principal; fork on purpose → Add existing passkey → merged)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

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

_Pending._

### How to validate

_Pending._
