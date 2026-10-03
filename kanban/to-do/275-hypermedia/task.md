# Hypermedia example: server-supplied actions into TimeWarp.State and Ctrl+K

## Description

Add an example in the TimeWarp Architecture Blazor app where the server, not a hardcoded client table, says which actions are legal right now.

TimeWarp.State (TimeWarpEngineering/timewarp-state) is Flux-shaped. One scoped `IStore` lives in a DI scope: one per Blazor circuit, or one for the whole WebAssembly app. Feature state types derive from `State<TState>`. Actions are nested `ActionSet` types. An async `StateActionHandler` mutates a clone that `StateTransactionBehavior` swaps in before the handler runs, and restores the previous state if the handler throws. Blazor pages inherit `TimeWarpStateComponent` and subscribe with `GetState<T>()`. Dispatch goes through `ISender<ClientPipeline>`. The counter sample uses a generated helper (such as `IncrementCount`) rather than writing the store directly. There is no separate reducer type and no separate effect type. A handler must not send further actions. `ServerPipeline` is a different mediator scope and does not run the store behaviors.

Ctrl+K in this app already searches routes and commands that apply to the current principal. `CommandPaletteRoster` builds that menu from `PageRegistry` and `IActionCatalog` (`[CatalogAction]`). `CommandPaletteRunner` runs a command row through `ActionCatalogEntry.Execute`. That palette is the client knowing its own menu. This task is the hypermedia version: each response hands the menu again.

Build the example on the internal Blazor surface, the shape a future TimeWarp Financial app would copy for its own Blazor UI. Do not build that financial app. The QuickBooks-compatible API surface stays plain JSON and is out of scope. There is no QuickBooks API in this repo. Do not add one.

The screen that displays the data may be specific to the sample. The action layer is generic: parse the payload, put the data in the TimeWarp.State store, expose the server commands as buttons and as Ctrl+K entries, and dispatch the chosen command.

## Requirements

1. A sample endpoint returns a hypermedia payload: data to display, plus a list of commands the server says are valid for that state. The sample returns at least two commands. Each command has a label and an identity the generic layer can dispatch. No client code lists those commands in a route table, an `IActionCatalog` entry, or a `[CatalogAction]` written for this example.
2. A generic Blazor page receives that payload. It puts the data into a feature state on the TimeWarp.State store and renders what `GetState<T>()` subscribed it to. The visible data comes from the store, not from a field that bypasses the store.
3. Each command in the latest payload is both a button on the page and a Ctrl+K entry while that page is current. Invoking either one dispatches that command through `ISender<ClientPipeline>` (a generated helper or the same client pipeline Ctrl+K already uses for catalog commands). Dispatch does not call `ServerPipeline`.
4. The server is the source of truth for which actions are legal. After a command completes, a follow-up payload may offer a different set. Buttons and the Ctrl+K list for this page show only the commands in that latest payload.
5. A command the server did not return cannot be invoked from that page. It has no button, no Ctrl+K row, and no other control on the page that sends it.
6. Parsing the payload, writing the store, listing commands, and dispatching the chosen command stay generic so another screen can reuse them. Layout of the data may stay specific to the sample screen.
7. Leave the existing Ctrl+K roster in place for the rest of the app (`PageRegistry` pages and human catalog actions). Do not replace it with this payload, and do not satisfy this task by only adding static catalog rows.
8. Tests cover the acceptance bar in Notes. Do not change any JSON API that is meant to stay a plain document (no hypermedia controls added to existing JSON endpoints). Do not add a financial ledger, an actor host, or a TimeWarp Financial app.

## Checklist

- [ ] Sample endpoint returns data plus at least two commands
- [ ] Generic page loads that payload into a TimeWarp.State feature state and renders from the store
- [ ] Returned commands appear as buttons and as Ctrl+K entries, and both dispatch on the client pipeline
- [ ] A follow-up payload changes which commands are offered
- [ ] A command absent from the payload cannot be invoked from the page
- [ ] No client-side hardcoded route table, catalog entry, or action table for this example
- [ ] Existing PageRegistry / IActionCatalog Ctrl+K roster still works for other pages
- [ ] Tests for the acceptance bar

## Notes

Ctrl+K today is the client knowing its own menu (task 239, done). Hypermedia is the server handing that menu back on each response. Relevant code:

- `source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs`
- `source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-runner.cs`
- `source/container-apps/web/projects/web-spa/features/application/command-palette-state/`

Out of scope:

- The QuickBooks-compatible API surface. It stays plain JSON. Do not add it to this repo.
- Building the TimeWarp Financial app, its ledger, or an actor runtime. This example is only a pattern a later financial Blazor UI can copy.
- A hypermedia format on every existing endpoint. One sample endpoint and one generic action layer are enough.
- Nested dispatch. TimeWarp.State already forbids a handler from sending further actions. The next menu arrives as the next payload after the dispatched command completes.

Acceptance criteria:

- The sample endpoint returns data plus at least two commands.
- The page shows that data from the TimeWarp.State store.
- Only the returned commands appear as buttons and in Ctrl+K.
- Invoking one dispatches it, and a follow-up payload can change which commands are offered.
- A command the server did not return cannot be invoked from that page.
- The QuickBooks-style JSON API is untouched.

Created: 454564 (2026-10-04). Captured only. No product code.
