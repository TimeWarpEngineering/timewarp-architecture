# Research Entra ID as a linked credential on passkey principals plus tenant user sync

## Description

Research task. Decide how `TimeWarp.Identity` extends a passkey-first `Principal` with a
**linked Microsoft Entra ID identity**, and how a downstream product pre-provisions
principals by **syncing users from an existing Entra workforce tenant**. Passkeys stay the
base authentication mechanism (program lock #1, #9); Entra is an extension of a principal,
never the center (program lock #10, non-goal "Entra External ID as primary identity").

The first consumer is **crunchit** (Crunchitfs). Its staff live in the `crunchitfs.com`
M365 tenant (tenant id `30f3971f-4719-4f20-9b6f-88916e0b95bd`). Bill's portal mockup
(`crunchit/documentation/design/crunchit-portal.html`) shows the expected functionality:
a Staff list synced from M365 (display name, title, office, department), accounts with no
Title filtered out (shared mailboxes / service accounts), portal-owned role / status /
assignments / per-user financials override / hidden flag, "Add staff manually" with an
invite token, "Sync now" with last-synced freshness, and "Continue with Microsoft 365" as
the sign-in button. We are the architects: the mockup is the expected *functionality*, not
a binding design. Output of this task is an RFC that picks the design, then child
implementation tasks.

## Parent

None (new program thread; follows 104 identity program).

## What exists today

### timewarp-architecture (`TimeWarp.Identity`, `source/libraries/timewarp-identity/`)

- `Principal : Entity<PrincipalId>` — `Kind` (Human/Agent/Service), `TrustTier`
  (Provisional → Keyed → Funded → Established), `IsQuarantined`, `DisplayName`, `Version`.
  `RecordCredentialAttached()` promotes Provisional → Keyed on first credential.
- `Credential : Entity<CredentialId>` — `PrincipalId` (1:N), `CredentialType`
  (**`None/Passkey/AgentKey` only**), `Handle` (lookup key), `PublicMaterial`
  (verification material), `Label`, one-shot `Revoke()`. Type and Handle immutable.
- `IPrincipalStore` (`persistence/i-principal-store.cs`) with `FindCredentialByHandleAsync(type, handle)`,
  `AddCredentialAsync` (auto-promotes to Keyed). In-memory and EF/Postgres stores;
  EF has a unique index on `(Type, Handle)` and `Version` as concurrency token.
- Task 104-005 built the **"attach a new credential to the currently authenticated
  principal"** ceremony (`AddPasskey`, `AddAgentKey`, `GetCredentials`, `RevokeCredential`)
  behind the `credential-management` policy with ownership checks and last-active-credential
  protection. This is the seam for an Entra-linked credential.
- Entra/MSAL today (task 104-021) is a **separate, parallel auth scheme**:
  `Authentication:UseEntra` (default false) makes `AddMicrosoftIdentityWebAppAuthentication`
  the default scheme with `identity-session` as a second scheme. It never touches
  `Principal`/`Credential`. SPA `GetCurrentUser` is `[ClientOnlyContract]` mock-only.
  Packages already referenced: `Microsoft.Identity.Web 4.14.2`,
  `Microsoft.Authentication.WebAssembly.Msal 10.0.11`.
- No Microsoft Graph client, no batch import, no SCIM anywhere in the repo.

### crunchit (`Crunchitfs/crunchit`)

- Auth is entirely mock: SPA `mock-authentication-state-provider.cs` + `/signin` role picker;
  BFF endpoints are all `[EndpointAllowAnonymous]` "until Entra/BFF auth". Does **not**
  reference `TimeWarp.Identity`.
- Staff entity (`portal-mock-seed.cs`, `get-staff-contracts.cs`): StaffId, Name, Email,
  Title, Office, Department, Role (owner/admin/staff, app-local), AssignedClients,
  FinancialsOverride. Identifier is email; no Entra object id field.
- M365 features (`get-m365-integration`, `sync-m365`, `connect-m365`) exist but are simulated.
- Kanban task 008 "Plan Entra ID auth and role model" (to-do, blocked on hosting task 006)
  states "Staff live in Microsoft 365, so sign-in should be Entra ID" and leaves open:
  SWA built-in auth vs MSAL, roles from Entra groups vs app-assigned, per-user override
  storage, Graph consent model. Infra has no app registration, Key Vault, or managed identity.

## Entra facts to design against (verified 2026-09; links in Notes)

- Stable identity key is **`tid` + `oid`** (immutable within a tenant). `sub` is pairwise
  per app; `preferred_username` / `upn` / `email` are mutable and must never be a join key.
- Link flow: register Entra OIDC under a **named scheme** (not default), challenge it from an
  authenticated identity-session action with `prompt=select_account`, capture the external
  principal in `OnTicketReceived`, attach the credential to `HttpContext.User`'s principal,
  `HandleResponse()` and redirect. Do not let it sign in as primary.
- Tenant sync options: Graph **delta query** (`/users/delta`, `/groups/{id}/members/delta`,
  app-only `User.Read.All` + `GroupMember.Read.All`, cert or managed identity, honour 429
  `Retry-After`), SCIM push (Entra provisioning to an endpoint we host, ~40 min cadence,
  disable → `active:false`), Graph change notifications (payload-less wake-up), JIT at first
  sign-in (complements, does not replace pre-provisioning).
- Entra's own passkeys (FIDO2 and synced passkeys) are a separate credential store from
  our first-party WebAuthn; no interaction.
- Security: linking only from an authenticated session on the existing principal (no
  email auto-link); pin `tid`; Entra disable/delete must revoke our own sessions (relying
  party's job); sync-created principals need an onboarding path to their first passkey.

## Design forks the RFC must resolve

1. **Extension-only vs bootstrap-capable.** Is Entra linking only available to an already
   authenticated passkey principal, or does a first Entra sign-in find-or-create a principal
   by `tid:oid`? Bill's mockup and crunchit task 008 assume "Continue with Microsoft 365"
   bootstraps; program lock #1 says passkey first. Candidate: Entra bootstrap allowed only
   for a **pre-provisioned (synced) principal**, and the session is not "fully onboarded"
   until a passkey is registered in that same session.
2. **Credential shape.** New `CredentialType.EntraAccount` (or generic `ExternalIdentity`
   with provider in metadata)? `Handle` = UTF-8 `"{tid}:{oid}"` keeps the `(Type, Handle)`
   unique index and `FindCredentialByHandleAsync` working. `PublicMaterial` is documented as
   cryptographic verification material; decide whether to repurpose it as opaque metadata,
   leave it empty by convention, or add a separate `ExternalIdentity` entity/table
   (ASP.NET Identity `AspNetUserLogins` shape: LoginProvider + ProviderKey → PrincipalId).
3. **Trust tier semantics.** Does an Entra-only credential count as "Keyed"? A synced
   principal with no passkey yet: Provisional? New tier? Quarantine-style flag?
4. **Step-up.** Is a valid identity-session cookie sufficient authorization to attach an
   Entra credential, or must the passkey be re-asserted (mirror the 104-005 decision).
5. **Sync mechanism.** Graph delta pull (recommended by research) vs SCIM receiver vs JIT.
   Filtering rule (mockup: skip accounts without `jobTitle`). Field mapping:
   `displayName`, `jobTitle`, `officeLocation`, `department`, `mail`, `accountEnabled`.
   Group → role mapping or app-assigned roles only (crunchit leans app-assigned).
6. **Where the profile fields live.** `Principal.DisplayName` exists; title/office/department
   are product profile data. Progressive profile (104-024) vs product-owned staff table
   keyed by `PrincipalId`. Decide what `TimeWarp.Identity` owns vs what the product owns.
7. **Sync job placement.** web-server (human plane) vs api (agent plane) vs a new worker
   host, under the task 118 plane split and `tw-slice-isolation` rules. Scheduler,
   deltaLink persistence, idempotency, and audit trail.
8. **Onboarding a synced principal to its first passkey.** Invite/claim link (mockup's
   manual-add invite token generalised) vs Entra-bootstrap-then-upgrade. Manually-added
   staff (no Entra account yet) must also work and later reconcile to a synced record.
9. **Revocation.** Entra `accountEnabled=false` or deletion → revoke Entra credential, and
   product decides whether the principal's passkey sessions are also revoked.
10. **Retire or rewire the dormant `UseEntra` scheme** (104-021 shape) so there is one
    Entra story in the template, not two.

## Deliverables

- `rfc/rfc.md` in this task folder (use `tw-rfc-ballot` for forks 1, 2, 5, 7; the rest can
  be resolved in prose) with a decision per fork and the rationale.
- A proposed `TimeWarp.Identity` public surface delta (types, store port methods, ceremony
  endpoints) and a sequence diagram for link, bootstrap, sync, and revoke.
- Child task list for implementation, split by repo:
  timewarp-architecture (library + template) and crunchit (Graph sync, staff slice, infra
  app registration + Graph permissions, replacing mock auth).
- Update crunchit task 008 to depend on this task's outcome.

## Checklist

- [ ] Read program locks in `kanban/done/104-agent-ready-identity-and-x402-program/task.md`
      and 104-005, 104-021, 104-024 task bodies
- [ ] Read Bill's mockup `crunchit/documentation/design/crunchit-portal.html` (Staff page,
      Settings M365 card, sign-in) and crunchit task 008
- [ ] Verify Microsoft.Identity.Web 4.x link-not-sign-in pattern against a current sample
- [ ] Prototype spike (throwaway, not merged): named Entra scheme challenge from an
      identity-session action, capture `tid`/`oid`, attach `Credential`
- [ ] Prototype spike: Graph `/users/delta` with `$select` and `$filter` for `jobTitle`,
      app-only auth via certificate, deltaLink round trip
- [ ] Write `rfc/rfc.md`; run rfc-ballot on the four contested forks
- [ ] Create child implementation tasks (architecture + crunchit) from the RFC
- [ ] Results and How to validate

## Session

- Created: 3376783 (2026-09-14)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Prior art in this repo: 097-001 (hybrid server id + keys, no Entra center), 098-008
  (flag off Entra/MSAL without breaking optional enterprise later), 104-005 (multi-credential
  attach/list/revoke), 104-021 (UseEntra non-default), 104-024 (progressive profile),
  104-032 (EF principal store), 132 (sign-in entry points kept distinct).
- Entra references:
  - ID token claims: https://learn.microsoft.com/en-us/entra/identity-platform/id-token-claims-reference
  - OIDC in ASP.NET Core 10: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-10.0
  - Delta query users/groups: https://learn.microsoft.com/en-us/graph/delta-query-users ,
    https://learn.microsoft.com/en-us/graph/delta-query-groups ,
    https://learn.microsoft.com/en-us/graph/delta-query-overview
  - SCIM provisioning: https://learn.microsoft.com/en-us/entra/identity/app-provisioning/use-scim-to-provision-users-and-groups
  - Change notifications: https://learn.microsoft.com/en-us/graph/change-notifications-overview
  - Least-privilege Graph permissions: https://learn.microsoft.com/en-us/graph/best-practices-graph-permission
  - Revoke user access: https://learn.microsoft.com/en-us/entra/identity/users/users-revoke-access
  - Entra passkeys: https://learn.microsoft.com/en-us/entra/identity/authentication/concept-authentication-passkeys-fido2
- Packages (nuget.org, 2026-09): Microsoft.Identity.Web 4.14.2 (already in
  `Directory.Packages.props`), Microsoft.Graph 5.25.0 + Azure.Identity,
  Microsoft.AspNetCore.Authentication.OpenIdConnect ships in the .NET 10 shared framework.
  ADAL and Azure AD Graph are retired; do not use.
- Unverified items from research: exact `OnTicketReceived` + `HandleResponse()` link idiom
  (community pattern, confirm against a current Identity.Web sample), MSAL.NET current
  version, Identity.Web 3.x → 4.x breaking changes.

## Results

_Pending._

### How to validate

_Pending (research task: validated by the RFC being balloted and child tasks created)._
