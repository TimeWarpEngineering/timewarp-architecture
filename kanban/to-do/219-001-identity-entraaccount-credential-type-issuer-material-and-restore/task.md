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

- [ ] `CredentialType.EntraAccount`
- [ ] Handle + issuer helpers
- [ ] Purpose/Design on `credential.cs` / `credential-type.cs`
- [ ] `Credential.Restore()` + store round-trip tests
- [ ] `dev build` 0/0 and identity unit tests green

## Notes

RFC: `kanban/in-progress/219-research-entra-id-as-a-linked-credential-on-passkey-principals-plus-tenant-user-sync/rfc/rfc.md` §4 D2, §7.1.

Join key is `tid:oid`, never email/UPN/`sub`. Library stays Graph-free and EF-free.

## Depends on

(none — first architecture child)

## Session

- Created: 3423200 (2026-09-14)
