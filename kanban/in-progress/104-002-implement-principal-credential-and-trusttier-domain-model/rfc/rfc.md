# RFC: Ratify TimeWarp.Identity domain model before Wave 1 ceremonies

**Status:** Ballots tallied (2026-07-17). Resolutions recorded; **fold-in on host task 104-002** (reopened in-progress). Not product law until fold-in lands on this task.
**Host task:** 104-002 — kitchen for RFC + fold-in (agent-collaboration same-task rule).
**Parent program:** 104 (Agent-ready Identity and x402).
**Author:** orchestrator (Grok), 2026-07-17.
**Audience:** Independent reviewers. Append a ballot under [Reviewer opinions](#reviewer-opinions)
using the template at the bottom. Do **not** rewrite others' entries.

> **Working material, not law.** Resolutions fold into Design regions and product code **on task
> 104-002**. Do not treat this RFC as a rule until fold-in is recorded in task Results.

---

## 1. Why this exists

104-002 shipped a clean, tested domain model (`PrincipalId`, enums, `Principal`/`Credential`,
`IPrincipalStore` + in-memory). A post-implementation review
([`2026-07-16-code-review.md`](../2026-07-16-code-review.md)) found **cascade risks**: cheap to
change now, expensive after 104-003/004/005 bind handlers and contracts to the surface.

This RFC ballots those independent decisions so we do not start WebAuthn (104-003) on unratified
trust/id semantics.

### Out of scope

- WebAuthn ceremony design (104-003)
- Agent token format (104-004)
- HTTP API shapes (005+)
- EF Core schema / migrations (later host)
- Skills / ADRs (program rule: after software works)

### In scope

Eight decisions below. Objective bugs already fixed in 104-002 (mutable getter arrays, empty
`PrincipalId` on `Credential.Create`, `DateTimeOffset`) are **not** reopened.

---

## 2. Sources of truth (evidence)

| # | Source | Path / ref | Nature |
|---|--------|------------|--------|
| A | Shipped code | `source/libraries/timewarp-identity/*.cs` | Product truth today |
| B | Unit tests | `tests/libraries/timewarp-identity-tests/**` | What the double certifies |
| C | Implementation plan | 104-002 task Notes | Pre-code decisions |
| D | Code review | `2026-07-16-code-review.md` | Cascade analysis |
| E | Program 104 context | parent folder `task.md` | Product locks (hybrid id, tiers, multi-cred) |

### Evidence matrix (selected dimensions)

| Dimension | A — Code today | C — Plan | D — Review |
|-----------|----------------|----------|------------|
| Trust model | Single `TrustTier` enum: Keyed=0…Quarantined=3; free `SetTrustTier` | Same single axis | Split risk vs progression; no ordinal safety |
| Birth tier | `Create` → Keyed with 0 credentials | Keyed = "has credential" narrative | Birth invariant false |
| Enum zeros | Human=0, Keyed=0, Passkey=0 | Explicit ints, no reserved 0 | Fail-open defaults |
| Credential id | raw `Guid` | raw Guid | Wrap as `CredentialId` |
| Store Get* | Shared mutable instances | In-memory for tests | Snapshot so missing Update fails in tests |
| Clocks | `DateTimeOffset.UtcNow` inside Create/Revoke | Optional inject | `TimeProvider` |
| Concurrency | None | Unspecified | Token now vs later |
| Update handle | Migration branch in `UpdateCredentialAsync` | Port listed Update | Dead branch — delete |

---

## 3. Objective (already fixed — not balloted)

| Item | Status |
|------|--------|
| Credential getter mutability | Fixed: getters return copies |
| Empty PrincipalId on Credential.Create | Fixed: `IsEmpty` rejected |
| DateTime → DateTimeOffset | Fixed |

---

## 4. Decisions needing ballots

For each: options, trade-offs, **author lean**. Reviewers vote by decision number + short topic.

### Decision 1 — Trust model shape (stop-the-line)

**Topic:** How should trust progression and quarantine interact?

| Option | Description |
|--------|-------------|
| **A. Single enum (status quo)** | `TrustTier`: Keyed, Funded, Established, Quarantined; free `SetTrustTier` |
| **B. Split axes** | Progression enum (e.g. Provisional/Keyed/Funded/Established) **plus** orthogonal risk (`IsQuarantined` / `PrincipalStatus`) |
| **C. Single enum + constrained transitions** | Keep one enum but replace setter with `Promote`/`Quarantine` that forbid ordinal abuse |

**Trade-offs:** A is simple but `tier >= Funded` fails open for Quarantined=3. B matches security reality; more surface. C keeps one field but needs careful ordering (Quarantined can't sit at top of ordinal ladder).

**Author lean: B** — quarantine is not a "higher tier." Progression and risk must not share one ordered enum. Also introduce a floor below Keyed (or set Keyed only when first credential attaches) so abandoned registration ≠ keyed.

---

### Decision 2 — Enum zero values (stop-the-line)

**Topic:** Should security enums reserve `0 = None/Unknown`?

| Option | Description |
|--------|-------------|
| **A. Meaningful zero (status quo)** | Human=0, Keyed=0, Passkey=0 |
| **B. Reserved zero** | `None/Unknown = 0`; real values start at 1; reject None at Create/Set |

**Trade-offs:** A is natural language ordering; fails open on default/missing JSON. B renumbers now (breaking if any external store already has 0 — none yet); safer for contracts.

**Author lean: B** — no production rows yet; renumber before 104-003.

---

### Decision 3 — CredentialId wrapper (stop-the-line)

**Topic:** Wrap credential primary key like `PrincipalId`?

| Option | Description |
|--------|-------------|
| **A. Raw Guid (status quo)** | `Credential.Id` / `GetCredentialAsync(Guid)` |
| **B. `CredentialId` record struct** | Mirror `PrincipalId`; store APIs take `CredentialId` |

**Trade-offs:** A fewer types. B prevents principal/credential Guid mix-ups at call sites — the exact bug `PrincipalId` was meant to prevent.

**Author lean: B** — add while surface is still small.

---

### Decision 4 — In-memory store read semantics (stop-the-line)

**Topic:** Should the test double share mutable instances or snapshot?

| Option | Description |
|--------|-------------|
| **A. Shared references (status quo)** | Get returns store's object; mutate without Update "works" |
| **B. Snapshot on read/write** | Get returns copy (or immutable view); Update required to persist — matches EF/request-scoped reality |

**Trade-offs:** A simpler and faster tests; certifies handlers that forget Update. B harder double; catches missing Update before production.

**Author lean: B** — wrong test double is worse than verbose tests.

---

### Decision 5 — Time / id nondeterminism

**Topic:** Inject `TimeProvider` (and optional id factory) into Create/Revoke?

| Option | Description |
|--------|-------------|
| **A. Static UtcNow / CreateVersion7 (status quo)** | Simple; fuzzy time asserts |
| **B. Optional `TimeProvider` parameters** | Default `TimeProvider.System`; tests pass fake |
| **C. Full factory ports** | `IPrincipalIdFactory` + TimeProvider on every create |

**Trade-offs:** A fine until ceremony replay tests. B is BCL-only, small surface. C more DI ceremony than needed now.

**Author lean: B** — enough for 104-006 without inventing ports.

---

### Decision 6 — Optimistic concurrency token

**Topic:** Add version/etag on Principal/Credential now?

| Option | Description |
|--------|-------------|
| **A. None (status quo)** | Add when EF lands |
| **B. `ulong` / `Guid` row version on both entities + store contract** | Port knows concurrency now |

**Trade-offs:** A fewer fields until host needs it. B avoids dual-schema churn when EF + revoke races appear; still needs store semantics defined.

**Author lean: A for Principal, B-lite for Credential** — at least document that `UpdateCredentialAsync` is last-write-wins until a later task adds a token. Prefer not blocking 003 on full concurrency design. **Ballot as A vs B** (B = both entities get a version field now).

**Author lean (ballot choice): A** — defer concurrency token to host/EF task; record last-write-wins in Design. Do not block Wave 1.

---

### Decision 7 — UpdateCredentialAsync handle-migration branch

**Topic:** Dead code that reindexes handle on Update when Type/Handle are immutable?

| Option | Description |
|--------|-------------|
| **A. Keep branch (status quo)** | Future-proof if handles become mutable |
| **B. Delete branch; document immutable handle/type** | Port contract honest; EF implementers don't implement fiction |

**Author lean: B**.

---

### Decision 8 — Credential material type

**Topic:** `byte[]` copies on get vs `ImmutableArray<byte>` / `ReadOnlyMemory<byte>`?

| Option | Description |
|--------|-------------|
| **A. `byte[]` + copy on get (status quo)** | Works; allocation per access; CA1819 pragma |
| **B. `ImmutableArray<byte>`** | Immutable by type; no copy-on-get; slightly awkward create APIs |
| **C. `ReadOnlyMemory<byte>` properties** | Zero-copy views of private arrays; careful with lifetime |

**Trade-offs:** A clear and tested. B best long-term immutability. C hot-path friendly but easier to misuse if memory is rented.

**Author lean: A for Wave 1** — revisit if 003 profiling shows cost; not stop-the-line. **Acceptable to pick B if reviewers want type-system immutability before ceremonies.**

---

## 5. Author priority for pre-003 work

If ballots accept leans, implement **before 104-003** in this order:

1. Decision 1 (trust model)
2. Decision 2 (enum zeros)
3. Decision 3 (CredentialId)
4. Decision 4 (store snapshots)
5. Decision 7 (delete dead update branch)
6. Decision 5 (TimeProvider) — can pair with 003 if needed
7. Decision 6, 8 — document or defer per tally

---

## 6. Reviewer opinions

*(Independent ballots. Entries not rewritten after the fact.)*

### Ballot template (historical)

```markdown
### <agent/model name> — <date>
**Overall:** <one line>
**Decision 1 (trust model):** <A|B|C or refined> — <why>
…
```


### general-purpose-A — 2026-07-17
**Overall:** Agree with all stop-the-line leans (1–4, 7); ship those before 104-003; defer concurrency and material-type polish.
**Decision 1 (trust model):** B — Quarantine is a risk override, not a progression rung; verified `Quarantined = 3` with free `SetTrustTier` makes `tier >= Funded` fail open. Keep progression orthogonal to `IsQuarantined`; drop free setter for constrained Promote/Quarantine. Fix birth invariant: code starts Keyed with zero credentials.
**Decision 2 (enum zeros):** B — Human=0, Keyed=0, Passkey=0 fail open on default/missing JSON; renumber now; no production rows.
**Decision 3 (CredentialId):** B — PrincipalId exists to prevent Guid mix-ups but Credential.Id is naked Guid; wrap while surface is small.
**Decision 4 (store snapshots):** B — Shared mutable Get* certifies handlers that skip Update*; snapshot so tests match EF UoW.
**Decision 5 (TimeProvider):** B — BCL TimeProvider default System for 006; no id-factory ports yet.
**Decision 6 (concurrency token):** A — Document LWW; full token without EF is speculative; do not block Wave 1.
**Decision 7 (update handle branch):** B — Unreachable fiction; delete and state real port contract.
**Decision 8 (material type):** A — Copy-on-get fine for Wave 1.
**Anything the author missed:** Snapshot needs clone/with-style APIs; document Find-by-handle includes revoked; AddCredential TOCTOU; pair D1 floor with first-credential attach rule.

### general-purpose-B — 2026-07-17
**Overall:** Stop-the-line on trust split, reserved zeros, CredentialId, and store snapshots before 104-003; defer concurrency/material polish with explicit LWW docs.
**Decision 1 (trust model):** B — Single ordered ladder + free SetTrustTier is fail-open; C alone still invites ordinal abuse. Split progression vs risk; floor below Keyed; named predicates not raw >=.
**Decision 2 (enum zeros):** B — Fail closed like PrincipalId.Empty; renumber before contracts freeze.
**Decision 3 (CredentialId):** B — Highest-probability mix-up surface; wrap before 005 routes.
**Decision 4 (store snapshots):** B — Double must not certify wrong handlers.
**Decision 5 (TimeProvider):** B — Enough for 006; C premature.
**Decision 6 (concurrency token):** A — Document LWW; version when multi-request races land.
**Decision 7 (update handle branch):** B — Delete dead branch.
**Decision 8 (material type):** A — Not a security cascade.
**Anything the author missed:** Birth floor sub-decision of D1; ship authorization predicate; port docs for revoked find; handle length bound at ceremony layer.

### adversarial-C — 2026-07-17
**Overall:** Review cascade is real on trust transitions; several “stop-the-line” items are hygiene/YAGNI dressed as security — reject soft consensus on B/B/B/B.
**Decision 1 (trust model):** C refined (not full B) — Ordinal fail-open and free setter are real; full dual-axis status is over-model before any gate exists. Do: kill free setter, add IsQuarantined, fix birth; defer rich PrincipalStatus until 008/013. Ban raw ordinal compares via named predicates.
**Decision 2 (enum zeros):** A — Domain already rejects undefined enums; defaulted JSON is a contract problem. B is optional defense later, not a 003 gate.
**Decision 3 (CredentialId):** A (until 005) — 003 is handle-centric; fossilization risk is 005. Add when credential ids hit public contracts.
**Decision 4 (store snapshots):** A — Clone-on-Get can train wrong host pattern (tracked EF often mutates without Update); domain lacks copy constructors. Prefer Design LWW + optional spy tests.
**Decision 5 (TimeProvider):** A — Fuzzy asserts enough until 006; inject at ceremony edge later.
**Decision 6 (concurrency token):** A — Agree with author.
**Decision 7 (update handle branch):** B — Delete dead branch.
**Decision 8 (material type):** A — Keep copies for Wave 1.
**Anything the author missed:** D1 bundles three independent fixes; product lock #7 narrates single tier list including Quarantined; snapshot vs change-tracking EF conflict; only trust transitions + dead Update branch are clearly stop-the-line.


## 7. Tally

| # | Topic | A | B | Adv-C | Outcome |
|---|-------|---|---|-------|---------|
| 1 | Trust model | B | B | C refined | **Dissent → maintainer: C refined** (see §7.1) |
| 2 | Enum zeros | B | B | A | **Dissent → maintainer: B** (renumber now; free before contracts) |
| 3 | CredentialId | B | B | A (until 005) | **Dissent → maintainer: B** (add now; 005 will fossilize) |
| 4 | Store snapshots | B | B | A | **Dissent → maintainer: A** (Adv EF/UoW argument wins for Wave 1) |
| 5 | TimeProvider | B | B | A | **Defer B** to 104-006 — not a 003 blocker |
| 6 | Concurrency token | A | A | A | **Unanimous A** |
| 7 | Update handle branch | B | B | B | **Unanimous B** |
| 8 | Material type | A | A | A | **Unanimous A** |

### 7.1 Maintainer resolutions (2026-07-17)

**Decision 1 — C refined (security intent of B, Wave-1 size of C):**
- Add orthogonal quarantine (`IsQuarantined` or equivalent risk flag), **not** Quarantined as top ordinal tier.
- Remove free `SetTrustTier`; use constrained `Promote` / `Quarantine` / `ClearQuarantine`.
- Fix birth: Provisional (or equivalent) until first credential **or** set Keyed only on first attach.
- Ship named predicates (e.g. `IsFundedAndActive`) so handlers never write `tier >= Funded`.
- Defer rich multi-state `PrincipalStatus` taxonomy until 008/013 write real gates.

**Decision 2 — B:** Renumber enums with reserved `0 = None/Unknown` and reject None at domain entry points. No production rows yet; cheaper than relying only on future contract validators. (Adv: contract-level fix also valid as defense-in-depth later.)

**Decision 3 — B:** Introduce `CredentialId` before 104-003/005. Adv correctly notes 003 is handle-centric; still add the type so store and 005 do not fossilize raw Guid.

**Decision 4 — A:** Keep shared-reference in-memory store for Wave 1. Document last-write-wins and host Update expectations in Design. Snapshot-on-get deferred (EF change-tracking hosts often do not use explicit Update).

**Decision 5 — Defer B:** Optional `TimeProvider` when 104-006 needs controllable clocks; not required to open 003.

**Decision 6 — A:** Document last-write-wins; no concurrency token in Wave 1.

**Decision 7 — B:** Delete dead handle-migration branch; document immutable Type/Handle and Update purpose (e.g. revoke persistence).

**Decision 8 — A:** Keep `byte[]` copy-on-get for Wave 1.

### 7.2 Fold-in (same host task)

Per **agent-collaboration** / **rfc-ballot**: fold-in is **104-002**, not a sibling process task.
Erroneous child **104-026** was archived (process residue).

- Host **104-002** reopened → **in-progress**
- Stop-the-line code (D1 refined, D2, D3, D7) + Design updates for deferred items live on 104-002 checklist
- **104-003** remains backlog until 104-002 is done with fold-in complete

---

## 8. Fold-in checklist (host 104-002)

- [ ] On **104-002**: implement D1 refined, D2, D3, D7
- [ ] Design regions updated (incl. D4/D6/D8 docs; D5 deferred note)
- [ ] Tests green
- [ ] This RFC status banner: folded in when 104-002 re-done
- [ ] 104-002 Results cover ballot outcome **and** what landed in the repo
- [ ] Do **not** create another “apply resolutions” task

