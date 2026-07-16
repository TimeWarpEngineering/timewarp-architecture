# Code Review — 104-002 Principal / Credential / TrustTier domain model

- **Date**: 2026-07-16
- **Commit reviewed**: `55c6646f` (feat(identity): add Principal Credential TrustTier domain model)
- **Verification**: `dev build` re-run → 0 warnings / 0 errors. `dotnet fixie tests/libraries/timewarp-identity-tests` re-run → 35 passed.
- **Verdict**: Line-level execution is clean and matches the plan. The cascade risk is in five design
  choices — cheap to change now, expensive after 104-003/004/005 build contracts and handlers on them.
  Items 1–4 below should be resolved **before 104-003 starts**.

## Requirements coverage (first pass)

- Stable never-recycled id: `PrincipalId` readonly record struct over Guid v7; `From` rejects
  `Guid.Empty`; `Credential.Create` rejects `default`. Tested. ✅
- Explicit enums: `PrincipalKind`, `TrustTier`, `CredentialType` with explicit values; undefined
  values rejected at all entry points. Tested. ✅
- Multi-credential by model: Principal/Credential 1:N; store tests prove multi-cred, `(type, handle)`
  uniqueness, same-handle-different-type allowed. ✅
- Persistence strategy documented: `IPrincipalStore` port + `InMemoryPrincipalStore` Design region
  records the no-EF decision and reference semantics. ✅
- Template wiring: slnx entry inside existing `<!--#if (!foundationPackages) -->` block;
  `tests/libraries/**` added to matching template.json exclude. ✅
- TWA0004 absence in test files is repo policy (`tests/Directory.Build.props` NoWarn). ✅

## Will cascade — fix before 104-003

### 1. TrustTier conflates two axes and has no transition rules (security kernel)

Keyed → Funded → Established is a **progression** axis. Quarantined is a **risk override** — an
orthogonal state that can strike at any tier. One enum for both means every future authorization
check inherits a footgun: `TrustTier` begs ordinal comparison (`tier >= TrustTier.Funded`), and with
`Quarantined = 3` a quarantined principal **passes** that check. 104-013 wires payment settle →
tier; 104-008 gates x402 on it; the first ordinal comparison ships a privilege escalation.

Compounding it:
- `SetTrustTier` accepts any defined value from anywhere — Quarantined → Funded is one legal call
  with no rule saying it isn't.
- The birth invariant is false: Keyed is defined as "has a credential," but `Principal.Create`
  assigns Keyed with zero credentials, so an abandoned registration is indistinguishable from a
  keyed principal.

**Recommendation**: separate the risk state (e.g. `IsQuarantined` / `Status`), add an honest floor
tier below Keyed (or set Keyed on first credential attach), and replace the setter with constrained
transitions (`Promote` / `Quarantine`). Stop-the-line item.

### 2. Meaningful zero values on security enums fail open

`Human = 0`, `Keyed = 0`, `Passkey = 0`. Every defaulted struct, missing JSON field, or un-set DB
column silently deserializes to the **most privileged interpretations** ("human, trusted-keyed,
passkey"). `PrincipalId` got this right (empty rejected); the enums got it wrong. Once these values
are in contracts (104-003) and rows (EF later), renumbering is a breaking migration.

**Recommendation**: reserve `0 = None/Unknown` now and reject it in `Create` / `SetTrustTier`,
mirroring `PrincipalId.IsEmpty`.

### 3. `PrincipalId` is wrapped but `Credential.Id` is a raw `Guid`

The stated purpose of `PrincipalId` is preventing Guid mix-ups at call sites. The most likely
mix-up in this system is *principal id vs credential id* — and half of that pair is naked
(`GetCredentialAsync(Guid)`). 104-005's list/revoke endpoints will fossilize raw Guid into
contracts and routes.

**Recommendation**: add `CredentialId` now while the surface is eight files.

### 4. In-memory store reference semantics make the test double lie about production

Entities are mutable and `Get*` returns the store's own instances, so a handler that mutates and
*forgets* `UpdateAsync` still passes every test — and breaks the day EF (DbContext per request)
replaces the store. That is the worst kind of seam divergence: it certifies wrong code. The Design
region documenting it does not defuse it; 003–005 handlers will be written against this double.

**Recommendation**: make the in-memory store snapshot on read/write (copy semantics) so a missing
`Update*` fails in tests the same way it fails in production.

## Design debt — decide now, cheaper now than later

### 5. Nondeterminism baked into the domain

`DateTimeOffset.UtcNow` and `Guid.CreateVersion7()` are called inside `Create` / `Revoke`. The
tests already show the symptom (fuzzy `ShouldBeInRange` time assertions). `TimeProvider` is BCL, so
injecting it does not violate the dependency-free rule. Matters for 104-006 ceremony tests
(WebAuthn timestamp/replay semantics need a controllable clock).

### 6. No optimistic concurrency anywhere

No version/etag on entities; the port contract says nothing about lost updates. Credential
revocation is exactly the write that must not be lost-updated. Adding a concurrency token later
changes both the schema and the port; adding it now changes eight files.

### 7. `UpdateCredentialAsync` handle-migration branch is unreachable

`Type` / `Handle` are immutable and `Id` is only minted inside `Create`, so no caller can present
the same Id with a different handle key. The dead branch implies a contract ("handles can change on
update") that every future EF implementer will dutifully and pointlessly implement. Delete it, and
state the port's actual contract: is `Update*` required to persist, and what atomicity does
`AddCredentialAsync`'s principal-existence check guarantee? An EF implementer currently has to guess.

## Noted, not blocking

- `byte[]`-copy-per-getter is a hidden allocation on what becomes the per-authentication hot path
  in 003; `ImmutableArray<byte>` expresses the same immutability with no copies and no CA1819 pragma.
- `FindCredentialByHandleAsync` returning revoked credentials is the right call (distinguishes
  "revoked" from "unknown") but is undocumented contract — add a doc line when 104-003 touches it.
- Handle length is unbounded and gets hex-encoded into the index key; bound it at the ceremony
  layer (WebAuthn caps credential IDs at 1023 bytes).
- Revoked credentials keep their handle in the index, so a revoked handle can never be
  re-registered — correct for WebAuthn credential-id semantics; keep it.
- The `tests/libraries/**` template exclude couples *every future* library's tests to the
  `foundationPackages` flag — correct today with one occupant, worth remembering.

## Root cause

The implementing agent built a clean version of the plan; the plan itself under-specified the trust
model. Item 1 especially is a plan-level gap, not an execution error.
