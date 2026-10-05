# Hypermedia evaluation: server-offered actions into TimeWarp.State and Ctrl-K (approaches B and C)

## Description

Vet whether hypermedia fits this architecture: the server tells the client which actions are
valid right now, instead of the client computing it. Build **two approaches side by side on one
lab page**, then write a comparison so Steve can decide: adopt B, adopt C, or neither. This is an
**evaluation**. Its deliverable is working code for both plus the written comparison. Whatever
loses is removed before anything ships in the template.

Rewritten 2026-10-05 (cockpit, per Steve), after reading the current code. This replaces the
earlier "toy sample endpoint" framing.

### How the client works today (facts this task builds on)

- Every user interaction is a TimeWarp.State action. Components only dispatch (TWA0026). Direct
  mediator `Send` in SPA code is banned (TWA0022). Handlers do side effects and must not dispatch
  other actions (TWS0002).
- Ctrl-K (`features/application/command-palette*`): `CommandPaletteRoster.BuildAsync` builds rows
  from three static sources: `PageRegistry.All`, `IActionCatalog.Entries` (Human/Both, no
  required parameters, permissions via `IAuthorizationService`), and the principal. **There is no
  hook for page-contributed (contextual) rows.** `CommandPaletteRunner` runs a command row with
  `ActionCatalogEntry.Execute(store, [], ct)`, always with no arguments.
- The action catalog (TimeWarp.State 12.0.0-beta.7) has `IActionCatalog.Find(name)` and a
  reflection-free `Execute(IStore, object?[] args, ct)`. **Nothing binds JSON to the positional
  `object?[]` args.** `ActionCatalogArguments.Get<T>` converts primitives and strings, not
  `JsonElement`.
- The SPA calls the server only from handlers, via typed `IApiService.GetResponse(IApiRequest)`
  (contract `[ApiRoute]`, generated routes, `OneOf<Response, SharedProblemDetails>`).
- "What's valid now" is computed on the client today, duplicating server rules:
  - Revoke: `CredentialsState.CanUnlink(count) => count > 1` mirrors the server's 409
    LastCredential rule, with a race the mirror does not close.
  - Link Microsoft 365: `CanLinkMicrosoft365(offered, count)`.
  - Role editing: `CanManage`.
- Precedent: `GetCredentials` already ships server-derived `IsActive`. `GetHumanUx` returns
  `Actions: [{Id, Label, Href}]`, which is hypermedia for agents only.

## The two approaches

**B: responses name catalog actions.** A response carries offered actions such as
`[{ name: "Credentials.RevokeCredential", label, args: { credentialId: "…" } }]`.

- The client resolves each one through `IActionCatalog.Find`, binds `args` to the entry's
  parameters, and runs it through `Execute`, via one generic action (for example
  `RunOfferedAction`) whose handler does the work.
- The catalog is the allow-list: the server can only offer actions the client already has.
- The server still enforces permissions on the real endpoint.
- Needs:
  - a JSON → `object?[]` binder that uses `ActionCatalogParameter` (ClrType, JsonSchema);
  - a contextual-rows hook in Ctrl-K.

**C: follow the server's links (Siren-style).** A response carries commands such as
`[{ rel, label, method, href, bodySchema?, body? }]`.

- One generic action, `FollowCommand(command)`, has a handler that sends the request to `href`;
  the returned payload becomes the new state and the new menu.
- The client is a generic interpreter, with no client knowledge of the command.
- Needs:
  - a generic, untyped request path through `IApiService`, or an alternative, recorded;
  - the same contextual-rows hook in Ctrl-K.

**Out of evaluation:** `timeWarpState.DispatchRequest(typeName, json)` / `JsonRequestHandler`. It
resolves any type with `Type.GetType` and has no allow-list. Do not build on it. That problem is
tracked separately in timewarp-state task 096.

## Requirements

1. **Lab page:** one new page in its own slice (for example `features/hypermedia-lab/`), with two
   tabs, B and C. It uses the **real credentials data and rules**: list credentials, Revoke,
   Rename, and Link Microsoft 365 where the server says it is valid.
   - Do **not** modify the real Settings or Passkeys pages, or `CanUnlink` and its mirrors. That is
     the adoption follow-up.
   - Gate the page with `[Page(... Policy = …)]` (signed-in), and make it `Navigable` so Ctrl-K and
     the NavMenu can reach it.
2. **Server side:** add lab endpoints that return the credentials data plus the offered
   actions/commands, computed **on the server** from the same rules the real handlers enforce.
   Reuse the existing handlers and rule code; do not write a second implementation.
   - For C, the command endpoints the links point to may be the existing typed endpoints where
     possible.
   - Contracts follow the house rules: `[ApiEndpoint]` plus exactly one of `[EndpointAuthorize]`
     or `[EndpointAllowAnonymous]`, Validator, mock factory, serialization round-trips.
   - Do **not** add hypermedia fields to existing contracts.
3. **Client side:** the payload goes into a TimeWarp.State feature state, and the page renders
   from the store.
   - Offered actions/commands appear as **buttons** and as **Ctrl-K rows while the lab page is
     current**. Only the latest payload's actions are shown; anything the server didn't offer has
     no button, no row and no other way to invoke it.
   - After an action completes, the follow-up payload may offer a different set (for example,
     revoke down to one credential and Revoke disappears).
4. **Ctrl-K contextual rows:** add the smallest hook that lets the current page contribute rows,
   for example a `CommandPaletteState.SetContextualRows` action plus clearing them on navigation.
   - Respect TWA0009: Applications is the platform tier and must not reference the lab slice's
     types. Use a substrate-level row shape, or `[CrossSliceReference]` with a reason.
   - The existing roster (PageRegistry plus catalog) is unchanged for other pages.
   - The runner gains the ability to run a contextual row: B through the catalog with args, C
     through `FollowCommand`.
5. **B specifics:** put the JSON → args binder in the template first, beside the palette or in a
   lab-adjacent platform location. It moves into TimeWarp.State only if B is adopted. The server
   must only ever name catalog entries that exist, and the client rejects unknown names and
   failed binding, failing closed with a notification.
6. **C specifics:** record how the generic request is built (method, href, body) without
   bypassing auth: the same bearer-token pipeline as `IApiService`. Only follow same-origin,
   relative `href`s; refuse anything else, failing closed.
7. **Rules:** zero TWA0022/TWA0026 violations; outcomes go through `NotificationState` (TWA0025);
   handlers don't dispatch (TWS0002); Purpose/Design regions on every new file.
8. **Tests:** co-located Jaribu or the existing web-spa and web-server integration suites.
   - For each approach: the offered set matches the server rule; invoking runs the real operation;
     the follow-up payload changes the set; a non-offered action can't be invoked.
   - B: unknown catalog names and bad args fail closed. C: a cross-origin href is refused.
   - The Ctrl-K contextual rows appear on the lab page and are cleared when you leave it.
9. **Comparison write-up** in this task's Notes (the main deliverable). For B and C, cover:
   - new plumbing (files and lines);
   - how naturally it fits TimeWarp.State, the catalog and the typed contracts;
   - type safety and compile-time checking;
   - the security model;
   - server/client coupling;
   - the Ctrl-K integration cost;
   - testability;
   - what an agent or WebMCP surface (task 271) could reuse.

   End with a recommendation (B, C, or neither) **as an open question for Steve. Do not decide.**

## Checklist

- [x] Lab slice + page (two tabs) on real credentials data; real Settings and Passkeys pages untouched
- [x] Server lab endpoints compute offered actions/commands from the real rules (no second implementation)
- [x] B: catalog-named actions, JSON → args binder, generic run action, fail-closed
- [x] C: link commands, generic `FollowCommand`, same-origin only, same auth pipeline, fail-closed
- [x] Ctrl-K contextual-rows hook (TWA0009-clean), cleared on navigation; other pages unchanged
- [x] Tests per approach + Ctrl-K hook
- [x] Comparison write-up in Notes, ending in an open question for Steve
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [x] Do **not** start an AppHost; record the browser check as not performed
- [x] Implementation review (effort 3, disposition accepted-exceptions)
- [ ] Host `open-pr`

## Notes

- **After Steve decides:** a follow-up task adopts the winner on the real Credentials pages, deletes
  `CanUnlink` / `CanLinkMicrosoft365` and their mirrors, and removes the lab page and the losing
  approach. If the answer is "neither", it removes the lab entirely. The lab must not ship in a
  template release as-is.
- Related: task 239 (Ctrl-K), task 260/265 (interactions are actions, TWA0026), task 271 (agentic UI
  over the catalog), timewarp-state 092/094 (catalog, DisplayName), timewarp-state 096
  (`JsonRequestHandler` allow-list).
- Out of scope, unchanged from the original: the QuickBooks-compatible API surface, the TimeWarp
  Financial app, ledgers and actor runtimes. Do not add hypermedia to existing JSON endpoints.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

### B vs C comparison (evaluation deliverable)

Both approaches run on `/HypermediaLab` (two tabs) against the real credentials. The server
decides what is offered from one place: `LabCredentialSnapshot` sends the real `GetCredentials` and
`GetEntraSignInOffered` requests through `ISender`, and the decision comes from Identity's new
`CredentialRules` (`CanRevoke`, `HoldsMicrosoft365`, `CanLinkMicrosoft365`). `RevokeCredential.Handler`
and `EntraTicketProcessor` now call the same predicates, so there is no second implementation. B and
C are the same decisions written two ways.

**Shared plumbing (needed by either approach):**

| File | Lines |
|------|-------|
| `credential-rules-application.cs` | 30 |
| `lab-credential-snapshot-application.cs` | 81 |
| Ctrl-K hook: `i-command-palette-context-source.cs` + `command-palette-context.cs` | 21 + 47 |
| Ctrl-K hook: Contextual row kind / `RequiresInput` | +30 |
| Ctrl-K hook: `Open` append | +9 |
| Lab state, tab, page, context source, row mapper | ~51 + 34 + 279 + 28 + 111 |

**1. New plumbing.**
- **B** adds about 450 lines. The contract and handler are 152 + 61. The JSON → `object?[]`
  binder, `ContextualActionArguments`, is 98. The runner's contextual path and the follow-up refresh
  are about 90. `FetchCredentialOffers` is 53.
- **C** adds about 545 lines. The contract and handler are 209 + 65. `FollowCommand` is 160.
  `FollowedLinkRequest`, the untyped `IApiRequest`, is 44. `AppRelativeHref` is 24.
  `FetchCredentialCommands` is 42.
- **C's Ctrl-K rows also need B's machinery.** A palette row can only run a catalog entry, so a C
  row is the catalog entry `HypermediaLab.FollowCommand` with arguments `{method, href, fields}`.
  Those arguments are bound by B's binder. C therefore costs C's plumbing *plus* most of B's.

**2. Fit with TimeWarp.State, the catalog and the typed contracts.**
- **B fits naturally.** An offer is just "run this cataloged action with these arguments". The
  real `Credentials.RevokeCredential` / `RenameCredential` actions and their handlers, notifications
  and `TrackAction` run unchanged. After the action, the runner dispatches the parameterless
  `FetchCredentialOffers` to refresh, so no handler dispatches another action (TWS0002).
- **C sits beside the store rather than in it.** One generic handler sends whatever the payload
  says and then GETs `Self`. The real Credentials actions never run, so their outcomes and state
  updates (for example `CredentialsState`) are bypassed.
- **C needed workarounds:**
  - an untyped request type, `FollowedLinkRequest`, which uses `[JsonExtensionData]` as the body;
  - a private `[JsonConstructor]` so the store can round-trip it;
  - a placeholder response type, `Ignored`;
  - a workaround for a TimeWarp.State beta.8 ActionSet generator gap with nullable generic
    parameters (recorded in the `FollowCommand` Design region).

**3. Type safety and compile-time checking.**
- **B:** offered names are `OfferedActionNames` constants. A test pins that every name the server
  can emit resolves in the SPA catalog. Arguments are type-checked at *run time* against
  `ActionCatalogParameter.ClrType` (unknown name, missing required argument, wrong type → refused).
  Nothing ties the server's argument names to the action's constructor at compile time; a generator
  could close that gap if B is adopted.
- **C:** hrefs are strings built from the generated routes on the server (good), but the client
  checks nothing about them except "same-origin and in the current payload". A body/field mismatch
  shows up only as the target endpoint's 400.

**4. Security model.**
- **Both** are fail-closed allow-lists over the *current* payload:
  - a contextual row runs only if the current page still contributes exactly that row
    (`CommandPaletteContext.IsOffered`, record equality including arguments);
  - user input may only fill parameters the offer left unbound, and can never override an argument
    the server set;
  - the server re-enforces permissions and rules on the real endpoint (for example the 409
    LastCredential backstop).
- **B** adds a second allow-list, the client's catalog: the server can only ask for something the
  client already ships. In the lab, contextual rows do not check the catalog entry's `Visibility` or
  `Permissions` (the server decided and re-enforces). So *any* shipped catalog entry is offerable.
  If B is adopted, check Visibility/Permissions, or an offerable-names list, before `Execute`
  (review M4).
- **C's allow-list is "same origin"** (`AppRelativeHref`: rejects absolute, `//`, `/\`, backslash,
  whitespace and control characters, and `javascript:`). After that, any app endpoint the bearer
  token can reach is a candidate. A compromised or buggy payload can aim the user's token at any
  same-origin route. B cannot.
- **C has a `NAVIGATE` method** (full-page redirect, used for the Entra link challenge). It widens
  the surface further.
- **Both** send requests through `IWebServerApiService`, so they use the same bearer-token pipeline;
  neither has a second HTTP path.

**5. Server/client coupling.**
- **B** couples the server to client *action names and parameter names*. That is a new contract
  surface, which the catalog test pins today.
- **C** couples the client to nothing domain-specific. It is the classic hypermedia decoupling, but
  it is paid for in items 2, 3 and 4. The server must also know its own routes, which it already does
  through the generated `[ApiRoute]` members.

**6. Ctrl-K integration cost.**
- The hook is the same for both: a pull-based `ICommandPaletteContextSource` port in Applications
  (TWA0009-clean, no lab types).
- Rows are "cleared on navigation" by construction, because a source returns nothing for another
  path. There is no `SetContextualRows` action and no navigation listener.
- Rows that need user input (Rename's nickname, C commands with `Fields`) stay as page buttons but
  are left out of the palette, which has no argument UI.
- For C, the rows go through the B binder, as noted in item 1.

**7. Testability.**
- **B** tests through the real catalog and real actions with a scripted BFF. Assertions are simple:
  "this action ran with these arguments".
- **C** needs the BFF script to understand raw POST hrefs and bodies, and its success path skips
  the real Credentials handlers. Tests show the HTTP happened, not the domain action.
- Both have web-server endpoint tests that pin the offered set against the real handlers.

**8. Reuse by an agent or WebMCP surface (task 271).**
- **B** reuses directly. The catalog already has `Visibility`, `Permissions` and JSON schema per
  parameter. An agent could take a server offer as `{name, args}`, validate it with the same binder,
  and execute it. Offer = tool call.
- **C** gives an agent raw HTTP affordances (`rel`/`href`/`method`/`fields`), which is closer to
  today's agent-only `GetHumanUx` `Actions`. It is usable by a generic HTTP agent but has no
  catalog-level schema or permission metadata.

**Lab artefact, not a finding against either approach:** B and C refresh independently. After a B
revoke, the C tab and its Ctrl-K rows stay stale until C refetches; the server's 409 still holds
(review M2). Adoption keeps one approach, which removes this.

**Not performed:** a browser check. No AppHost was started, as this task required. All behaviour
was validated headless (see Results).

**Open question for Steve — adopt B, adopt C, or neither?**

The evidence above leans B:
- it reuses the catalog as the allow-list and runs the real actions;
- C's palette integration ends up depending on B's binder anyway;
- C widens the token's reach to any same-origin route.

C's argument is server-side evolvability without client releases. That only matters if the client
should be able to run actions it was not built with, and the catalog design deliberately says it
should not. "Neither" stays reasonable if the duplicated client-side `CanUnlink` /
`CanLinkMicrosoft365` mirrors are cheaper to keep than a new server↔client name contract.
**Not decided here.**

## Session

- Created: 2026-10-04 (original framing)
- 2026-10-05: rewritten for a B-versus-C evaluation on a lab page (cockpit, per Steve)
- 2026-10-05: implement oracle (headless, resumed after an earlier failed run). Committed the prior
  run's uncommitted work, then fixed: `HypermediaLabState.Clone` must give a new Guid (InvalidCloneException);
  `FollowedLinkRequest` needs a private `[JsonConstructor]` (extension data cannot bind to a ctor
  parameter); test usings. Added web-server endpoint tests and contract round-trips. All gates green.
- 2026-10-05: review oracle (Claude Opus 5.5, headless). Effort 3; reviewers general, security and
  tests (Sonnet subagents). Fixed M1 (palette `@key` collision), M3 and M5. Disposition accepted-exceptions.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-05T04:00:08Z

## Results

Delivered: lab slice `features/hypermedia-lab/` (server: `GetCredentialOffers` (B) +
`GetCredentialCommands` (C) endpoints over `LabCredentialSnapshot` / Identity `CredentialRules`,
reused by `RevokeCredential.Handler` and `EntraTicketProcessor`), SPA `HypermediaLabState` + page
`/HypermediaLab` (two tabs, `[Page(Policy = CredentialManageSelf, Navigable = true)]`, NavMenu "Labs"),
B binder `ContextualActionArguments`, C `FollowCommand` + `FollowedLinkRequest` + `AppRelativeHref`,
Ctrl-K contextual-rows hook (`ICommandPaletteContextSource` / `CommandPaletteContext`, runner
`RunContextualAsync`). Real Settings / Passkeys pages and `CanUnlink` / `CanLinkMicrosoft365` untouched.
Comparison write-up: Notes → "B vs C comparison", ending in an open question for Steve.

Gates (2026-10-05, this worktree): `dev build` 0 warnings / 0 errors; `dev test` all suites passed
(0 failed); `dev template-smoke` SUCCEEDED; `ganda repo audit` passes all checks. Browser check
**not performed** (no AppHost, by requirement).

### How to validate

Smoke:

```bash
./bin/dev build
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class HypermediaLab
cd ../web-server-integration-tests && dotnet test -c Release -- --filter-class HypermediaLab
cd ../web-contracts-tests && dotnet test -c Release
```

Expect: build 0/0. SPA lab suite 23/23. It covers:
- B: offered Revoke runs and refreshes to the follow-up set; Rename with input; unknown catalog name
  and bad args fail closed; a non-offered row is refused.
- C: the offered link is followed and the payload refreshes from `Self`; cross-origin, `//`, `/\`,
  `javascript:` and relative hrefs are refused; a non-offered command is refused.
- Ctrl-K rows appear only on `/HypermediaLab`.

Server lab suite 11/11: offered sets match the real rule (Revoke only above one active credential),
a real revoke changes the follow-up set, unauthenticated requests get 401, and C hrefs are
app-relative. Contracts 57/57, including the lab round-trips.

Optional manual check (not performed here): `dev run`, sign in, open `/HypermediaLab`, revoke down
to one credential on each tab and watch Revoke disappear; press Ctrl-K on the lab page and elsewhere.

### Implementation review

- **Effort and rounds:** effort 3, 2 rounds. Round 1 had three reviewers (general, security,
  tests). Round 2 was the orchestrator re-verifying the fixes.
- **Final counts:**
  - bug: 1 fixed;
  - suggestion: 3 wontfix;
  - nit: 2 fixed, 2 wontfix;
  - **0 open.**
- **Disposition:** `accepted-exceptions`.
- **Fixed:**
  - M1: the Ctrl-K row `@key` collided for contextual rows sharing a catalog Target, so the palette
    would throw when rendering on the lab page. The key is now the whole row.
  - M3: the ranker's Design region now matches the sort order.
  - M5: a JSON `null` for a required parameter is refused (new test input; lab suite 24/24, palette
    35/35, `dev build` 0/0).
- **Wontfix (lab scope):**
  - M2 and M4 are recorded in the comparison above.
  - M6, M7 and M8: rationale is in the disposition.
- **Paths:**
  - `review/review-framework.md`
  - `review/round-2/merged.md` (ledger in `review/round-1/merged.md`)
  - `review/disposition.md`
