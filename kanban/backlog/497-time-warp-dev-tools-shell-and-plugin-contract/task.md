# TimeWarp DevTools shell and plugin contract

## Description

Build the TimeWarp DevTools shell: a Blazor Server app that hosts dev tools as plugins behind a minimal plugin contract. This is step 3 of task 335, which was moved to done after only its AppHost wiring subtasks (335-001, 335-002) shipped. Today "TimeWarp DevTools" is the State inspector with one hard-coded "State" label in its header; there is no shell, contract, or registry in any repo.

Three planned tools wait on this shell:

- the State inspector (timewarp-state, today's only DevTools UI)
- the Taratibu run-graph plugin (task 422)
- a software-factory view of ganda work across the repo set: live kanban board, walk timelines, attention queue (Steven, 2026-11-19 brainstorm)

This task delivers the shell, the contract, and the two ways to launch it. It does not port or build any of those three tools. A small built-in plugin proves the contract.

Origin: Steven, 2026-11-19, "draft the shell spec", after research showed 335 step 3 was never built.

## Requirements

### Contract

- A contract assembly (working name `TimeWarp.DevTools.Abstractions`) that references no `TimeWarp.State*` package and no plugin.
- A plugin supplies:
  - a stable id (kebab-case, used in the route), a display name, and an icon
  - a root Razor component rendered as the plugin's page
  - optional service registration (`IServiceCollection`)
  - optional endpoint mapping (`IEndpointRouteBuilder`), so a plugin can own its ingest. The State inspector needs this for `/ingest/spans` and `/ingest/manifest`.
- Each plugin owns its data source. The shell provides no shared collector, no OTLP receiver, and no shared event store. (State keeps its JSON ingest; ganda task 490 decided the ganda view reads SigNoz.)
- Plugins are composed at build time: the host references plugin assemblies and registers them (working shape `builder.AddTimeWarpDevTools(devtools => devtools.AddPlugin<TPlugin>())`). No runtime assembly discovery or loading.

### Shell

- Blazor Server (InteractiveServer), FluentUI, following tw-blazor, tw-blazor-css-strategy, and tw-blazor-layout (empty layout plus one cascaded shell component).
- Chrome: product name, plugin navigation built from the registry, theme switch. One route per plugin (`/{plugin-id}`); `/` opens the first registered plugin. Deep links inside a plugin stay under its route prefix.
- The shell may use TimeWarp.State for its own UI state. That state is separate from any plugin's store (task 103's separate-store rule) and is not exposed through the contract.
- A plugin whose component throws shows an error panel inside its page. Navigation and the other plugins keep working.
- With zero external plugins the shell still runs and shows the built-in plugin.

### Built-in plugin

- One plugin shipped with the shell, "About": lists registered plugins (id, name, assembly, version) and the shell version. It exercises every part of the contract, including an endpoint (`GET /_devtools/plugins` returning the same list as JSON).

### Launch

- **From an Aspire AppHost:** a hosting extension (working name `builder.AddTimeWarpDevTools()`) that adds the shell as a run-mode-only resource, excluded from the publish manifest, with the dashboard link text "TimeWarp DevTools". Same pattern as `AddTimeWarpStateDevTools` in timewarp-state.
- **Standalone:** the shell starts with no AppHost and no inspected app (`dotnet run` of the host project at minimum). The software-factory view watches the machine, not one app, so it needs this.
- Development only. No authentication. Binds to localhost by default.

### Not in scope

- Porting the State inspector to a plugin. That is a timewarp-state follow-up (its task 118 said to file one once the contract exists).
- The software-factory plugin and the Taratibu run-graph plugin (422).
- Renaming existing `TimeWarp.State.DevTools.*` packages, APIs, or env vars.
- Replacing `AddTimeWarpStateDevTools()` in this repo's AppHost. Both run side by side until the State plugin exists.
- The Roslynk repo explainer from 335.

## Open decisions

Each has a default the implementer follows unless Steven says otherwise before launch.

1. **Home repo.** Default: timewarp-architecture, as 335 said ("No new repos"). Alternative: a new `timewarp-devtools` repo, which is cleaner if the shell and contract ship as packages that timewarp-state and ganda-side plugins reference.
2. **How plugins outside this repo get the contract.** A State plugin in timewarp-state must reference the contract assembly, so it has to be a published package. Default: publish `TimeWarp.DevTools.Abstractions` and the shell as packages from the home repo. This is the strongest argument for the alternative in 1.
3. **Who composes the host.** Default: the shell is a library, and a thin host project in the consuming repo picks its plugins. Alternative: one packaged host with a fixed plugin set.
4. **Standalone launch verb.** Default: `dotnet run` only in this task; a `dev devtools` or `ganda` verb is a follow-up.
5. **Names.** `TimeWarp.DevTools.Abstractions`, `TimeWarp.DevTools.Shell`, `TimeWarp.DevTools.Hosting` are working names (State task 124 left plugin-neutral naming undecided).

## Acceptance criteria

- [ ] The contract assembly builds with no `TimeWarp.State*` reference, checked by a test.
- [ ] The shell runs standalone and shows the About plugin; `GET /_devtools/plugins` returns it.
- [ ] A second test-only plugin registered in a test host appears in navigation, renders at its route, resolves a service it registered, and answers on an endpoint it mapped.
- [ ] A plugin component that throws shows the error panel; the other plugin still renders.
- [ ] The AppHost extension adds the resource in run mode and it is absent from compose, kubernetes, and aca publish output (same facts as `state-dev-tools-model-tests.cs`).
- [ ] bUnit tests for navigation and the error panel; one closed-box check that the shell loads in a browser.
- [ ] `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit` green.

## Checklist

- [ ] Steven settles open decisions 1 to 3 (or accepts the defaults)
- [ ] Contract assembly and registration API
- [ ] Shell app: layout shell, navigation, routing, theme, error boundary per plugin
- [ ] About plugin and `/_devtools/plugins`
- [ ] Aspire hosting extension and publish-exclusion tests
- [ ] Standalone launch
- [ ] Tests and gates
- [ ] Documentation: how to write a plugin (one page)
- [ ] File follow-ups: State inspector as a plugin (timewarp-state), software-factory plugin, and repoint 422's `## Depends on` from 335 to this task

## Session

- Created: a599aa96-c546-4a01-b756-fd6a6134bce7 (2026-11-19)
- Spec drafted: a599aa96-c546-4a01-b756-fd6a6134bce7 (2026-11-19), from Steven's "draft the shell spec". Not launched.

## Notes

- Current DevTools code is all in timewarp-state under `source/timewarp-state-devtools*` (inspector app, `-app` package, `-hosting`, `-export`, `-wasm`). `Inspector.razor` has the hard-coded `<nav class="plugins">` with a single "State" entry.
- This repo wires it in `source/container-apps/aspire/projects/aspire-app-host/web-server-resource.cs` (`AddRunModeTooling`) and `source/container-apps/web/projects/web-server/web-server-state-dev-tools.cs`.
- Ganda already emits the spans a software-factory plugin would read: `ganda.task.transition`, `ganda.task.walk`, `ganda.task.walk.node`, `ganda.agent.run`, `ganda.claim.*` on ActivitySource `TimeWarp.Ganda`, exported over OTLP to SigNoz. The span names are a contract (`telemetry.md` in timewarp-ganda). That plugin also needs current board and claim state, which telemetry does not carry; it would read ganda's JSON output for that.
- The data-source rule in Requirements ("each plugin owns its data source") settles a conflict between existing tasks: the State inspector uses custom JSON ingest, while 422, ganda 490, and taratibu 059 assume OTLP or SigNoz. The shell takes no side.
- Related: 335, 335-001, 335-002, 422, 407 (backlog); timewarp-state 118, 124; timewarp-ganda 488, 490; timewarp-taratibu 059.

## Learnings

<!-- Add dated entries as the work teaches something: what happened, what to do next time, and why. -->
