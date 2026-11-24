# Persisted entity inventory

Spike output for task 329. Checked on 2026-11-25 against `PostgresDbContext`
(`source/container-apps/web/platform/postgres/postgres-db-context-infrastructure.cs`) and the
migration snapshot (`platform/postgres/migrations/PostgresDbContextModelSnapshot.cs`, last
migration `AddRoles`). The column lists come from the snapshot.

## Summary

13 `DbSet`s and 3 owned tables: 16 tables. Four types implement `IAggregateRoot`: `Profile`,
`FeedbackItem`, `Role`, `AgentHumanLink`. `Principal`, `Credential`, and `SiteSettings` say in
their Design regions that they are deliberately not aggregates.

Changes from the task's starting inventory:

- `RolePermissionGrant` is no longer a `DbSet`. `Role` owns `PermissionGrant` and
  `AgentScopeGrant` rows (task 473).
- New since the task was written: `DataProtectionKey`, `FeedbackAttachmentBytes`,
  `PrincipalAgentScopeGrant`, `ProtectedSecret`, and the owned `FeedbackActivity` thread.
- `TimeWarp.Identity` is now an in-repo library (`source/libraries/timewarp-identity`) that ships to
  generated apps as a package. `Principal`, `Credential`, and `SiteSettings` live there.
- `DataProtectionKey` is an ASP.NET type. The app owns its `IEntityTypeConfiguration`
  (`platform/postgres/data-protection-key-entity-type-configuration-infrastructure.cs`), so an EF
  annotation can classify it even though an attribute cannot.

## Classes

- **Secret**: never shown, even to an administrator (key material, credential bytes, ciphertext,
  key ring XML).
- **Personal**: identifies a person (names, email, account handles, user-named files, device
  description). Behind a PII permission.
- **Text**: free text a person wrote (feedback title, body, notes). Behind the owning slice's
  read permission.
- **Safe**: ids, timestamps, enums, counts, versions, flags, admin-authored configuration.
- "Call" marks a judgment call; the reason is given.

No entity property carries a classification today. The only classification attributes,
`[PersonalData]` and `[SensitiveData]` (task 344, `aspire-service-defaults`), are used on two
`[LoggerMessage]` parameters. The rest is comments (`Credential.AccountHint` "PII").

## Table

| Type (owner) | schema.table | Aggregate | Secret | Personal | Text | Safe | Curated admin read |
|---|---|---|---|---|---|---|---|
| `DataProtectionKey` (ASP.NET; config in platform/postgres) | data_protection.keys | no | `Xml` (master key; stored unprotected, no `ProtectKeysWith*`) | | | `Id`, `FriendlyName` | none, and none wanted |
| `Profile` (features/profile) | profiles.profiles | yes | | `DisplayName`, `Email` | | `Id`, `Language`, `Region` (call: locale narrows identity a little), `Theme`, `Notifications`, `Version` | inside `ListPrincipals` / `GetPrincipal`, gated by `admin.profiles.read` (328) |
| `FeedbackItem` (features/feedback) | feedback.feedback_items | yes | | | `Title`, `Body` | `Id`, `OwnerPrincipalId`, `Kind`, `FiledAt`, `Status`, `Severity`, `Version` | `ListFeedback`, `GetFeedbackForAdmin`; `/Admin/Feedback` pages (314, 327) |
| `FeedbackActivity` (owned by `FeedbackItem`) | feedback.feedback_activity | owned | | | `Body` (filer or admin text; internal notes hidden from the filer) | `FeedbackItemId`, `Sequence`, `Kind`, `AuthorPrincipalId`, `From/ToStatus`, `From/ToSeverity`, `At` | inside `GetFeedbackForAdmin` |
| `FeedbackAttachment` (features/feedback) | feedback.feedback_attachments | no | | `FileName` (user-named) | | `Id`, `OwnerPrincipalId`, `FeedbackItemId`, `ContentType`, `Size`, `StorageKey` (call: addresses the bytes; hiding is reasonable), `UploadedAt`, `Version` | inside `GetFeedbackForAdmin` |
| `FeedbackAttachmentBytes` (features/feedback) | feedback.feedback_attachment_bytes | no | `Content` (call: a user's upload; a blob, never a grid cell) | | | `StorageKey` | download endpoint only |
| `AgentHumanLink` (features/agent-links) | agent_links.agent_links | yes | | | | all: `Id`, `AgentPrincipalId`, `HumanPrincipalId`, `Status`, `CreatedAt`, `DecidedAt`, `Version` | **none** (only the user's own `/AgentLinks`) |
| `Principal` (timewarp-identity) | identity.principals | no | | `DisplayName` (not returned by any admin contract today; admin pages show the profile name) | | `Id`, `Kind`, `TrustTier`, `IsQuarantined`, `CreatedAt`, `MergedIntoPrincipalId`, `Version` | `ListPrincipals`, `GetPrincipal`; `/Admin/Principals` pages |
| `Credential` (timewarp-identity) | identity.credentials | no | `Handle` (private field; passkey credential id or key id; Entra `tid:oid`), `PublicMaterial` (COSE/SPKI key or issuer) | `Nickname`, `AccountHint` (Entra UPN; code says own list only), `RegisteredBrowser`, `RegisteredOs` (call: coarse, but describes the user's device) | | `Id`, `PrincipalId`, `Type`, `CreatedAt`, `RevokedAt`, `Label` (call: authenticator provider name; old rows may hold user text), `RegisteredAttachment`, `LastUsedAt`, `Version` | **none** (only the user's own `GetCredentials`) |
| `PrincipalRoleAssignment` (features/admin/principals) | identity.principal_roles | no | | | | `PrincipalId`, `RoleId` | `RoleIds` in `ListPrincipals` / `GetPrincipal` |
| `Role` (features/admin/roles) | identity.roles | yes | | | | `Id`, `Name`, `Description`, `Version` | `GetRoles`, `GetRole`; `/Admin/Roles` pages |
| `PermissionGrant` (owned by `Role`) | identity.role_permissions | owned | | | | `RoleId`, `PermissionId` | inside `GetRole` |
| `AgentScopeGrant` (owned by `Role`) | identity.role_agent_scopes | owned | | | | `RoleId`, `Scope` | inside `GetRole` |
| `PrincipalAgentScopeGrant` (platform/authorization) | identity.principal_agent_scopes | no | | | | `PrincipalId`, `Scope` | inside `GetPrincipal` |
| `SiteSettings` (timewarp-identity; singleton) | identity.site_settings | no | | | | all, including `EntraAcceptedTenants` jsonb (call: organisation tenant ids and domains, admin-set configuration, not about a person) | `GetSiteSettings`; `/Admin/Authentication`, `/Admin/AI` |
| `ProtectedSecret` (platform/postgres) | secrets.protected_secrets | no | `Ciphertext`, `Suffix` (call: last four of the key; the AI page already shows it) | | | `Name`, `Purpose` (call: per-user rows embed the user id, so a list shows who stored a key), `SetAt`, `SetByPrincipalId` | `GetXaiApiKeyStatus` (site key metadata, `settings.write`); `/Admin/AI` |

## Coverage

- **Covered by a curated admin page:** `Principal`, `Profile`, `PrincipalRoleAssignment`,
  `PrincipalAgentScopeGrant` (principals area); `Role` and its two owned tables (roles area);
  `FeedbackItem`, `FeedbackActivity`, `FeedbackAttachment` (feedback area); `SiteSettings` and the
  site `ProtectedSecret` row's metadata (Authentication and AI pages).
- **No admin read at all:** `Credential` metadata, `AgentHumanLink`, per-user `ProtectedSecret`
  metadata.
- **Never browsable:** `DataProtectionKey`, `FeedbackAttachmentBytes`. Each is a secret or a blob
  plus a key; neither has a column an operator needs.

## Things the inventory turned up

- `Credential.Handle` is mapped from a private field and `RegisteredBrowser` / `RegisteredOs` from
  private properties. Anything that discovers columns from public properties misses them. EF's
  model has them.
- The ASP.NET key ring is stored as plain XML (no `ProtectKeysWith*` call). That is outside this
  spike, but anyone with database read access holds the master key. A follow-up candidate.
- The only `Personal` field on `Principal` (`DisplayName`) is not returned by any admin contract.
- `Profile.Email` maximum length (254) is duplicated as a literal in `ListPrincipals`' validator,
  noted there already.
