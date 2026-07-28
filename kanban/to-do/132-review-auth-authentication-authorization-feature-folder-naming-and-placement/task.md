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

- [ ] Inventory `web/features/{auth,authentication,authorization}` + SPA twins + touchpoints from `identity/`
- [ ] Answer the six questions in `disposition.md`
- [ ] Propose final folder/namespace names (and what not to call things)
- [ ] Call out web vs api placement for each surviving concern (118)
- [ ] Create follow-on implement tasks only for accepted renames/moves
- [ ] Commit artifacts; mark done when disposition is accepted (implementation may live in children)

## Notes

- Related: **118** (real-domain showcase / host-role mapping web human vs api agent); **104-016 / 104-021** (retire Passwordless / template flag placement); **identity** slice is the modern authN path.
- SPA page `Authentication.razor` / `RedirectToLogin.razor` still under `Features.Authentication` — any rename must include SPA + contracts + tests in one coherent plan.
- Suggested glossary starting point (for review to accept or reject, **not** decided):
  - **Identity** — principal, credentials, ceremonies (prove who)
  - **Session / current-user** — signed-in projection used by UI (may not need its own top-level name)
  - **Authorization** — policies, modules, roles, route guards
  - Avoid bare **Auth** as a peer of Authentication and Authorization

## Session

- Created: 2026-07-28 — scaffolding from folder inventory + 118 host split note
- Review: _pending_
- Disposition: _pending_

## Results

_Fill after disposition is accepted._
