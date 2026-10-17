# WebMCP tools register on page load and are discoverable over HTTP

## Description

First child of 343 (isitagentready.com score). This task replaces 382 ("WebMCP tools visible in
inspector but undiscoverable over HTTP"), which was never published. Its content is kept below
and the scope is wider.

Two audiences fail to see our WebMCP tools today, for different reasons:

1. **The isitagentready.com scanner** loads `/` in a real browser for about 6 s and checks
   `navigator.modelContext` / `document.modelContext` for registered tools. The 2026-10-17
   baseline (`../343-max-out-the-isitagentreadycom-score-for-archtimewarpwork/scan/baseline.json`)
   reports `webMcp: fail`, "No tools registered via document.modelContext or
   navigator.modelContext". The cause is not verified yet. Candidates: tools register only after
   Blazor WASM boot (longer than the scan window), registration is skipped when the browser has
   no native `modelContext` (the inspector extension supplies one), or `/` registers no tools at
   all.
2. **HTTP-only agents** (curl, webfetch, OpenCode without a WebMCP browser) read the discovery
   docs. Those docs describe only the backend MCP stub, so the agent concludes that no tools
   exist.

The same baseline also reports `authMd: fail`, "auth.md exists but does not describe agent
registration". This task already edits `auth.md`, so the fix rides here.

## Problem (from 382, 2026-10-16)

Evidence from `https://arch.timewarp.work/`:

- `GET /.well-known/mcp/server-card.json` returns `status: "stub"`,
  `capabilities.tools: false`, `resources: false`, `prompts: false`, `tools: []`,
  with text `do not call /mcp expecting tools/list`.
- `POST /mcp {"method":"tools/list"}` returns `405 Allow: GET, HEAD`.
- `/llms.txt`, `/auth.md`, `timewarp-architecture-discovery/SKILL.md` and
  `timewarp-identity/SKILL.md` describe the MCP card as a stub and list no WebMCP surface.
- The browser inspector on the same host shows WebMCP tools, for example
  `Credentials.AddPasskey` (runs only on `/Settings`), the feedback list (runs only on
  `/Feedback`), and page goto (site_map destinations; pass a url, and the person approves first).
  Each non-home route returns a navigate offer.

Nothing is broken at runtime. The machine-readable story for the frontend WebMCP layer is wrong
or missing (confusion between backend MCP and WebMCP).

## Requirements

- **Decide and record** that backend MCP (`/mcp`, streamable HTTP) and frontend WebMCP
  (`navigator.modelContext.registerTool`) are separate transports. Discovery must describe both.
- **Page-load registration:** find out why the scanner sees no tools on `/`, with evidence from
  a headless browser run that mirrors the scanner. Then make at least one honest tool register
  on `/` early enough for page-load detection; the site-wide page goto is the natural candidate.
  Do not register a fake or no-op tool to score.
- **HTTP discoverability without JS:** add a WebMCP section to `/llms.txt`, `/auth.md` and the
  discovery skill. It covers route scoping (`/` vs `/Feedback` vs `/Settings`) and inspector
  steps (`typeof navigator.modelContext`, the Chrome Canary flag or MCP-B polyfill, Copy trace).
- **Keep the card honest:** do not claim `/mcp` tools until they exist. Put WebMCP in its own
  section beside the card, not inside `capabilities.tools`.
- **auth.md agent registration:** describe how an agent registers, which is by enrolling a
  public key (agent key / passkey); there is no email form. Use wording that meets the
  scanner's Auth.md registration markers (check the scanner's recommendation text) without
  claiming any flow that does not exist.
- **How to validate:** record which URLs an HTTP agent fetches, which strings prove WebMCP is
  advertised, and that route-scoped tools need the matching SPA route.
- **Generated, not hand-maintained:** where these docs list tools or routes, derive them from
  the tool registry / `[Page]` registry, or add a build check (343 requirement 3).
- Everything lands in the template and ships to every generated app. Values that vary per
  deployment come from configuration.

## Checklist

- [x] Confirm the inspector tool list and routes (`/`, `/Settings`, `/Feedback`, site_map goto)
- [x] Reproduce the scanner's `webMcp` fail headlessly; record the cause
- [x] Register an honest tool on `/` at page load; headless check passes
- [x] Update `llms.txt` / `auth.md` / discovery SKILL with a WebMCP section (generated or build-checked)
- [x] `auth.md` describes agent registration (public-key enrollment)
- [x] Add a `How to validate` smoke (`curl` discovery strings, no JS needed) to the in-repo readiness test
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [ ] After deploy: re-scan; `webMcp` and `authMd` pass (record in 343 `scan/`)

## Goal (implementer restatement)

The browser scanner and an HTTP-only agent both miss the tools this app already has. The scanner opens `/` for about six seconds and looks for tools on `document.modelContext` or `navigator.modelContext`. HTTP clients read `/llms.txt`, `/auth.md`, and the discovery skill, which only describe the backend MCP card. That card is a stub and must stay a stub.

This fix pass keeps the discovery document honest and tied to the prose:

- `/.well-known/webmcp.json` `routeScopedTools` lists only catalog actions whose visibility includes `Agent`, under the `IActionCatalog` name. `/Settings` includes `Credentials.AddPasskey` and omits `Credentials.AddExistingPasskey` and `Credentials.LinkMicrosoft365`.
- A test fails when a WebMCP section in `/llms.txt`, `/auth.md`, or the discovery skill names a tool or Pascal route that the generated document does not publish.

Done means:

- A headless run that supplies a model context before page scripts shows why today's `/` reports no tools, and the cause is written down.
- `/` registers the real `navigate` tool during HTML parse, before Blazor starts, when a model context is already there. Calling it goes to a real menu URL and asks the person first. It is the same destination list as the in-app tool. No placeholder tool.
- After the interactive circuit starts, that early registration is replaced by the full page tool list, including `navigate`.
- `/llms.txt`, `/auth.md`, and `timewarp-architecture-discovery` explain both transports: backend MCP (`/mcp`, card `capabilities.tools` false) and frontend WebMCP (route scoped: `/` has the site-wide goto; `/Feedback` and `/Settings` add their own actions). Inspector steps name `typeof navigator.modelContext`, the Chrome Canary flag or the MCP-B polyfill, and Copy trace.
- Where those docs name tools or routes, the list comes from the `[Page]` registry (a generated JSON document), and a test fails if the document drifts from that registry.
- `/auth.md` tells an agent how to register: enroll a public key (agent key or passkey). There is no email form. The wording matches the scanner's Auth.md registration markers (agent audience, registration endpoints, supported methods, credential use) and does not invent an OAuth `agent_auth` flow.
- An in-repo HTTP test fetches the discovery URLs and checks those strings without running JavaScript.
- The live re-scan of arch.timewarp.work waits until this ships. It is not part of this walk.

## Session

- Created: 355282 (2026-10-17), folded from 382 (ses_eba2c9566ffdNojmwvFICuxvJm, 2026-10-16)
- Implementer: grok session 01a1497f-378b-7860-9c50-5695793d5779 (2026-10-17)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-17T11:40:03Z
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-17T11:42:28Z
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-17T12:02:34Z
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-17T12:06:51Z
- Review round 1: reviewer-claude (claude-opus-5-5), effort 2, roster general, session not reported. Result: fix pass needed for M1 and M2 (2026-10-17)
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-17T12:52:21Z
- Implement-fix-2 oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-17
- Review round 2: reviewer-claude (claude-opus-5-5), effort 2, roster general, session not reported. Result: M1 and M2 fixed, nothing new, disposition clean (2026-10-17)

## Review

- Round 1: M1 and M2 implemented on this id.
- Round 2: Done
## Notes

- 382 was abandoned and its claim released in favour of this task. Its original kitchen was
  never published.
- This branch also contains the seven task-371 commits through `ffc54646f`. Local `master` is
  `f97007a61` (four kanban-383 commits after merge-base `6f19ce597`). Those were not merged here.

## Results

Backend MCP (`/mcp`, streamable HTTP, card `capabilities.tools` false) and frontend WebMCP
(`document.modelContext.registerTool`, fallback `navigator.modelContext`) are separate transports.
The card stays a stub. WebMCP is its own document at `/.well-known/webmcp.json`, generated from
`PageRegistry.ActionScopes` and `AgentSiteMap`. Route rows keep only catalog entries whose
visibility includes `Agent`, under the `IActionCatalog` name. A test fails if a catalog name,
route row, or a tool or Pascal route named in a WebMCP markdown section drifts.

### Cause

Headless probe of `https://arch.timewarp.work/` on 2026-10-17, with a recording `modelContext`
installed on `document` and `navigator` before page scripts, then a 6 s wait:

- HTTP 200. `DOMContentLoaded` at 218 ms. At 6 s, `window.Blazor` was an object and `readyState` was `complete`.
- `navigate`, `site_map`, and `page_context` registered together at `performance.now()` 399 ms. Each had an execute function and an abort signal.
- `hasPageLoadElement` was false. Nothing registered during HTML parse.

`/` already has those three honest tools, but only after the interactive render (`WebMcpAgentSurface` → `web-mcp.ts` `Replace`), which is after `DOMContentLoaded`. A check at parse time sees an empty context. When the browser has no `modelContext`, `Replace` feature-detects and does nothing; the inspector extension supplies the API and a person waits. That matches the baseline `webMcp` fail (`check.webMcp.noToolsDetected`). The deployed page had no page-load registration. This change does not ship an MCP-B polyfill. A browser that never exposes `modelContext` still sees no tools; the docs name the Canary flag and the polyfill for that case.

### What landed

- `WebMcpPageLoad` is the first thing in `<body>`. It embeds the real `navigate` payload (`AgentNavigate.Create`, Ask before editing, so the person confirms) and a parser-blocking classic script. `execute` refuses a url outside the enum, calls `window.confirm`, and `location.assign` only for a same-origin URL.
- When Blazor publishes, `web-mcp.ts` aborts `window.__timewarpWebMcpPageLoad` and registers the full list, including `navigate`, with the in-app approval bar.
- `GET /.well-known/webmcp.json` is anonymous JSON. `routeScopedTools` comes from the page registry, filtered to `IActionCatalog` entries whose visibility includes `Agent`. Human-only `/Settings` actions (`Credentials.AddExistingPasskey`, `Credentials.LinkMicrosoft365`) are omitted. `Credentials.AddPasskey` stays. `/Feedback` and `/Settings` are examples of routes whose extra tools run only on that SPA route.
- `/llms.txt`, `/auth.md`, and `timewarp-architecture-discovery/SKILL.md` describe both transports, the route split, and inspector steps. `/auth.md` says an agent registers by enrolling a public key (agent key or browser passkey). There is no email form. It does not claim OAuth `agent_auth` or `register_uri`.
- The server card description points at the WebMCP document. `capabilities.tools` stays false, `tools` stays `[]`, `status` stays `stub`.

### Gates

Re-run on the fix pass (2026-10-17):

- `./bin/dev build`: 0 warnings, 0 errors.
- `./bin/dev test --quiet`: 23 of 23 projects passed (includes `web-spa-playwright-tests` and `web-spa-integration-tests`).
- `dotnet run source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-tests.cs`: 6/6 passed, including `WebMcpDiscovery_Should_BeReadableWithoutJavaScript` (served JSON contains `Credentials.AddPasskey` and omits the two human-only names).
- `Destinations_Should_` (project directory, `--filter-class`): 7/7 passed. Catalog names match `IActionCatalog`. The new case checks WebMCP markdown citations against the document.
- `PageLoad_Given_ModelContext`: passed on the earlier implement walk. The first `navigate` is recorded with `window.Blazor` still undefined, `hasExecute` true, and an abort signal. `web-mcp-page-load.js` precedes `blazor.web.js`. This fix pass did not re-run that Playwright filter; the full `dev test` suite, which includes `web-spa-playwright-tests`, passed.
- `./bin/dev template-smoke`: succeeded (SmokeDefault build 0/0; generated web Jaribu 373/373, api 9/9, testing 14/14).
- `ganda repo audit`: 32 passed, 0 failed.

### How to validate

#### Smoke

No JavaScript. Replace `<host>` with the deployed origin (local proof uses the in-proc host from the readiness test).

```bash
curl -fsS "https://<host>/llms.txt"
curl -fsS "https://<host>/auth.md"
curl -fsS "https://<host>/.well-known/agent-skills/timewarp-architecture-discovery/SKILL.md"
curl -fsS -D - "https://<host>/.well-known/webmcp.json" -o /tmp/webmcp.json
curl -fsS "https://<host>/.well-known/mcp/server-card.json"
curl -fsS "https://<host>/"
curl -fsS "https://<host>/js/features/web-mcp-page-load.js"
```

The same strings are asserted by `WebMcpDiscovery_Should_BeReadableWithoutJavaScript` in `source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-tests.cs`.

#### Expect

- `/llms.txt`, `/auth.md`, and the discovery skill contain `WebMCP`, `document.modelContext`, `typeof navigator.modelContext`, `MCP-B`, and `Copy trace`.
- `/llms.txt` and the discovery skill also contain `/Feedback` and `/Settings`.
- `/auth.md` contains `You are an agent`, `Agent registration`, `Supported methods`, `Credential use`, `/api/identity/agent/register`, `public key`, and `no email form`.
- `/.well-known/webmcp.json` is `application/json` and contains `"transport": "webmcp"`, `"capabilitiesTools": false`, `navigate`, `document.modelContext`, `/Feedback`, `/Settings`, and `"Credentials.AddPasskey"`.
- That document does not contain `Credentials.AddExistingPasskey` or `Credentials.LinkMicrosoft365`.
- `/.well-known/mcp/server-card.json` still contains `"tools": false`, `"tools": []`, and `"status": "stub"`.
- The HTML for `/` contains `id="timewarp-webmcp-page-load"`, and `web-mcp-page-load.js` appears before `blazor.web.js`. The script body contains `registerTool`.
- Route-scoped tools (credential actions such as `Credentials.AddPasskey`, feedback actions) are listed under their route template in `webmcp.json`. They run only when the SPA is on that route. `/` registers `navigate` (site-wide goto; the person approves). `site_map` and `page_context` join after the page becomes interactive.
- A tool id or Pascal route named in a `## WebMCP` section of `/llms.txt`, `/auth.md`, or the discovery skill is also a name in `webmcp.json`. `WebMcp_Sections_Should_Cite_Only_Published_Tools_And_Routes` fails when one is not.

### Not in this walk

The live re-scan of arch.timewarp.work waits until this is deployed. Parent 343 records the result under `scan/`. The checklist item stays open.

### Review

- Rounds: 2 (effort 2, roster: general, reviewer-claude claude-opus-5-5).
- Final counts: bug 1 fixed, suggestion 1 fixed, nit 0. Open 0, wontfix 0.
- Disposition: **clean**. Round 2 re-verified M1 and M2 on `94133f633` and found nothing new. The reviewer re-ran the gates: `./bin/dev build` 0/0, the HTTP smoke runfile 6/6, and `web-spa-integration-tests` 301/301.
- M1: `routeScopedTools` keeps catalog entries whose visibility includes `Agent`. `/Settings` publishes `Credentials.AddPasskey` and omits `Credentials.AddExistingPasskey` and `Credentials.LinkMicrosoft365`.
- M2: `WebMcp_Sections_Should_Cite_Only_Published_Tools_And_Routes` reads the `## WebMCP` sections of `llms.txt`, `auth.md`, and the discovery skill and requires each cited catalog tool, short tool id, and Pascal route to appear in the generated document.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md` (final ledger), `review/disposition.md`.
