# TimeWarp DevTools shell: Blazor shell hosting dev tools as plugins

## Description

Build a TimeWarp DevTools Blazor shell app, owned by Architecture, that hosts dev tools as plugins behind a minimal plugin contract. No new repos: the shell, the contract and plugin 2 all live in Architecture.

- **Plugin contract (minimal):** name, icon, tab content, optional service registration. The contract must not depend on TimeWarp State.
- **Plugin 1, State DevTools:** the State task 103 companion Blazor inspector (registered via `AddTimeWarpStateDevTools` in Aspire; packages `TimeWarp.State.DevTools.Hosting` and `TimeWarp.State.DevTools.Export` at 12.0.0-beta.12). Packaging it as a plugin, and moving shell-related pieces out of State, is State task 118.
- **Plugin 2, Repo explainer:** a dynamic Blazor, C#-only interactive repo explainer in the spirit of [archify](https://github.com/tt-a1i/archify), rewritten in Blazor. Its source is [Roslynk](https://github.com/mrpmorris/Roslynk), Peter Morris's MCP server that exposes a live Roslyn compilation. Starting scope: project reference graph, type hierarchy plus implementations, focusable call graph with source links, search, theme, export.

Origin: Steven's grok.com discussion, https://grok.com/share/bGVnYWN5LWNvcHk_5708bbd6-b09e-4faf-a204-1862df3f8d5c (shell-with-plugins recommended over a tabbed single app), and the 2026-10-13 call.

## Approach / build sequence

1. Upgrade TimeWarp Architecture first. A few things are in flight and need to settle: task 332 (the TimeWarp.Nuru 3.0.0-beta.80 bump), and the State packages are at beta.10 while beta.12 is available.
2. Bring today's State DevTools setup (State task 103's inspector, plus `TimeWarp.State.DevTools.Hosting` and `.Export` at 12.0.0-beta.12) into Architecture's AppHost and get it working as-is.
3. Only after step 2 works, decompose it into the shell-and-plugins design: the shell and a minimal plugin contract in Architecture, State DevTools as plugin 1 (packaged by State task 118), and the Roslynk-backed repo explainer as plugin 2.

## Open points

- The inspector project is currently not packable and is located by a path walk (`DevToolsProjectLocator`), so it only works inside the State repo. The shell must ship as a package/tool to remove that limit.
- The plugin contract must not depend on State.
- Keep task 103's separate-store rule.

## References

- TimeWarpEngineering/timewarp-state task 103 (State DevTools companion app)
- TimeWarpEngineering/timewarp-state task 118 (package the inspector as plugin 1)
- Task 332 (Nuru beta.80 bump)
- https://grok.com/share/bGVnYWN5LWNvcHk_5708bbd6-b09e-4faf-a204-1862df3f8d5c

## Learnings

