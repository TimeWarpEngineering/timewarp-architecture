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

- [x] Referenceable-resource marker + generated registry + analyzer
- [x] Initial resource types wired (feedback, principals, credentials, roles — confirm)
- [x] `@` UX: types → search → insert, keyboard, permission-filtered
- [x] Typed token resolution for the agent; server re-authorizes
- [x] Old page-scoped parser/menu removed; Tag button parked for 346
- [x] `?` shortcuts included or filed
- [x] Tests (generator/analyzer, handler/integration, state, Playwright)
- [x] Skill note (`tw-web-api-contracts` or `tw-blazor`): how to make a resource referenceable
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-13 (cockpit, from a design conversation with Steve; do this before 346)
- 2026-10-13 implement oracle (ganda task work): marker, analyzer, generator, state, picker,
  page_context resolution, tests, skill note; gates below.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-13T14:03:36Z
- Review oracle (tw-implementation-review, effort 3, roster general): claude-opus-5-5 orchestrator + general reviewer subagents (session ids not reported); 2 rounds; fixes 4181c4291 + round-2 fix — 2026-10-13
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-13T14:23:14Z

## Results

### Review disposition

- **Outcome:** accepted-exceptions. 0 open after 2 rounds. Effort 3; reviewer roster: general.
- **Final counts:** bug 0; suggestion 5 fixed / 1 wontfix; nit 4 fixed / 3 wontfix.
- **Fixed:** analyzer rejects shapes the generator can't compile, and Policy is required (TWA0033); search transport, JSON and null-body failures become a failed search; picker state resets when the principal changes; `@` opens the picker only at the start of a word; the inserted token gets a trailing space; Design notes on the stamped UserId and on case folding.
- **Wontfix:**
  - M3: cross-assembly duplicate prefixes, because the template has one contracts assembly.
  - M10: the race test and the remaining analyzer branches are not covered.
  - M12: a fast keystroke after `@` can keep the picker from opening.
  - M13: Open has no guard if the principal changes mid-flight.
  - Rationale for each is in disposition.md.
- **Artifacts:** `review/review-framework.md`, `review/round-1/{general,merged}.md`,
  `review/round-2/{general,merged}.md`, `review/disposition.md`.

### Settled shape (recorded)

- **Marker:** `[ReferenceableResource(prefix, displayName, IdProperty = …, LabelProperty = …,
  ItemsProperty = "Items")]` on the outer `[ApiEndpoint]` contract class
  (`source/analyzers/timewarp-architecture-attributes/referenceable-resource-attribute.cs`).
  Property names are strings so contracts use `nameof(Contract.Item.X)`.
- **Shape rules** (`source/analyzers/shared/referenceable-resource-shape.cs`, linked into both
  analyzer packages): `[ApiEndpoint]` + `[EndpointAuthorize]`; nested `Query` with a public
  settable `string? Search`; nested `Response` with an enumerable items property; item
  `IdProperty` is a `[TypedId]` struct **or `Guid`**; item `LabelProperty` is a string; prefix is
  `[a-z][a-z0-9-]{0,31}`.
- **Analyzer:** `ReferenceableResourceAnalyzer` — **TWA0033** (shape) and **TWA0034** (duplicate
  prefix, compilation-end), runs on the contracts compilation.
- **Generator:** `ResourceReferenceRegistryGenerator` emits `GeneratedResourceReferenceTypes`
  into the SPA (gated on the hand-written `ResourceReferenceType` host), one
  `ResourceReferenceType.Create<TQuery, TResponse, TItem>(…)` per marked contract: prefix,
  display name, id type name, the `[EndpointAuthorize]` Policy, query builder (`Search` set;
  `UserId` stamped for `IAuthApiRequest`), items/id/label lambdas. Bad shapes and repeated
  prefixes are skipped (the analyzer reports them).
- **Guid ids:** feedback and role ids are `Guid` on the wire because `FeedbackItemId` lives in
  the domain project, which contracts cannot reference; the registry records `IdTypeName`
  (`Guid`, `PrincipalId`, `CredentialId`). Lifting those typed ids into contracts is a follow-up.

### Initial resource types (prefixes)

| Prefix | Contract | Policy | Id type | Label |
|--------|----------|--------|---------|-------|
| `credential` | `GetCredentials` (caller's own) | `credential.manage.self` | `CredentialId` | new derived `DisplayName` (nickname → provider label → "Passkey {fingerprint}") |
| `feedback` | `ListFeedback` (admin, all owners) | `admin.feedback.read` | `Guid` | `Title` |
| `my-feedback` | `ListMyFeedback` (own filings) | `feedback.file.self` | `Guid` | `Title` |
| `principal` | `ListPrincipals` | `admin.principals.read` | `PrincipalId` | `DisplayName` (id when no profile read) |
| `role` | `GetRoles` | `admin.roles.read` | `Guid` | `Name` |

`Search` was added to `ListFeedback` (store filter, EF lower-LIKE + in-memory), `ListMyFeedback`
(now `IQueryStringRouteProvider`), `GetCredentials` (`CredentialSummary.Matches`), and `GetRoles`
(handler filter). `ListPrincipals` already had it (typing a term there needs
`admin.profiles.read`, as before). Mock factories honour `Search`.

### SPA

- `AskReferenceState` (`features/application/ask-reference-state/`): Open (permission-filtered
  types via `ResourceReferenceRegistry.PermittedAsync` + `IAuthorizationService`), Filter (type
  filter; `prefix:` chooses the type; with a type chosen, debounced 200 ms search guarded by
  `SearchGeneration`), ChooseType, Back, MoveHighlight, Highlight, Close, Choose (records the
  reference for the current `ConversationGeneration`, capped at 20). Handlers call the list query
  through `IWebServerApiService` (TWA0022/TWA0026 clean).
- `AskReferencePicker.razor`: "Reference a resource" combobox; ↑↓, ↵, Esc, Backspace-on-empty →
  types. Results capped at 20 with "More match. Type to narrow the list." (no paging UI).
  `[DirectComponentSideEffect]` re-justified: caret insertion into the library textarea
  (`InsertReference`, which replaces the typed `@`) and returning focus to it (`FocusComposer`).
- `AgentAsk.razor`: typing `@` in the composer opens the picker; the **Tag button is removed**
  (lead slot left for 346); placeholder "Tell the app what you want. Type @ to tag a resource.";
  Close also closes the picker.
- Removed `AskResourceReferences.FromPageContext`, `AskResourceReference`, `AskResourceMenu` and
  the "Nothing on this page to tag" path.
- Token resolution: `page_context` gains `references` (token, type, idType, id, label) from the
  chosen references of the current conversation; the Ask preface tells the model to resolve
  `@prefix:id` through it and that a reference grants nothing. Each later tool call is
  re-authorized by its endpoint.
- Not done: rendering the inserted token as a chip in the textarea (the library `MessageInput`
  owns a plain textarea); tokens stay plain text.

### `?` shortcuts — filed as follow-up

Not included: a `?` list needs the same picker over suggestion cards / catalog actions plus its
own action set; out of scope here. Follow-up: "Ask `?` shortcuts list in the composer" (create
with `ganda kanban create` from the cockpit).

### Tests added

- `tests/analyzers/timewarp-architecture-analyzers-tests/referenceable-resource-analyzer-tests.cs` (9)
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests/resource-reference-registry-generator-tests.cs` (3)
- `tests/container-apps/web/web-spa-integration-tests/features/application/ask-reference-tests.cs`
  (8: registry entries, Member vs Administrator vs signed-out type lists, unoffered type ignored,
  types → search → choose, `prefix:` + Back, credential search, page_context resolves the token
  to `PrincipalId`, New conversation clears it)
- Handler runfiles: `feedback-filing-tests.cs` (`ListMyFeedback` search own titles only),
  `feedback-triage-tests.cs` (`ListFeedback` search across owners + validator)
- `web-server-integration-tests` `roles-endpoint-tests.cs`: `GetRoles` Search over HTTP
- Playwright: `ask-surface` (signed-out SPA: `@` opens the picker with no types, Esc keeps the
  `@` and refocuses the textarea; Tag button gone), `ask-sign-in` (signed-in Administrator:
  `@` → Enter type → Backspace → Enter → Enter item; token replaces `@`; sent message carries the
  token; fake upstream's page_context answer lists it under `references`)

### Gates

- `dev build`: 0 warnings, 0 errors
- Tests (every project under `tests/`, `dotnet test -c Release` per project, as `dev test` runs
  them): all passed — analyzers 216, sourcegenerator 108, web-spa-integration 223,
  web-server-integration 332/333 (1 skipped), playwright 11, web-jaribu 331, plus the rest
- `ganda repo audit`: passes all checks
- `dev check-version`: source 2.0.0-beta.20 already ahead of nuget 2.0.0-beta.19
- `dev template-smoke`: SUCCEEDED (defaults + `--postgres false` + no-api apps build 0/0; generated web-jaribu 331/331)
- aspire-tests 44/44, yarp-integration 16/16, api-server-integration 1/1

### How to validate

**Smoke:**

```bash
dev build
dotnet run --project tests/analyzers/timewarp-architecture-analyzers-tests -- --filter-class ReferenceableResource
dotnet run --project tests/analyzers/timewarp-architecture-sourcegenerator-tests -- --filter-class ResourceReferenceRegistry
dotnet run --project tests/container-apps/web/web-spa-integration-tests -- --filter-class AskReference
dotnet run --project tests/container-apps/web/web-spa-playwright-tests -- --filter-class AskSignIn
```

Manual: `dev run`, sign in, open Ask (top bar), type `@` in the composer.

**Expect:**

- Every test command reports `failed: 0` (9, 3, 8, 2 tests respectively).
- In the browser: "Reference a resource" lists the types you may list (an Administrator sees
  Credentials, Feedback items, My feedback, Principals, Roles; a Member only Credentials and My
  feedback). ↵ opens a type and lists its items site-wide; typing filters them; Backspace on an
  empty box returns to the types; Esc closes and returns to the composer; ↵ on an item replaces
  the `@` with `@prefix:id`. No Tag button. After sending, the reasoning/page_context shows the
  token under `references` with its label and id type.

