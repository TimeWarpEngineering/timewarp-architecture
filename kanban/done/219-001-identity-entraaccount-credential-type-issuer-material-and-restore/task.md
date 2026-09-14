# Identity: EntraAccount credential type, issuer material, and Restore

## Parent

219

## Description

Fold-in of architecture RFC 219 **Decision 2 A′** into `TimeWarp.Identity`. Add `CredentialType.EntraAccount`, handle/issuer helpers, a type-dependent `PublicMaterial` contract, and `Credential.Restore()` so Graph re-enable can reuse the same `(Type, Handle)` row.

Do **not** add Graph, OIDC, or template ceremony here (those are 219-002).

## Requirements

- `CredentialType.EntraAccount = 3` (reserved zero stays `None`)
- `EntraAccountHandle.Encode/TryDecode`: UTF-8 `"{tid}:{oid}"` canonical lowercase GUIDs
- `EntraIssuerMaterial.FromTenantId`: UTF-8 `https://login.microsoftonline.com/{tid}/v2.0`
- Rewrite `credential.cs` Purpose/Design: `PublicMaterial` is **type-dependent verification material** (COSE/SPKI for Passkey/AgentKey; issuer URI for EntraAccount only)
- Hosts must never feed Entra rows into WebAuthn/AgentKey verify
- `Credential.Restore()` — one-shot inverse of `Revoke()`; throws if not revoked; clears `RevokedAt`
- Store: no new lookup methods; `FindCredentialByHandleAsync` still returns revoked rows
- Tests: encode/decode round-trip; Create with issuer material; Restore; unique handle still enforced; existing passkey/agent tests stay green

## Checklist

- [x] `CredentialType.EntraAccount`
- [x] Handle + issuer helpers
- [x] Purpose/Design on `credential.cs` / `credential-type.cs`
- [x] `Credential.Restore()` + store round-trip tests
- [x] `dev build` 0/0 and identity unit tests green
- [x] Implementation review (effort 1, general) — disposition **clean**

## Notes

RFC: `kanban/in-progress/219-research-entra-id-as-a-linked-credential-on-passkey-principals-plus-tenant-user-sync/rfc/rfc.md` §4 D2, §7.1.

Join key is `tid:oid`, never email/UPN/`sub`. Library stays Graph-free and EF-free.

No Graph, OIDC, or template ceremony (219-002). Store port unchanged: Restore is `Credential.Restore()` then `UpdateCredentialAsync`. `TryDecode` rejects non-canonical (uppercase / non-D) encodings so unique `(Type, Handle)` cannot split one account across casings.

Review (2026-09-14): effort 1, roster `general`, 1 round, disposition **clean** (0 open). Kitchen: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Depends on

(none — first architecture child)

## Session

- Created: 3423200 (2026-09-14)
- Implementer: grok session 01a0a02e-9556-7e40-8a69-35284dfed141 (2026-09-14)
- Review oracle: grok session 01a0a037-48fd-7190-9f39-8fea7589d855 (2026-09-14)
- Reviewer (general, round 1): grok session 01a0a038-ce46-7380-a110-8516afbabdc0 (2026-09-14)

## Results

Fold-in of RFC 219 Decision 2 A′ into `TimeWarp.Identity`.

**Implemented**
- `CredentialType.EntraAccount = 3` (`None` stays 0)
- `EntraAccountHandle.Encode` / `TryDecode`: UTF-8 `{tid}:{oid}` canonical lowercase GUID "D" form
- `EntraIssuerMaterial.FromTenantId`: UTF-8 `https://login.microsoftonline.com/{tid}/v2.0`
- `Credential.Restore()`: one-shot inverse of `Revoke()`; throws if not revoked; clears `RevokedAt`
- Purpose/Design on `credential.cs` and `credential-type.cs`: `PublicMaterial` is type-dependent (COSE/SPKI for Passkey/AgentKey; issuer URI for EntraAccount only). Hosts must never feed Entra rows into WebAuthn/AgentKey verify
- No new `IPrincipalStore` lookup methods; `FindCredentialByHandleAsync` still returns revoked rows

**Files**
- `source/libraries/timewarp-identity/credentials/credential-type.cs`
- `source/libraries/timewarp-identity/credentials/credential.cs`
- `source/libraries/timewarp-identity/credentials/entra-account-handle.cs`
- `source/libraries/timewarp-identity/credentials/entra-issuer-material.cs`
- `source/libraries/timewarp-identity/persistence/i-principal-store.cs` (Design)
- `source/libraries/timewarp-identity/persistence/in-memory-principal-store.cs` (Design)
- `source/libraries/timewarp-identity/overview.md`
- `source/libraries/timewarp-identity/timewarp-identity.csproj` (package description/tags)
- `tests/libraries/timewarp-identity-tests/credential-tests.cs`
- `tests/libraries/timewarp-identity-tests/entra-account-handle-tests.cs`
- `tests/libraries/timewarp-identity-tests/entra-issuer-material-tests.cs`
- `tests/libraries/timewarp-identity-tests/entra-account-store-tests.cs`

**Decisions / deviations**
- `Create` still accepts any non-empty `PublicMaterial` bytes (same as Passkey/AgentKey not validating COSE/SPKI at Create). Legal Entra material is produced by `EntraIssuerMaterial.FromTenantId`; the type-dependent contract is Design law plus helpers.
- `TryDecode` is fail-closed on non-canonical encodings (uppercase hex, N-format, non-ASCII).
- Restore store round-trip lives in identity in-memory tests, not the shared EF contract suite — persistence is existing `UpdateCredentialAsync` of `RevokedAt`.
- Graph, OIDC, and template ceremony remain 219-002.

**Test outcomes**
- `dotnet run tools/dev-cli/dev.cs -- build`: 0 Warning(s), 0 Error(s)
- `cd tests/libraries/timewarp-identity-tests && dotnet test -c Release`: 195 passed, 0 failed, 0 skipped (includes existing passkey/agent tests)

**Review**
- Rounds: 1 · Effort: 1 · Roster: general
- Counts (final): bug 0/0/0 open/fixed/wontfix; suggestion 0/0/0; nit 0/0/0
- Disposition: **clean** (no issues raised; no exceptions)
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Smoke**

```bash
cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class EntraAccountHandle_
cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class EntraIssuerMaterial_
cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class Restore
cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class EntraAccountStore_
```

**Expect**
- `EntraAccountHandle_`: Encode emits lowercase `{tid}:{oid}`; TryDecode round-trips; uppercase / N-format / empty / non-ASCII fail
- `EntraIssuerMaterial_`: UTF-8 `https://login.microsoftonline.com/{lowercase-tid}/v2.0`
- `Restore`: clears `RevokedAt`; throws if not revoked; revoke-after-restore works
- `EntraAccountStore_`: Find-by-handle returns the revoked Entra row; Restore + Update reuses the same `CredentialId`; duplicate Entra handle still throws while the first row is revoked

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

cd tests/libraries/timewarp-identity-tests && dotnet test -c Release
# expect: passed, failed 0 (195 succeeded in this implementer run)
```

**Not in scope:** Graph client, OIDC scheme, template bootstrap/link (219-002). No live Entra tenant.
