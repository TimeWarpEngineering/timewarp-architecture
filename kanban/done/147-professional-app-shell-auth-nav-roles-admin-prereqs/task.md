# Professional app shell: auth, nav, roles, admin prereqs

## Description

Prerequisites for treating the template as a real application — **not** under epic **118**
(marketplace). 118 depends on a clean human shell; this program owns auth / nav / roles /
admin hygiene so marketplace work is not bolted onto demo nav.

## Relationship to 118

| Program | Owns |
|---------|------|
| **147** | Professional shell prereqs (nav, roles, policies, admin screens, first-run UX) |
| **118** | `real-domain` flag + agentic marketplace / fleet dogfood domain |

Sequencing: land 147 children needed for honest login/nav/admin **before** 118 multiplies
product pages. 118 may start once shell gates exist; remaining 147 children can still run
in parallel.

## Children

| Id | Scope | Status |
|----|--------|--------|
| **147-001** | Gate demo + developer nav behind Developer (+ route authorize) | done |
| **147-002** | Trim RoleIds to product roles; role→policy map | done |
| **147-003** | Enforce page policies on remaining product routes (Profile, Settings, …) | done |
| **147-004** | Admin principals + roles list screens with real policies | done |
| **147-006** | EF principal→role store behind postgres flag | done |
| **147-007** | Replace EnsureCreated with EF migrations (web Postgres) | done |
| **147-005** | First-run home + login professional chrome | to-do (spec is an empty template — needs elaboration before work; note: appbar/footer chrome largely landed via 156/157/162) |

## Target roles (product)

| Role | Audience |
|------|----------|
| Member | Any passkey principal (default after login) |
| Operator | Marketplace ops / job oversight (118) |
| Administrator | Tenant admin (roles, principals) |
| Developer | Template dogfood only — demos + diagnostics |

## Target nav

- **Everyone:** Home, Settings  
- **Administrator:** Admin (roles, later principals)  
- **Developer:** Demos + Developer tooling  
- **Later / 118:** Marketplace  

## Checklist

- [x] 147-001 demo nav gated
- [x] 147-002 RoleIds trim
- [x] 147-003 remaining page policies
- [x] 147-004 admin list screens
- [x] 147-006 EF principal role store (durable under postgres)
- [x] 147-007 EF migrations replace EnsureCreated
- [ ] 147-005 first-run chrome

## Session

- Created: 2026-08-04
- 147-001 done: demos require Developer; admin Roles/New requires Administrator
- 147-004 done: principal→role store, admin Roles/Principals lists, Administrator policies
- 147-006 done: durable IPrincipalRoleStore via EF
- 147-007 created: EF migrations as schema SSOT (folder task for review)
- 147-007 done (2026-08-06): migrations SSOT + AppHost AddEFMigrations; amended by 155
  (no wait edge — hybrid on-demand); close-out gates flushed and fixed 104-035/164/template
  excludes; full template-smoke matrix green. Only 147-005 remains — its spec is an empty
  template and needs elaboration (header/footer chrome already landed via 156/157/162).
