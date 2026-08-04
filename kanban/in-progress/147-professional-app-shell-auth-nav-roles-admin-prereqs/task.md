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
| **147-005** | First-run home + login professional chrome | to-do |

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
- [ ] 147-005 first-run chrome

## Session

- Created: 2026-08-04
- 147-001 done: demos require Developer; admin Roles/New requires Administrator
- 147-004 done: principal→role store, admin Roles/Principals lists, Administrator policies
