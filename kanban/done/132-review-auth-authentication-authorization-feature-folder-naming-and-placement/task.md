# Review auth / authentication / authorization feature folder naming and placement

## Description

Three sibling product-slice folders sit under `source/container-apps/web/features/`:

| Folder | What lives there today (spot inventory) |
|--------|------------------------------------------|
| `auth/` | `get-sign-in-token` — legacy Passwordless.dev token mint; `Features.Auth`; `[ClientOnlyContract]`, dormant, unconsumed by SPA |
| `authentication/` | `get-current-user` (modules + roles grants) + `mock-user-ids-contracts.cs` — `Features.Authentication` |
| `authorization/` | `role-ids-contracts.cs` — **namespace is `Features` (substrate), not `Features.Authorization`** |

Parallel SPA trees also exist (not under the features/ grammar root but same names):

| SPA path | Contents |
|----------|----------|
| `web-spa/features/authentication/` | `AuthenticationStateListener`, claims principal factory with roles |
| `web-spa/features/authorization/` | `AuthorizationState`, policy/module requirement registration |

Separately, the going-forward identity story lives under **`features/identity/`** (passkeys, agent keys, session, EF principal store, …).

**Hypothesis (Steve):** in common English and security vocabulary, **auth ≈ authentication ∪ authorization**. Three peer folders with those names are hard to reason about — either they are not three real product concerns (and should collapse/rename), or they *are* three concerns and the labels are wrong.

This task is a **naming + placement review with a disposition**, not a bulk move. Decide what each folder *is*, what it should be called, and how it relates to `identity/` and the 118 host split (web human plane vs api agent plane) before 118 marketplace slices land more auth-adjacent surface.

## Requirements

### Questions to answer (write into `disposition.md`)

1. **Are these three different concerns?**  
   If yes: what is each concern's one-line definition, and what names make that obvious without the "auth ⊂ authn+authz" collision?  
   If no: which folders should merge, and under what single slice name / namespace?

2. **Does `auth/` earn a top-level slice?**  
   Today it is essentially one legacy Passwordless contract+handler. Candidates: fold into `identity/`, rename, or schedule delete under 104-016/104-021 (already noted on the contract Design region).

3. **Is `GetCurrentUser` authentication or authorization (or neither)?**  
   The contract returns module/role *grants* for the signed-in user — reads like authorization/session projection, lives under `authentication/`. SPA `AuthorizationState` fetches it. Name/placement should match the real job.

4. **Is `RoleIds` a product slice at all?**  
   File path says `features/authorization/`; namespace says substrate `Features`. That mismatch is already a smell (TWA0009 / placement). Substrate catalog vs product slice vs platform cluster — pick one and align path + namespace + grammar.

5. **How do these relate to `features/identity/`?**  
   Identity is the live principal/credential story. Avoid a fourth near-synonym without a crisp boundary (e.g. "who you are / prove it" vs "what you may do" vs "demo ERP role catalog").

6. **118 host-role mapping (web vs api):**  
   Task 118 records **web = human plane** (passkey/session) and **api = agent plane** (bearer/x402). Which of today's auth*/authentication*/authorization* pieces are web-only, which should move (or be dual-hosted) when marketplace endpoints target api-server, and which are template-demo scaffolding that should stay behind a flag or go away?

### Constraints

- Slice isolation (TWA0009): product `…Features.<Id>` must not cross-reference other product slices; substrate/platform placement is the escape for shared ids like `RoleIds`.
- Feature filename grammar + `tw-feature-placement` skill apply to any rename/move.
- Do not silently re-host dormant Passwordless `GetSignInToken` as a public endpoint (existing security note on the contract).
- Prefer decision + small follow-on tasks over a mega-rename PR mixed with behavior change.

### Deliverables (this folder)

1. **`inventory.md`** — complete file/namespace map for auth, authentication, authorization under `web/features/` **and** `web-spa/features/`; note any handlers/endpoints still live vs client-only/mock.
2. **`disposition.md`** — answers to the six questions; chosen taxonomy (table: concern → folder → namespace → host family web/api/spa); explicit **reject / defer / do now** for each rename or move.
3. If work remains: **child or sibling kanban tasks** created via `ganda kanban create` (never hand-numbered) for mechanical renames/moves.

### Done criteria

- Disposition recorded and committed under this folder
- No unresolved "three sibling names that mean the same thing" — either collapsed, renamed with a clear glossary, or justified as three distinct concerns with non-colliding names
- 118 implications noted so marketplace scaffolding does not invent a fourth parallel auth tree on the wrong host

## Checklist

- [x] Inventory `web/features/{auth,authentication,authorization}` + SPA twins + touchpoints from `identity/`
- [x] Answer the six questions in `disposition.md`
- [x] Propose final folder/namespace names (and what not to call things)
- [x] Call out web vs api placement for each surviving concern (118)
- [x] Create follow-on implement tasks only for accepted renames/moves
- [x] Commit artifacts; mark done when disposition is accepted (implementation may live in children)

## Notes

- Related: **118** (real-domain showcase / host-role mapping web human vs api agent); **104-016 / 104-021** (retire Passwordless / template flag placement); **182** (permission engine under `authorization/`); **identity** slice is the modern authN path.
- Original brief’s three `web/features/` peers are already gone or transformed (104-016/021, 182). Remaining collision is SPA `authentication/` + `account/` + `identity/`.
- Follow-on **132-001**: fold SPA authentication and account login UX into identity (keep `/authentication/{action}`, `/Login`, `/Logout`).

## Session

- Created: 2026-07-28 — scaffolding from folder inventory + 118 host split note
- Implementer: Grok session (2026-09-04) — inventory + disposition against current overnight tree
- Review: Grok (2026-09-04) — effort 1 general; rounds 1–3; host review oracle
- Naming disposition: folder `disposition.md`
- Review disposition: `review/disposition.md` — **clean** (0 open)

## Results

Disposition recorded under this folder. No product-code moves on this id (brief: decision + small follow-on).

### What landed

- `inventory.md` — file/namespace map for remaining auth-adjacent trees (web features, SPA twins, web + api identity-host, api agent-bearer sample). Original `features/auth/` and `features/authentication/` are **absent**; `authorization/` is the 182 permission engine; `RoleIds` is substrate under `admin/roles/`; `GetCurrentUser` is identity `[ClientOnlyContract]`.
- `disposition.md` — answers to the six questions; glossary; taxonomy table; reject/defer/do-now.
- Child **132-001** — mechanical SPA fold of `authentication/` + `account/` login UX into `identity/`.
- Review kitchen under `review/` (framework, three rounds, merged ledgers, disposition).

### Key decisions

- Two product concerns: **Identity** (prove who) and **Authorization** (what you may do). Bare **Auth** is forbidden. **Admin** is catalog CRUD, not a synonym.
- Authorization engine stays Features **substrate** in folder `authorization/` (182 / TWA0009). Do not isolate as `Features.Authorization`.
- `GetCurrentUser` is mock/Entra grants projection, not who-am-I (`GetCurrentSession`). Keep under identity; do not re-host; defer rename.
- 118: do not invent `api/features/auth*`. `api/platform/identity-host/` is **already live** (bearer validation). Web owns `RoleIds` / `PermissionIds` / `AuthenticationSchemeNames` today; api does not reference them. Dual-host should reuse those catalogs + evaluator. Duplicated `AgentTokenDefaults`: keep token claim-type strings aligned; policy-name constants already differ (not byte-identical).

### Implementation review

- **Rounds:** 3 (effort 1, roster: general)
- **Final counts:** bug 2 fixed / 0 open / 0 wontfix; suggestion 0; nit 0
- **Disposition:** `clean` (`review/disposition.md`)
- **M1:** 118 map overstated catalog sharing and treated api identity-host as future — fixed in inventory §9 + taxonomy/Q6
- **M2:** “byte-identical” `AgentTokenDefaults` overstated — fixed; token claim types vs divergent policy names
- **Paths:** `review/review-framework.md`, `review/round-3/merged.md`, `review/disposition.md`

### Files changed

- `kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/{task,inventory,disposition}.md`
- `kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/review/**`
- Child kitchen **132-001** (separate task branch)

### Test outcomes

No product code changed on this id. Inventory paths verified in-tree (`test ! -d features/auth`, `test ! -d features/authentication`). Review re-verified `api/platform/identity-host/` and web vs api `AgentTokenDefaults` divergence.

### How to validate

**Smoke**

```bash
# from repo root (this worktree)
test ! -d source/container-apps/web/features/auth
test ! -d source/container-apps/web/features/authentication
test -d source/container-apps/web/features/identity
test -d source/container-apps/web/features/authorization
test -d source/container-apps/api/platform/identity-host
test -f source/container-apps/web/features/admin/roles/role-ids-contracts.cs
test -f source/container-apps/web/features/identity/get-current-user/get-current-user-contracts.cs
test -d source/container-apps/web/projects/web-spa/features/authentication
test -d source/container-apps/web/projects/web-spa/features/account
rg -n "namespace TimeWarp.Architecture.Features" \
  source/container-apps/web/features/admin/roles/role-ids-contracts.cs \
  source/container-apps/web/features/authorization/permission-ids-contracts.cs \
  source/container-apps/web/features/identity/get-current-user/get-current-user-contracts.cs
rg -n "byte-identical" \
  kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/inventory.md \
  kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/disposition.md
# expect: no matches
```

**Expect**

- First two `test ! -d` commands succeed (no `auth/` or `authentication/` under `web/features/`).
- Identity, authorization, api identity-host, `role-ids-contracts.cs`, `get-current-user-contracts.cs` exist.
- SPA `authentication/` and `account/` still exist (fold is **132-001**, not this id).
- `role-ids-contracts.cs` and `permission-ids-contracts.cs` are `namespace TimeWarp.Architecture.Features;` (substrate).
- `get-current-user-contracts.cs` is `namespace TimeWarp.Architecture.Features.Identity;` and carries `[ClientOnlyContract]`.
- Folder `disposition.md` answers questions 1–6 and contains the reject/defer/do-now table plus 118 host map (`api/platform/identity-host/` already live; catalogs web-owned today).
- `review/disposition.md` outcome is **clean**; last merged counts are 0 open.
- Child **132-001** is on origin-home inbox (`kanban/to-do/132-001-fold-spa-authentication-and-account-login-ux-into-identity/task.md`; from origin-home: `ganda kanban show 132-001`) with parent 132 and depends-on 132. This overnight worktree will not list it until master is merged.

**Automated gate**

None on this id (docs/kanban only). Product-code gate is **132-001** after the fold (`./bin/dev build` 0/0 + SPA login return-url tests).

**Not in scope**

- SPA folder moves, GetCurrentUser rename, dual-hosting authorization onto api-server (118).
- `ganda kanban done` / PR create (host nodes).

