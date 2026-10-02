# .NET 11 agentic UI over the TimeWarp.State action catalog

## Description

Give the template an **agentic UI**. A user types intent in natural language ("log me out",
"create a role called Editors", "link my Microsoft 365"), and an AI agent picks the right
TimeWarp.State action from the action catalog, fills its parameters, and runs it through the
store, with approval where the action is consequential. This replaces fuzzy matching or synonym
lists as the answer to "the palette should understand what I mean". Steve decided on 2026-10-02:
no login/logout keyword patch; go straight here.

Foundation: Microsoft's **`Microsoft.AspNetCore.Components.AI`**, experimental, on **.NET 11 RC1**
(https://devblogs.microsoft.com/dotnet/build-agentic-ui-blazor/). It provides:

- `ChatPage`, `UIAgent<TState>` (wraps any Microsoft.Extensions.AI `IChatClient`), `MessageList`,
  `MessageInput`, `ContentBlock` / `BlockRenderer<TBlock>`, and `AgentBoundary`.
- Tools via `AIFunctionFactory.Create()`, with `ApprovalRequiredAIFunction` for human-in-the-loop
  and `RegisterUIAction` for decisions only the user can make.
- Remote agents via `AGUIChatClient` (HTTP/SSE) with `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`,
  or in-process agents.

What we already have that maps onto it. TimeWarp.State 12.0.0-beta.7's catalog
(`IActionCatalog` / `ActionCatalogEntry`) gives every `[CatalogAction]`:

- `Name`, `DisplayName` and `Description`;
- typed `Parameters` plus a JSON schema;
- `Permissions` and `Visibility` (Human / Agent / Both);
- a reflection-free `Execute(IStore, args, ct)`.

That is almost exactly an `AIFunction`'s shape. Agent-only actions (CreateRole, RenameCredential,
RevokeCredential, UpdateProfile, UpdateSiteSettings) have had no caller until now.

## Depends on

- 267

## Phase 1: design (stop for Steve before building)

Write `kanban/<this task>/design.md` covering each item below, with a recommendation and the
alternatives. Then **stop and hand back to Steve**. Steve decides architecture, so do not
implement past a throwaway spike until he approves.

1. **Catalog → tools.** Generate `AIFunction`s from catalog entries. Should this be a
   TimeWarp.State feature (a generator or adapter in timewarp-state, which would mean a
   timewarp-state task and release), or an adapter in this template? Map `Visibility` (Human-only
   is never a tool?) and `Permissions`: filter tools per principal through `IAuthorizationService`,
   the same as the palette does. Map the JSON schema into the tool parameter schema.
2. **Approval.** Which actions wrap in `ApprovalRequiredAIFunction`? For example, everything that
   mutates, everything not read-only, or a new catalog flag. How does that relate to permissions?
3. **Where the agent runs.** Server-side behind AG-UI (`AGUIChatClient` from the WASM UI to a
   web-server endpoint), or in-process. The tools execute in the **browser store**, so how do
   server-side agent decisions dispatch client-side actions? Frontend tools versus a round-trip
   protocol. Security: the server must never trust a model to bypass server authorization, and
   every endpoint keeps its `[EndpointAuthorize]`.
4. **Model and provider.** It works with any `IChatClient`. Template default: none, OpenAI,
   Azure/Foundry, or local (Ollama, ONNX)? Configuration and secrets, cost and privacy notes, and
   behavior when no model is configured. The feature must be off and the app fully usable without
   a model, which matters because the template ships to every generated app.
5. **UI surface.** An "ask" mode in the Ctrl-K palette (task 239), a dedicated `ChatPage`, or
   both. Keep TWA0025 (outcomes go through NotificationState) and TWA0026 (components only
   dispatch). How do agent-run actions surface their outcomes?
6. **Template shape.** Is it opt-in by configuration? Should it be a template flag? Flags are
   architecture-axis only, so justify one or avoid it. Is it a demo slice? Which tests are
   possible without a live model (a fake `IChatClient` that emits tool calls)?
7. **Agents beyond the UI.** Can the same catalog-to-tools mapping serve external agents (MCP,
   WebMCP, task 238 lineage) later? Note it; don't build it.

## Phase 2: implement (after Steve approves design.md)

Implement the approved design on .NET 11, with tests:

- catalog-to-tool mapping;
- permission filtering;
- approval gating;
- a fake-`IChatClient` end-to-end that dispatches a real action through the store;
- the no-model-configured path.

Add a skill section for the pattern. If Phase 1 decides a timewarp-state change is needed, file
it there and release it first.

## Checklist

- [ ] Depends on task 267 (.NET 11 + `AddDotnetProject`); do not start before 267 merges
- [ ] Phase 1 `design.md` (items 1–7, recommendation plus alternatives each); hand back to Steve
- [ ] Steve's decisions recorded in this task
- [ ] Phase 2 implementation per the approved design, with tests (mapping, permissions, approval,
      fake-client e2e, no-model path)
- [ ] Skill updated; Purpose/Design regions on new files
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`,
      `dev check-version` if packages ship
- [ ] Do **not** start the maintainer's AppHost; use no real model credentials in tests
- [ ] Implementation review; host `open-pr`

## Session

- Created: 494460 (2026-10-02)

## Notes

- Superseded: a palette keywords/synonyms task (login/logout). Steve declined it on 2026-10-02
  in favor of this.
- Considered and not chosen: SmartComponents `LocalEmbedder`. It is experimental, has had no
  NuGet release since 2024-04, runs server-side only (ONNX Runtime does not run in WASM), and
  ships a 23 MB model. Browser semantic search via JS `onnxruntime-web` / `transformers.js` is
  also out, because it conflicts with dropping the npm toolchain.
- Related: task 239 (Ctrl-K palette), task 260/265 (interactions are actions, TWA0026),
  timewarp-state 092/094 (action catalog, DisplayName).

## Results

*(fill when done)*

### How to validate

*(required before done)*
