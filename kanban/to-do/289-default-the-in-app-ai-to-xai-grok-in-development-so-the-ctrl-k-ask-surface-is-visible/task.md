# Default the in-app AI to xAI Grok in development so the Ctrl-K Ask surface is visible

## Description

Task 271 (PR #448, merged 2026-10-09) added an in-process agent over the TimeWarp.State action
catalog. Its Ask button in the Ctrl-K command palette only renders when an `IChatClient` is
registered (`CatalogAgentAvailability.IsConfigured`,
`source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-availability.cs`;
used by `CommandPalette.razor` `AskConfigured`). The template registers none
(`web-spa/program.cs` ~line 147), so on a stock `dev run` no AI is visible anywhere.

Steve, 2026-10-09: "lets default to one at least in dev mode or its pointless". He chose
**xAI Grok**, with the key from **user-secrets**.

This task reverses 271 `design.md` item 4 ("the template registers none ... the ask button is
absent") and takes the item 3 follow-up (keep the model key on the server). See
`kanban/done/271-net-11-agentic-ui-over-the-timewarpstate-action-catalog/design.md`.

## Requirements

1. **Default Grok in Development.** In Development, register an `IChatClient` for xAI Grok by
   default. xAI's API is OpenAI-compatible at `https://api.x.ai/v1`, so use the
   Microsoft.Extensions.AI OpenAI client (`Microsoft.Extensions.AI.OpenAI`, or the current
   recommended M.E.AI path) pointed at that endpoint. The model id is configurable
   (e.g. `XAI:Model`) with a sensible current Grok default; check https://docs.x.ai/docs/models
   at implementation time and record the chosen default and why.
2. **Key stays on the server.** The key comes from user-secrets / configuration
   (e.g. `XAI:ApiKey`, or an Aspire parameter wired from user-secrets). It is never in the repo
   or any appsettings file, and never shipped to the browser. web-spa is Blazor WASM, so the
   model call must run server-side (web-server or api-server) behind an **authorized** endpoint
   that the WASM client calls. Decide where it lives and document the decision (add a section to
   this task's folder or a design note, and update 271's design item 3/4 by reference).
   - The agent loop and tool execution stay in the browser as 271 designed
     (`FunctionInvokingChatClient` + `ActionCatalogEntry.Execute` on the browser store). The
     browser registers an `IChatClient` that relays chat completions to the server endpoint
     (e.g. a thin HTTP relay or the AG-UI path named in 271's follow-ups); the server endpoint
     only forwards to xAI and returns the model response. The server never executes catalog
     actions on the model's behalf.
   - The relay endpoint requires an authenticated principal, and should have basic limits
     (request size, rate or concurrency) so it is not an open proxy to the key.
3. **Visible not-configured state.** If the key is missing, the Ask surface is still visible
   and shows a clear "AI not configured" state with the exact pwsh command to fix it, e.g.:

   ```pwsh
   dotnet user-secrets set "XAI:ApiKey" "<your-xai-key>" --project <path-to-the-project-that-reads-it>
   ```

   (Use the real project path / user-secrets id once decided.) Never silently hide Ask.
   Outside Development, show the same visible not-configured state unless a provider is
   configured. `CatalogAgentAvailability` changes from "is an `IChatClient` registered" to a
   tri-state the UI can render (configured / not configured + fix hint).
4. **dev CLI / docs.** Document how to set the key in pwsh syntax (README / dev-cli docs /
   relevant skill). Optionally add a preflight warning in `dev run` when the key is missing
   (warning only; `dev run` must still start).
5. **Keep 271's guarantees.** Catalog `Visibility` filtering (Agent/Both only), per-principal
   permissions via `IAuthorizationService`, approval gating for every non-allow-listed
   (mutating) action via `ApprovalRequiredAIFunction` / `FunctionApprovalBlock`, and the server
   never trusts the model (endpoints keep `[EndpointAuthorize]`).
6. **WebMCP unchanged.** `WebMcpRegistration` behavior, page scoping and the confirm bar stay
   as they are.

## Acceptance / proof (in the PR description)

- A Playwright browser test against the real WASM render mode: open Ctrl-K and see Ask; with
  the key configured, ask something that runs a read-only catalog action (e.g.
  `Credentials.FetchCredentials` / `page_context`) and see the result; without the key, see the
  not-configured state with the fix command.
- A screenshot of the Ask surface in Ctrl-K attached to the PR.
- Unit/integration tests use a fake `IChatClient` (and a fake upstream for the server relay);
  CI needs no real xAI key.
- Build, tests, template-smoke and the ganda repo audit all green.

## Checklist

- [x] Decide where the server relay lives (web-server vs api-server) and record it
- [x] Add `Microsoft.Extensions.AI.OpenAI` (central package version) and the xAI options (`XAI:ApiKey`, `XAI:Model`, endpoint)
- [x] Server: authorized relay endpoint to `https://api.x.ai/v1`, key from user-secrets/config, limits
- [x] Development default registration; no key in repo/appsettings; nothing key-related reaches WASM
- [x] web-spa: relay `IChatClient` registered so Ask renders; tri-state availability
- [x] Ask surface shows "AI not configured" + pwsh `dotnet user-secrets set` command when key missing (all environments)
- [x] Docs (pwsh) for setting the key; optional `dev run` preflight warning
- [x] Verify 271 guarantees (visibility, permissions, approval, server distrust) still hold; WebMCP unchanged
- [x] Tests with fake `IChatClient` / fake upstream
- [ ] Playwright WASM test (configured + not-configured) and Ctrl-K Ask screenshot in PR
- [x] Recheck the WASM `SemaphoreSlim.Wait` observation (Notes) in the browser test
- [ ] Build, tests, template-smoke, ganda repo audit green

## Session

- Created: 620441 (2026-10-09)
- Implement: grok `01a11eaf-294b-79b0-a840-090b955324b6` (2026-10-09)

## Results

The relay lives on web-server. Development registers an OpenAI-compatible client for `https://api.x.ai/v1` with default model `grok-4.7` when `XAI:ApiKey` is set in user-secrets. The key is not in the repo or the browser. The SPA always registers `RelayChatClient`, so Ask stays visible. A missing key shows "AI not configured" and:

```pwsh
dotnet user-secrets set "XAI:ApiKey" "<your-xai-key>" --project source/container-apps/web/projects/web-server/web-server.csproj
```

`dev run` warns and still starts when the key is missing. Catalog visibility, permissions, approval, and WebMCP are unchanged. The server forwards declarations only.

The WASM proof did not pass. InteractiveWebAssembly store dispatches throw `PlatformNotSupportedException` from `SemaphoreSlim.Wait` in TimeWarp.State 12.0.0-beta.8 (`StateTransactionBehavior` → AnyClone). Ctrl-K calls `ApplicationState.SetActiveModal`, so the palette never opens and no Ask screenshot was captured. `UseStateTransactionBehavior` was not disabled. Filed https://github.com/TimeWarpEngineering/timewarp-state/issues/616.

### How to validate

Smoke: from `tests/container-apps/web/web-spa-playwright-tests`, `dotnet test -c Release`.

Expect: after timewarp-state 616 is fixed, Ctrl-K shows Ask. With no key the panel text is `AI not configured` and the setup command above. With the Development fake upstream, asking "What is on this page?" returns text that contains `page_context:` and `path`. The browser console does not contain `PlatformNotSupportedException` or `SemaphoreSlim.Wait`.

Smoke that passed on this walk: `web-server-integration-tests` filter `XaiChatUpstream` (3), `web-jaribu-tests` filter `CompleteAgentChat` (6), `web-spa-integration-tests` filter `CatalogAgent` (17). Fake clients only; no xAI key.

## Notes

- Source: Steve, 2026-10-09 — after 271 merged he could not find any AI in Ctrl-K or elsewhere;
  "lets default to one at least in dev mode or its pointless"; chose xAI Grok, key from
  user-secrets.
- The template must stay usable with no key: a missing key is a visible not-configured state,
  never a startup failure.
- **Confirmed 2026-10-09:** the Playwright WASM test (InteractiveWebAssembly, prerender off)
  logs `PlatformNotSupportedException` from `SemaphoreSlim.Wait` in TimeWarp.State 12.0.0-beta.8
  AnyClone, called by `StateTransactionBehavior` on `SyncWebMcp`, `ClearProfileData`, and
  `LoadChatConfiguration`. Ctrl-K does not open the palette. Filed
  https://github.com/TimeWarpEngineering/timewarp-state/issues/616.
  `UseStateTransactionBehavior` stays on.
- Related: 271 (agentic UI, PR #448), 272 (.NET 11, PR #447).
