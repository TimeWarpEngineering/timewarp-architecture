# Spike: source-generated read-only admin entity browser with redaction and PII gating

## Question

Steve, 2026-10-12: "Is there any reason the Admin shouldn't be able to see a list of all Entities?"
The cockpit's answer: no, not for **read-only** visibility, provided these hold:
1. **Secrets are never shown**, even to admins: passkey credential material, agent keys, session
   tokens, hashes, connection strings. Show metadata only, such as created, last used, nickname.
2. **PII is gated by its own permission** (least privilege; see `admin.profiles.read` in 328).
3. **Read-only.** Writes stay with each slice's named commands, so aggregate invariants
   (TWA0011/0012) are never bypassed.
4. **Access is audited** (who viewed what).
5. **Scope:** platform admin vs a future tenant admin (marketplace 118) must not leak across
   tenants.

Hand-building a list and detail page per entity is agreement by memory, so explore a
**source-generated** browser instead. Curated workflow pages stay: feedback triage (314) and
principals/profiles (328).

**This is a spike. The deliverable is a design brought back to Steve for decisions, not
production code.** Steve participates in architecture decisions. Stop at the design and options;
do not pick the final shape alone.

## Inventory to start from (updated 2026-11-24; re-verify DbSets against master)

- `IAggregateRoot` is implemented by **four** types: `FeedbackItem`, `Role` (added in Task 473),
  `Profile`, and `AgentHumanLink`.
- EF `DbSet`s across the template: `AgentHumanLink`, `Credential`, `FeedbackAttachment`,
  `FeedbackItem`, `Principal`, `PrincipalRoleAssignment`, `Profile`, `RolePermissionGrant`,
  `SiteSettings`. Some come from the `TimeWarp.Identity` package (`Principal`, `Credential`).
- So "all entities" is **not** "all aggregates". The anchor must be chosen deliberately: DbSets,
  aggregates, or an explicit opt-in attribute.

## Spike deliverables (written into this task folder, `design/`)

1. **Entity inventory.** Every persisted type: owning slice/package, fields, which are secret,
   which are PII, and whether a curated admin page already exists or is planned.
2. **Anchor options** (pros/cons):
   - (a) every `DbSet<T>` in the app's contexts;
   - (b) `IAggregateRoot` types only;
   - (c) explicit opt-in, for example `[AdminBrowsable]`;
   - (d) hybrid: opt-out with mandatory redaction.

   Note how each handles package-owned types (`TimeWarp.Identity`) the app can't annotate.
3. **Redaction model.** Attribute(s) such as `[AdminRedact]` / `[AdminPii(Permission = …)]`, plus a
   **fail-closed analyzer**. Known secret shapes (key material, hashes, tokens, `byte[]` credential
   fields, names matching secret patterns) must be annotated or the build fails. Decide the
   default for an unannotated field (fail closed: hidden unless marked safe?).
4. **Generator shape.**
   - What is emitted per entity: contracts (`[ApiEndpoint]` + `[EndpointAuthorize]`), handler,
     SPA list and detail.
   - Paging, sort and filter, reusing 327's server-side `FluentDataGrid` pattern.
   - How it composes with slice isolation (TWA0009) and the existing generators (FastEndpoints,
     `[Page]`, `[StateAccess]`).
   - Read-only enforcement by construction.
5. **Audit.** What is logged per view, and where. Is that a new platform concern, or does one exist?
6. **Permissions.** Per-entity read permissions vs one `admin.data.read` plus per-field PII
   permissions. Seed defaults.
7. **Cost/benefit vs hand-built pages** for the current entity count, and when the generator pays
   off.
8. **Recommended options** to discuss, with no final decision. Then stop for Steve.

## Constraints

- No production code merged from the spike. A throwaway prototype in the task folder or a branch
  is fine if it informs the design.
- Public-safe wording in anything that might later move into a skill (no history or client names).

## Goal (implementer restatement)

Produce a design, not code, for a read-only admin view of every persisted entity, generated from
the model instead of hand-built per entity. The design has to answer: which types are in scope
(the anchor), how secrets stay hidden and PII stays behind its own permission (and how the build
fails when a new secret-shaped field is not classified), what a generator would emit and where it
runs given the project layering, how views are audited, what the permissions are, and when the
generator beats hand-built pages. It ends with recommended options for Steve to decide; it picks
nothing final.

Acceptance criteria for this walk:

- `design/inventory.md`: every persisted type re-verified against master's `PostgresDbContext`
  and migration snapshot, with owning slice or package, columns, secret / PII / safe class per
  column, and whether a curated admin page exists or is planned.
- `design/options.md`: anchor options (a)–(d) with package-owned type handling; redaction model
  and fail-closed check; generator shape (emitted artifacts, paging/sort/filter per 327, TWA0009
  and generator composition, read-only by construction); audit; permissions and seed defaults;
  cost/benefit and break-even; recommended options as open questions, no final decision.
- No production code changes. The two "discussion" and "follow-up tasks" checklist items stay
  open: they need Steve.

## Checklist

- [x] Inventory (`design/inventory.md`)
- [x] Options for anchor, redaction/analyzer, generator, audit, permissions (`design/options.md`)
- [ ] Discussion with Steve; decisions recorded as a dated decision log here
- [ ] Follow-up implementation task(s) filed from the decisions

## Session

- Created: 2026-10-12 (cockpit, per Steve)
- 2026-11-25 (implementer, `ganda task work`): re-verified the inventory against master's
  `PostgresDbContext` and migration snapshot (13 DbSets + 3 owned tables; `RolePermissionGrant` is
  now owned by `Role`; identity types now live in the in-repo `timewarp-identity` library). Wrote
  `design/inventory.md` and `design/options.md`. Stopped at recommendations for Steve; the
  decision log and follow-up tasks stay open. No production code changed.

## Results

Design written; decisions pending with Steve.

- `design/inventory.md`: every persisted column classed Secret / Personal / Text / Safe, with the
  owning slice or package and the curated admin page that already reads it. Uncovered today:
  `Credential` metadata, `AgentHumanLink`, per-user `ProtectedSecret` metadata.
- `design/options.md`: ten codebase facts that constrain the design (layering means contracts
  cannot see web-domain entities; EF mapping includes private members and owned tables; a log
  data taxonomy already exists; no audit concern; no tenant concept), then options per concern and
  nine recommendations to discuss: anchor (d) opt-out with mandatory classification; one taxonomy
  for logs and the browser with `SensitiveData` split; fail closed and fail the gate; an `IModel`
  completeness test as the gate plus an analyzer for secret-shaped names; a run-time `IModel`
  catalog (G2) over a source generator; an audit platform port the curated PII pages also call;
  `admin.data.read` plus each owning slice's existing permission for gated columns; platform admin
  only for tenants; classification first, then audit, then the browser.
- Side finding for a possible follow-up: the ASP.NET data-protection key ring is persisted as
  plain XML (no `ProtectKeysWith*`), so database read access exposes the master key.

### Gates

- `./bin/dev build`: passed (client-name scan passed; build succeeded, 0 warnings, 0 errors).
- Test files: none. The change adds no production code and no test file, so no test covers it.
- `ganda repo audit`: passed (5 passed, 0 failed, 1 skipped: glossary has no entries).

### How to validate

- Smoke: read `design/inventory.md` and `design/options.md` in this task folder; spot-check the
  inventory against `source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs`
  (`grep -n "ToTable(" …` lists 16 tables) and `postgres-db-context-infrastructure.cs` (13 DbSets).
- Expect: every table in the snapshot appears in the inventory with each column classed; the
  options file covers anchor, redaction and check, generator shape, audit, permissions, tenant
  scope, cost/benefit, and ends with recommendations and open questions and no final decision;
  `git diff origin/master --stat` touches only this task folder.
