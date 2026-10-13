# Ask `@` references: site-wide typed resource references backed by list actions

## Why (Steve + cockpit, 2026-10-13)

The Ask panel's current "Tag" button mixes two ideas (see task 346 for the other one):
- It's labelled **Tag** (which reads as "point at something on the screen").
- What it actually does is open a **page-scoped** list of `@` tokens parsed from page_context by
  `AskResourceReferences.FromPageContext` (`web-spa/features/application/agent/ask-resource-references.cs`).
  That parser hard-codes three shapes (`@credential:{id}`, `@profile:{alias}`,
  `@siteSettingsVersion:{n}`). On most pages, for example `/Counter`, it says "Nothing on this page
  to tag". New slices are silently untaggable unless someone edits the parser. That is agreement by
  memory.

The model to copy is Cloudflare's dashboard AI (screenshots: Steve, 2026-10-13):
- There is **no button**. The placeholder says "Type @ to tag a resource or ? for shortcuts".
- Typing `@` opens **"Reference a resource"**, a list of **resource types** (`worker:` Workers,
  `zone:` Websites, `kv:`, `r2:`, `queue:`, `workflow:` …), with ↑↓ navigation, ↵ to choose and esc
  to dismiss.
- Choosing a type (`@worker:`) **fetches the list of that type across the whole account**, not the
  page. Choosing an item inserts `@worker:clef-decision-tester`.

So a resource type is an **explicit list/search action**, not an ad-hoc page fact.

## Design direction

- **A resource type is a list query contract** marked referenceable, for example
  `[ReferenceableResource("feedback", typeof(FeedbackItemId), DisplayName = "Feedback items")]`, or
  an equivalent. Settle the exact shape and record it. It goes on an existing list or search query
  that:
  - takes a search term (add one where missing), and
  - returns items carrying the resource's **typed id** (`[TypedId]`) plus a display label.

  The query runs **as the user**, so `[EndpointAuthorize]` filters what can be referenced. A Member
  never sees admin-only resource types or items.
- **The registry is generated, never hand-maintained.** A source generator (existing generator
  family) emits the `@` type registry from the marked contracts: prefix, display name, typed id,
  and how to call the query. An analyzer enforces the contract shape: a search parameter, a typed
  id plus a label in the item, and a unique prefix (prefer analyzers over convention by memory).
- **Tokens are typed:** `@<prefix>:<id>`. When a message is sent, each token resolves to a typed
  reference the agent can pass straight into catalog action parameters of the matching `[TypedId]`
  type (292 design item 5). The agent receives the label and the typed id. The server
  re-authorizes on use; a token never grants access.
- **Initial resource types** (whatever exists with a list query today; confirm each):
  - feedback items: `ListFeedback` / own `ListMyFeedback` for non-admins;
  - principals and profiles: `ListPrincipals`, admin only;
  - credentials: the current user's own;
  - roles: admin.

  Pick sensible prefixes and record them.
- **UX** (FluentUI v5, follows `tw-blazor`):
  - typing `@` (and the composer's existing affordance) opens the type list, filtered to types the
    user may list;
  - choosing a type shows search-as-you-type results from that query, debounced and paged;
  - keyboard: ↑↓, ↵, esc, and backspace back to the type list;
  - the chosen token is inserted as `@prefix:id`, ideally rendered as a chip with its label.

  Every interaction is a TimeWarp.State action. The handlers call the list queries (TWA0022 /
  TWA0026). Caret insertion into the composer textarea may keep the existing narrowly reasoned
  `[DirectComponentSideEffect]`; re-justify it if it stays.
- **`?` shortcuts.** The Cloudflare placeholder pairs `@` with `?` for shortcuts. If a `?` shortcut
  list fits cheaply, from the existing suggestion cards or catalog actions, include it. Otherwise
  record it as a follow-up.
- **Remove the old mechanism:** `AskResourceReferences.FromPageContext`, the page-scoped
  `AskResourceMenu` list, and the "Nothing on this page to tag" path. The **Tag** button is
  **repurposed by task 346** (element pick). Until 346 lands, remove the button or hide it behind
  `@`. Don't leave it opening the old menu.

## Tests

- Generator and analyzer tests (registry emitted; bad contract shapes rejected).
- Handler and integration: the type list is filtered by permission (Member vs Administrator); a
  search returns only authorized items; a token resolves to the right typed id.
- SPA state: `@` → types → search → insert.
- Playwright: type `@`, pick a type, search, choose; the token is in the composer and is sent.

## Checklist

- [ ] Referenceable-resource marker + generated registry + analyzer
- [ ] Initial resource types wired (feedback, principals, credentials, roles — confirm)
- [ ] `@` UX: types → search → insert, keyboard, permission-filtered
- [ ] Typed token resolution for the agent; server re-authorizes
- [ ] Old page-scoped parser/menu removed; Tag button parked for 346
- [ ] `?` shortcuts included or filed
- [ ] Tests (generator/analyzer, handler/integration, state, Playwright)
- [ ] Skill note (`tw-web-api-contracts` or `tw-blazor`): how to make a resource referenceable
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-13 (cockpit, from a design conversation with Steve; do this before 346)
