# Agentic UI over the action catalog

Decisions below were made under Steve's 2026-10-09 overnight instruction (implement and merge 272 → 271 → 273; WebMCP and the in-app agent share one mapping). They are recorded for morning review. Nothing here releases timewarp-state or adds a template flag.

## 1. Catalog → tools

**Recommendation.** An adapter in this template (`CatalogAgentToolSet` in web-spa). It reads `IActionCatalog` and builds `AIFunction`s. `Visibility` must include `Agent` (`Agent` or `Both`). Human-only is never a tool. Permissions go through `IAuthorizationService` with the same policy names the palette uses (`CommandPaletteRoster.IsPermittedAsync`): every listed permission must succeed, and an anonymous principal gets no tools. The tool parameter schema is built from each `ActionCatalogParameter`: the catalog's `JsonSchema` when the generator emitted one, and one level of public properties when the parameter is a complex CLR type (v1 of the catalog records only the type name). `UserId` is omitted from that reflected object; handlers stamp it.

Tools are page-scoped. `PageAgentScope` lists the catalog actions each page's buttons dispatch (task 282). The offered set is that list ∩ agent visibility ∩ the principal's permissions. `page_context` is an extra read-only tool (not a catalog action) that returns the path and, on Settings and Passkeys, the credential rows with `CanRevoke`, `CanRename`, and `CanLinkMicrosoft365`.

**Alternatives.**

- A generator or adapter inside timewarp-state. Cleaner for every consumer, and it could emit real schemas for complex parameters. Not tonight: that is a timewarp-state change and a release. Follow-up: emit full JSON schemas for complex action parameters, and a read-only flag (item 2).
- Offer the whole catalog on every page. Rejected: task 282 scopes agents to the actions the current page already shows.
- Trust the model to filter permissions. Rejected: the server and the client both filter; endpoints keep `[EndpointAuthorize]`.

## 2. Approval

**Recommendation.** Every tool not on the explicit read-only allow-list in `CatalogAgentApproval` (today only `Credentials.FetchCredentials`) is wrapped in `ApprovalRequiredAIFunction`. A name-prefix rule (`Fetch`/`Get`/`List`/`Search`) was dropped in review: a mutating action using one of those prefixes would skip approval. `FunctionInvokingChatClient` turns that into a `ToolApprovalRequestContent`, and the ask UI renders `FunctionApprovalBlock` (Approve / Reject) before the tool runs. WebMCP uses the same classification: the dispatcher binds the arguments first, shows the bound values on an in-app confirm bar (`AgentSurfaceState`), and only then calls `ActionCatalogEntry.Execute` with exactly those values. One call waits at a time, correlated by id; a second mutating call is refused, navigation cancels the waiting call, and after approval the dispatcher refuses unless the path is unchanged and a fresh selection still offers the tool. The chat driver re-selects the same way when a tool is invoked. Read-only tools run without a prompt. Approval is not a substitute for permissions; a tool that fails the permission check is not registered and is rejected again at WebMCP invoke time.

**Alternatives.**

- A new `[CatalogAction]` flag. Accurate, and the right long-term home. It belongs in timewarp-state. Follow-up, not a release tonight.
- Approve every tool, including reads. Noisier than the overnight rule ("mutating / non-read-only").
- Approve only when a permission is present. Permissions and blast radius are different questions. A permitted revoke still needs a person to confirm it.

## 3. Where the agent runs

**Recommendation.** In-process in the browser. `UIAgent` wraps whatever `IChatClient` the host registered. Tools are frontend tools: `FunctionInvokingChatClient` invokes them, and the function calls `ActionCatalogEntry.Execute` on the browser store. The model never receives a credential and never calls an HTTP endpoint itself. Actions that need the server go through their existing handlers, and those endpoints keep `[EndpointAuthorize]`.

**Alternatives.**

- Server agent behind `AGUIChatClient`. Right when the API key must stay off the client, and it is the documented follow-up. It needs a host endpoint, an AG-UI package, and frontend-tool round trips so decisions still land in the browser store. Not required to prove the catalog mapping, and it would put a model hop on the template by default.
- Let the server execute catalog actions on behalf of the model. Rejected: the store and the page flags live in the browser, and a server executor would be a second authorization path.

## 4. Model and provider

**Recommendation.** Any `IChatClient`. The template registers none. `CatalogAgentAvailability.IsConfigured` is `GetService<IChatClient>() is not null`. With no registration the ask button is absent, the palette behaves as it does today, and the rest of the app is unchanged. A host that wants a model registers an `IChatClient` (OpenAI, Foundry, Ollama, a fake). The key stays in that registration, not in this repo. Tests use a scripted `IChatClient`. Cost and privacy follow the host's client: an in-browser client puts the prompt and the page context on that provider's network; a host that cannot accept that should front the client with its own server (item 3 follow-up).

**Alternatives.**

- Ship an OpenAI or Foundry client with a placeholder key. Rejected: the template would be unusable or unsafe without a secret.
- A local ONNX model. Rejected earlier (task notes): size, WASM, and the dropped npm toolchain.
- A template flag to include the feature. Rejected in item 6.

## 5. UI surface

**Recommendation.** Ask mode on the Ctrl-K palette, not a second product page. When an `IChatClient` is registered, the palette footer shows Ask. It opens one modal (`AgentAsk`) that hosts `ChatPage` / `UIAgent` and a `FunctionApprovalBlock` renderer. Outcomes of catalog actions stay on the shell notification region (handlers already publish them; TWA0025). The component dispatches; tool execution and JS live in handlers and services (TWA0026). WebMCP confirmation is a shell bar, not a message bar with Error/Success intent.

**Alternatives.**

- A dedicated route only. The palette is the place a person already types intent. A route can wrap the same modal later.
- Both a route and the palette. Two shells for one agent, for no extra capability tonight.
- Palette keyword lists. Steve declined that on 2026-10-02.

## 6. Template shape

**Recommendation.** No template flag and no demo slice. The code ships in web-spa, dormant until an `IChatClient` is registered. WebMCP is the same code path and is a no-op when `document.modelContext` and `navigator.modelContext` are both missing. Tests cover the mapping, permission filter, approval, a fake client that dispatches `Counter.IncrementCounter` through the store, the absent chat client, and WebMCP registration.

**Alternatives.**

- An architecture-axis flag. The feature does not change the architecture of generated apps; it adds an optional client. A flag would violate the flag rule to hide a dormant UI.
- A sample slice that owns the agent. The agent is a host-wide surface over every slice's catalog, like the palette. It belongs next to the palette.

## 7. Agents beyond the UI

**Recommendation.** Built tonight, not only noted. `WebMcpRegistration` publishes the same page-scoped, permission-filtered descriptors. The browser module calls `registerTool` on `document.modelContext` (falling back to `navigator.modelContext`), or `provideContext` when only the older method exists. An abort signal replaces the previous page's tools. Missing API returns registered count 0. Invoke goes back into .NET, repeats the permission and page check, and uses the in-app approval bar for non-read-only tools. Execution is `ActionCatalogEntry.Execute`.

**Alternatives.**

- A separate MCP server process. Later, if a host wants agents that are not the page's browser. The mapping function is the piece that would be reused.
- `provideContext` only. The current WebMCP draft removed it because it could wipe another script's tools. We prefer `registerTool` and keep `provideContext` as a fallback for older drafts.

## Follow-ups (not this task)

- timewarp-state: full JSON schema for complex action parameters, and an explicit read-only (or approval) flag so the template can drop its allow-list.
- WebMCP approval against a DOM-actuating agent: the confirm bar is ordinary DOM, so a browser agent that can also click the page can press Approve itself. Gate approval on a trusted user gesture (`isTrusted`) or WebMCP `requestUserInteraction` once browsers ship it.
- Optional AG-UI endpoint so a host can keep the model key on the server while tools still execute in the browser store.
