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

## Inventory to start from (cockpit, 2026-10-12)

- `IAggregateRoot` is implemented by only **three** types: `Profile`, `AgentHumanLink`,
  `FeedbackItem`.
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

## Checklist

- [ ] Inventory (`design/inventory.md`)
- [ ] Options for anchor, redaction/analyzer, generator, audit, permissions (`design/options.md`)
- [ ] Discussion with Steve; decisions recorded as a dated decision log here
- [ ] Follow-up implementation task(s) filed from the decisions

## Session

- Created: 2026-10-12 (cockpit, per Steve)
