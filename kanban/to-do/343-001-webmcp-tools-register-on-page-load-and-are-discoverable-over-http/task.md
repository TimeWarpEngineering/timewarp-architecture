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

- [ ] Confirm the inspector tool list and routes (`/`, `/Settings`, `/Feedback`, site_map goto)
- [ ] Reproduce the scanner's `webMcp` fail headlessly; record the cause
- [ ] Register an honest tool on `/` at page load; headless check passes
- [ ] Update `llms.txt` / `auth.md` / discovery SKILL with a WebMCP section (generated or build-checked)
- [ ] `auth.md` describes agent registration (public-key enrollment)
- [ ] Add a `How to validate` smoke (`curl` discovery strings, no JS needed) to the in-repo readiness test
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [ ] After deploy: re-scan; `webMcp` and `authMd` pass (record in 343 `scan/`)

## Session

- Created: 355282 (2026-10-17), folded from 382 (ses_eba2c9566ffdNojmwvFICuxvJm, 2026-10-16)

## Notes

- 382 was abandoned and its claim released in favour of this task. Its original kitchen was
  never published.
