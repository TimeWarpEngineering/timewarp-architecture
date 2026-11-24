# Admin entity browser: options

Spike output for task 329. Read `inventory.md` first: it lists every persisted type and the
class of every column. This file lays out the options per concern and ends with recommendations
to discuss. Nothing here is decided.

The rules from the task stay fixed for every option:

1. Secrets are never shown, even to an administrator.
2. PII sits behind its own permission.
3. The browser is read-only. Writes stay with each slice's named commands.
4. Every view is audited.
5. A platform admin view must not leak across tenants once tenants exist.

## Facts from the codebase that shape every option

These came out of the inventory and the project layering. Each one rules something in or out.

- **F1. The four project layers cannot all see the entity types.** `web-contracts` references
  `foundation-contracts` and `timewarp-identity`, not `web-domain`. A generator running in
  `web-contracts` can see `Principal`, `Credential`, and `SiteSettings` (identity library), but
  not `Profile`, `FeedbackItem`, `Role`, or `AgentHumanLink` (web-domain). Only
  `web-infrastructure` sees every entity and the `PostgresDbContext`. So per-entity typed
  contracts cannot be generated from the entities. Either the contract is generic, or each entity
  gets a hand-declared contract view that a check compares with the entity.
- **F2. Generators do not see each other's output.** `ContractStateSourceGenerator`'s Design
  region already works around this for TimeWarp.State and TimeWarp.Mediator. A generator that
  emitted `[Page]` classes into `web-spa` would be invisible to `PageSourceGenerator` in the same
  compilation. A generator that emitted `[ApiEndpoint]` contracts into `web-contracts` would be
  visible to `FastEndpointSourceGenerator`, because that one scans referenced assemblies from
  `web-server`.
- **F3. Generators cannot emit `.razor`.** A generated page is a `RenderTreeBuilder` class, which
  `tw-blazor` and `tw-blazor-css-strategy` do not cover. One hand-written generic page driven by
  column metadata avoids that.
- **F4. EF's mapping is only fully known at run time.** Owned types (`Role`'s permission and
  agent-scope rows, `FeedbackItem`'s activity thread), value converters, shadow properties, and
  ignored members come from `IEntityTypeConfiguration` fluent calls, and some columns map private
  members (`Credential.Handle` is a private field; `RegisteredBrowser` / `RegisteredOs` are private
  properties). A source generator would have to re-parse those fluent chains from syntax. `IModel`
  at run time (or the checked-in migration snapshot) has the exact column list.
- **F5. Package-owned and framework-owned types.** `DataProtectionKey` is an ASP.NET type the app
  cannot annotate with an attribute, but the app owns its `IEntityTypeConfiguration`, so an EF
  annotation there can classify it. `Principal`, `Credential`, and `SiteSettings` live in `timewarp-identity`, which
  is in this repo but ships to generated apps as the `TimeWarp.Identity` package
  (`UseIdentityPackages`). We own that library, so it can carry attributes, but only attributes
  from a package it is willing to depend on.
- **F6. A data taxonomy already exists.** `aspire-service-defaults/data-taxonomy.cs` (task 344)
  defines `PersonalData` and `SensitiveData`, with `[PersonalData]` / `[SensitiveData]` attributes
  built on `Microsoft.Extensions.Compliance`. Log redaction erases both. Today the attributes sit
  on three `[LoggerMessage]` parameters and on no entity property. `SensitiveData` mixes two
  things the browser must treat differently: secrets (never shown) and free text a person wrote
  (shown behind a permission).
- **F7. The browser only exists with the `postgres` flag.** With `postgres` off, every store is
  in memory behind a port, and there is no `DbContext` to read. That is fine: nothing persists to
  browse. The browser's server half compiles under `#if postgres`, like `platform/postgres/**`.
- **F8. There is no audit concern today.** `grep -i audit` finds nothing that records who read
  what. Curated PII reads (task 328's profile fields, task 314's feedback bodies) are not audited
  either.
- **F9. There is no tenant concept today.** Marketplace task 118 is in the backlog. The only
  "tenant" in the code is the Entra tenant in `SiteSettings`, which is an identity-provider
  setting, not a data partition.
- **F10. Every `admin.*` permission is Administrator protected core.** `PermissionIds.IsProtectedCore`
  keys on the `admin` prefix. A new id also needs an entry in `PermissionIds.All`, a seed entry in
  `RolePermissionSeed`, and a data migration (as `AddAdminProfilesReadPermission` did for 328).

## 1. Anchor: which types the browser shows

| Option | What is in scope | For | Against | Package-owned types |
|---|---|---|---|---|
| (a) Every `DbSet<T>` | 13 DbSets plus 3 owned tables | Nothing is forgotten; a new table shows up with no step | Shows tables nobody should browse (`DataProtectionKey`, `FeedbackAttachmentBytes`, `ProtectedSecret`) unless each is excluded; "on by default" is the wrong default for a security surface | Included automatically, so each needs an exclusion or a classification the app writes outside the type |
| (b) `IAggregateRoot` only | `Profile`, `FeedbackItem`, `Role`, `AgentHumanLink` | Small, already marked, matches the consistency boundary | Misses `Principal`, `Credential`, `ProtectedSecret` metadata: the identity rows an operator most needs to inspect. Of the four it shows, three already have curated pages | `Principal` / `Credential` are deliberately not aggregates, so they are out |
| (c) Opt-in `[AdminBrowsable]` | Only types someone marked | Explicit decision per type; matches "fail closed" | A new type is invisible until marked (safe, but easy to forget); cannot mark `DataProtectionKey` | Needs the attribute on the identity types (identity takes a dependency) or a registration in app code |
| (d) Opt-out with mandatory classification | Every mapped type unless `[AdminHidden(reason)]` | Nothing is forgotten, and the classification check (section 2) blocks the build or test run when a column is unclassified, so a new table cannot ship until someone decides about it | More up-front classification work: every column of every table | Classification for framework types goes in app code (see 2c); a framework type can be hidden with a reason |

The anchor and the classification check are linked. Under (d), adding a table forces a decision
about each column. Under (c), adding a table forces nothing; the browser just does not show it.

A note on (a) versus (d): they show the same set. The difference is whether an unclassified
column hides silently (a) or fails the gate (d).

## 2. Redaction model

### 2a. Classes

The browser needs four classes per column. The existing taxonomy (F6) has two.

| Class | Browser behavior | Log behavior today | Examples |
|---|---|---|---|
| Secret | Never selected from the database. The column appears as "hidden" with no value | Erased (`SensitiveData`) | WebAuthn public key and sign data, agent key material, token hashes, `ProtectedSecret` ciphertext, `DataProtectionKey.Xml`, attachment bytes |
| Personal | Selected only when the caller holds the owning PII permission; otherwise null | Erased (`PersonalData`) | Profile display name and email, Entra UPN / object id |
| Sensitive text | Selected only with the owning slice's read permission | Erased (`SensitiveData`) | Feedback title, body, notes, replies |
| Safe | Always shown | Logged | Ids, timestamps, enums, counts, versions, flags |

Option **R1: one taxonomy for logs and the browser.** Split `SensitiveData` into `SecretData` and
`SensitiveData`, and tag entity properties with the same attributes the logger already reads.
One classification serves log redaction and the browser, so they cannot drift. The cost is
moving the taxonomy from `aspire-service-defaults` into a project the domain and the identity
library can reference (both use `Microsoft.Extensions.Compliance.Abstractions` attributes, which
is a small Microsoft package).

Option **R2: separate admin attributes** (`[AdminRedact]`, `[AdminPii(Permission = …)]`,
`[AdminSafe]`) in `TimeWarp.Architecture.Attributes`. Simpler to land, but it is a second
classification of the same fields beside the log taxonomy, which the "when two things must agree"
rule warns against.

### 2b. Default for an unclassified column

- **Fail closed and fail the gate (recommended to discuss).** An unclassified column is never
  selected, and the classification check fails, so it cannot stay unclassified.
- Fail closed silently: hidden, no failure. Safe, but a column nobody classified stays invisible
  forever and nobody notices.
- Fail open with a name pattern list: rejected. A pattern list misses the next secret name.

`Safe` has to be stated too (an attribute, or a type-level "all other columns safe" that names
the exceptions). Without that, "unclassified" and "safe" look the same.

### 2c. The fail-closed check

Two checks, because each catches what the other cannot.

1. **Analyzer (new TWA id) on in-repo entity types.** For each property of a browsable type, the
   analyzer requires a class when the property looks like a secret: `byte[]`, `ReadOnlyMemory<byte>`,
   or a name matching `Key|Secret|Token|Hash|Password|Ciphertext|Credential|Signature|Xml`. It
   gives the author an error at the property, in the IDE. It cannot see EF fluent mapping, shadow
   properties, or framework types (F4, F5).
2. **Model-completeness test (Jaribu) over `IModel`.** Builds `PostgresDbContext`'s model (no
   database needed), walks every entity type, owned type, and mapped property, and fails unless
   each column has a class or its type is hidden with a reason. This one covers owned tables,
   shadow properties, and `DataProtectionKey`. It is the real gate; the analyzer is the early
   warning.

Where the class for a framework type lives (`DataProtectionKey`): an EF annotation set in the
`IEntityTypeConfiguration` the app already owns
(`platform/postgres/data-protection-key-entity-type-configuration-infrastructure.cs`), or an
app-side registry keyed by CLR type and property name. The test reads either. The same EF
annotation route works for the identity types, whose configurations also live in the app
(`features/identity/*-entity-type-configuration-infrastructure.cs`), so the identity library need
not take an attribute dependency at all. The trade: an attribute sits on the property a reader is
looking at; an annotation sits in a different file.

## 3. Generator shape

### 3a. Options

**G1. Generic contracts, compile-time catalog.** One set of contracts in `web-contracts`
(admin-data slice): `ListAdminDataTypes`, `BrowseAdminData.Query { Type, Page, PageSize, SortBy,
SortDescending, Filter }`, `GetAdminDataRow.Query { Type, Id }`. The response carries column
descriptors (key, label, class, value kind) and rows of cell values. A source generator in
`web-infrastructure` reads the browsable entity types and their attributes and emits, per type, a
descriptor and a projection `Expression<Func<T, Row>>` that names only the columns allowed for
the caller's permissions. The SPA has one hand-written list page and one detail page that render
from the descriptors.

- For: projection is compiled and reviewable in generated source; secret columns never leave the
  database; no reflection on the request path.
- Against: the generator has to understand EF mapping from syntax (F4), so owned collections and
  converters are a source of bugs; sort columns are a runtime-validated string, not 327's closed
  enum.

**G2. Generic contracts, run-time catalog from `IModel`.** Same contracts and SPA pages as G1.
No source generator: at start-up the server builds the catalog from `PostgresDbContext.Model` and
the classes from 2a, and builds projection expressions from the model. The model-completeness test
(2c) runs over the same catalog code.

- For: exact column set, owned types and framework types included, no syntax parsing; the gate and
  the browser share one catalog, so they cannot disagree.
- Against: expression trees built at run time (reflection; acceptable on the server, which does
  not publish AOT today); the catalog is only visible by running it, not in generated source. Not
  "source-generated" in the task title's sense.

**G3. Per-entity declared views, typed contracts.** Each browsable type gets a hand-declared view
in `web-contracts` (`[AdminDataView("profile")] partial class ProfileAdminRow { … }` listing the
safe and gated columns), and an analyzer in `web-infrastructure` checks the view against the
entity, the way TWA0039–TWA0042 check contract agreement. A generator emits the query, handler,
FastEndpoint contract wrapper, and `[ContractState]`-ready shape from the view.

- For: typed contracts and closed sort enums, same as 327; each view is a visible decision.
- Against: one declaration per entity, so it is closer to hand-built pages; the SPA still needs a
  page per view or the generic page from G1/G2; F2 means generated `[Page]`s are not possible.

### 3b. What every option shares

- **Paging, sort, filter.** Server-side, as in 327: sort applied before paging with a stable
  tiebreaker on the id; the grid never sorts rows itself; each sort or page change dispatches an
  `AdminDataState` action. Filter in v1 is one text search over columns classed Safe; searching a
  Personal column needs its PII permission, as `ListPrincipals` does (sort and search can leak a
  value through ordering, so they are gated like the value).
- **Read-only by construction.** The server half runs only `Set<T>().AsNoTracking()` queries
  ending in a projection. It never receives a write-capable context: a read-only reader port in
  `web-application`, implemented in `web-infrastructure`, that exposes `IQueryable` only through a
  projection. The contracts are queries only (`IQuery<…>`); there is no command. Defence in depth,
  if wanted: a second Postgres login with `SELECT` only, used by the reader.
- **Slice isolation (TWA0009).** The browser reads every slice's types, which is what TWA0009
  forbids between product slices. It belongs on the platform tier (`platform/admin-data`), which
  product slices may use but which may not reference product slices, so the reader lives in
  `web-infrastructure` composition code (the outside tier, like `PostgresDbContext`), not in a
  product slice. The application handler sees only the reader port.
- **Endpoints and pages.** Contracts carry `[ApiEndpoint]` + `[EndpointAuthorize]`, so
  `FastEndpointSourceGenerator` emits the endpoints unchanged. The SPA pages use `[Page]` with a
  policy and `[StateAccess]` as any page does. `[Operation]` binds a contract to CLI and MCP too:
  the browser should be human-only in v1, so either no `[Operation]` (as `ListPrincipals` today)
  or an `[Operation]` with no agent-scope mapping. To decide.
- **Detail view.** One row with every allowed column, plus links to the curated page when one
  exists (principal → `/Admin/Principals/{id}`, feedback → `/Admin/Feedback/{id}`). The browser
  never duplicates a curated workflow.

## 4. Audit

No audit concern exists (F8), so whichever option wins is a new platform concern.

| Option | What | For | Against |
|---|---|---|---|
| A1. Structured log event | `[LoggerMessage]` per view: caller principal id, type, row id or query shape, columns revealed by class, PII revealed yes/no. Search text tagged `[PersonalData]` so it is erased | Small; goes to OTel today | Retention and integrity belong to the telemetry backend; erasable; not queryable in-app |
| A2. Durable append-only table | `audit.admin_data_access` rows with the same fields, written by the reader before it returns rows | Queryable, retained with the data, can back an "who viewed this" page later | New table, new migration, growth policy needed |
| A3. Port with both sinks | `IAdminAccessAudit` platform port; A1 always, A2 under `postgres` | One call site; curated PII reads (328, 314) can call it too | Two sinks to keep aligned |

Two points hold for every option:

- What is recorded is the access, never the values. A row id and the list of revealed column
  keys, not the email that was shown.
- If audit is a platform concern, the curated pages that already show PII (`ListPrincipals` /
  `GetPrincipal` with `admin.profiles.read`, the feedback item page) should call it too, or the
  browser is audited while the bigger PII surface is not.

## 5. Permissions

| Option | Shape | For | Against |
|---|---|---|---|
| P1 | One read id per entity (`admin.data.profile.read`, …) | Finest grain | Generated per type, but `PermissionIds.All` is a hand catalog in contracts that cannot see entities (F1); each id needs a seed and a migration (F10); a long checkbox list |
| P2 | `admin.data.read` opens the browser; Personal and Sensitive-text columns are gated by the **owning slice's existing permission** (`admin.profiles.read` for profile fields, `admin.feedback.read` for feedback text, `admin.principals.read` for principal rows) | The browser can never show more than the curated page would; no new PII ids; one new id and one migration | Each classified column names a permission id, so the class carries a permission reference |
| P3 | `admin.data.read` plus one `admin.data.pii.read` | Two ids, simple | A second way to see profile PII beside `admin.profiles.read`, so revoking one does not revoke the other |

Seed defaults under any option: the new `admin.*` ids go to Administrator only, as protected
core (F10). Operator, Member, and Developer get nothing. No agent-scope mapping.

## 6. Tenant scope

Nothing to partition today (F9). Options:

- **T1. Platform admin only, stated.** The browser is an `admin.*` capability of the platform
  Administrator and is never granted to a tenant role. When 118 adds tenants, a tenant admin gets
  no browser until a scope rule exists. No seam is built now.
- **T2. Build the scope seam now.** The reader takes a scope predicate per type; v1 passes
  "everything". This is design for a consumer that does not exist yet.

The fail-closed shape for later, whichever is picked: once a tenant key exists, a type with no
declared tenant column is hidden from every tenant-scoped caller.

## 7. Cost and benefit against hand-built pages

Counts come from `inventory.md`.

- 13 `DbSet`s plus 3 owned tables (16 tables).
- Already covered by curated admin pages: `Principal` (with `Profile`, role assignments, and
  agent-scope grants), `Role` (with its permission and agent-scope rows), `FeedbackItem` (with
  attachments and the activity thread), `SiteSettings` and the site xAI key metadata
  (Authentication and AI pages).
- Never browsable whatever is chosen: `DataProtectionKey`, `FeedbackAttachmentBytes`, and the
  `ProtectedSecret` ciphertext. Only metadata of the last is useful.
- Left uncovered: `Credential` metadata, `AgentHumanLink`, and per-user `ProtectedSecret`
  metadata. Three types, all in the identity / agent area. `Credential` and `AgentHumanLink` could
  also be sections on the existing principal detail page instead of pages of their own.

A hand-built read page per type is a query contract, handler, test, state, page, and page test:
three of those (or two sections on the principal detail page) cover today's gap. The browser (any G option) is one generic contract set, the
reader, the catalog, classification of every column (2a), both checks (2c), audit (4), one
permission with its migration, one generic list and detail page, and their tests. That is the
size of several hand-built pages, so for today's three uncovered types the hand-built pages are
cheaper.

The browser pays off when:

- product slices add tables faster than anyone writes admin pages (118 would add catalog, quote,
  order, job, and ledger types);
- the classification is wanted anyway: the same classes drive log redaction (R1), and could later
  drive exports, agent tools, and Ask `@` references;
- an operator needs to inspect a row a curated page does not show, without a developer opening
  a database console (which has no redaction and no audit at all).

The classification and the model-completeness test pay off on their own, before any browser: they
make "this column is a secret" a checked fact for logs today.

## 8. Recommendations to discuss

These are the spike's leanings, with the reason, for Steve to accept, change, or reject. Each is
one decision.

1. **Anchor: (d) opt-out with mandatory classification.** A new table cannot ship without a
   decision about each column, and framework types are hidden with a stated reason.
2. **Classes: R1, one taxonomy for logs and the browser,** with `SensitiveData` split into
   `SecretData` and `SensitiveData`. Needs two smaller decisions: where the class is written
   (attributes on the property, which means hosting the taxonomy where the domain and the identity
   library can reference it; or EF annotations in the configurations the app already owns, which
   covers identity and framework types with no new dependency), and which project hosts the
   taxonomy.
3. **Unclassified default: fail closed and fail the gate.**
4. **Gate: both checks.** The `IModel` completeness test is the gate; the analyzer is the IDE
   warning for secret-shaped names and byte arrays.
5. **Shape: G2 (run-time catalog from `IModel`) over G1.** The column set has to match EF's
   mapping exactly, and only the model knows it. This drops "source-generated" from the server half;
   the SPA half is one generic page either way. G3 is the choice if typed contracts and closed
   sort enums outweigh per-entity declarations.
6. **Audit: A3, a platform port,** and the curated PII pages call it too.
7. **Permissions: P2,** `admin.data.read` plus the owning slice's existing permission per gated
   column.
8. **Tenants: T1,** platform admin only, stated, no seam now.
9. **Order of work, if accepted:** classification and the completeness test first (useful alone,
   and they fix the log taxonomy gap); then the audit port with the curated pages calling it;
   then the browser.

Open questions the recommendations do not settle:

- Should `[Operation]` bind the browser contracts to CLI and MCP, or stay HTTP-only?
- Is a separate `SELECT`-only Postgres login worth it beside the read-only reader?
- Does `Sensitive text` (feedback bodies) belong in the browser at all, given the curated feedback
  page already shows it under the same permission?
