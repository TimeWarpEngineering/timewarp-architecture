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

- [ ] Lab slice + page (two tabs) on real credentials data; real Settings and Passkeys pages untouched
- [ ] Server lab endpoints compute offered actions/commands from the real rules (no second implementation)
- [ ] B: catalog-named actions, JSON → args binder, generic run action, fail-closed
- [ ] C: link commands, generic `FollowCommand`, same-origin only, same auth pipeline, fail-closed
- [ ] Ctrl-K contextual-rows hook (TWA0009-clean), cleared on navigation; other pages unchanged
- [ ] Tests per approach + Ctrl-K hook
- [ ] Comparison write-up in Notes, ending in an open question for Steve
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [ ] Do **not** start an AppHost; record the browser check as not performed
- [ ] Implementation review; host `open-pr`

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

## Session

- Created: 2026-10-04 (original framing)
- 2026-10-05: rewritten for a B-versus-C evaluation on a lab page (cockpit, per Steve)

## Results

*(fill when done)*

### How to validate

*(required before done)*
