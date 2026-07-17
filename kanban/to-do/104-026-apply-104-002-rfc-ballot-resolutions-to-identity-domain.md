# Apply 104-002 RFC ballot resolutions to Identity domain

## Parent

104

## Description

Fold in the resolved decisions from
`kanban/done/104-002-…/rfc/rfc.md` into `TimeWarp.Identity` code, Design
regions, and tests. **Blocks 104-003** until stop-the-line items land.

## Source RFC

`kanban/done/104-002-implement-principal-credential-and-trusttier-domain-model/rfc/rfc.md`

## Resolutions to implement (stop-the-line before 104-003)

| # | Topic | Resolution |
|---|-------|------------|
| 1 | Trust model | **C refined:** orthogonal `IsQuarantined` (or risk flag); constrained `Promote` / `Quarantine` / `ClearQuarantine` (no free `SetTrustTier`); birth floor — Provisional **or** Keyed only after first credential; named predicates (`IsFundedAndActive`) not ordinal `>=` |
| 2 | Enum zeros | **B:** reserve `0 = None/Unknown` for `PrincipalKind`, `TrustTier` progression values, `CredentialType`; reject None at Create/promote |
| 3 | CredentialId | **B:** add `CredentialId` record struct; store APIs use it |
| 7 | Update handle branch | **B:** delete dead handle-migration in `UpdateCredentialAsync`; document immutable type/handle |

## Document only / defer (not code-blockers for 003)

| # | Resolution |
|---|------------|
| 4 | **A:** keep shared in-memory refs; Design + Results document LWW and that handlers must `Update*` where hosts require it; no snapshot-on-get for Wave 1 |
| 5 | **Defer B:** `TimeProvider` when 104-006 needs clock control |
| 6 | **A:** last-write-wins documented; no concurrency token yet |
| 8 | **A:** keep `byte[]` copy-on-get |

## Checklist

- [ ] Decision 1: risk flag + constrained transitions + birth floor + named predicates
- [ ] Decision 2: renumber enums + reject None
- [ ] Decision 3: CredentialId + store API update
- [ ] Decision 7: delete dead UpdateCredential handle branch + Design note
- [ ] Design regions updated to match resolutions
- [ ] Unit tests updated/expanded
- [ ] `dev build` 0/0; identity tests green
- [ ] RFC banner: folded in; 104-002 Results updated

## Notes

- Adversarial ballot challenged full dual-status over-model; resolution keeps B’s security intent with C’s Wave-1 size.
- Do not start 104-003 until this task is done (or explicitly unblocked by maintainer).

## Session

- Created: 2026-07-17 (rfc-ballot on 104-002)
