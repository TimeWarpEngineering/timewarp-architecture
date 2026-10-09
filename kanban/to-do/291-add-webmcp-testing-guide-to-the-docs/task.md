# Add WebMCP testing guide to the docs

## Description

Task 271 shipped WebMCP (the same permission-filtered, page-scoped catalog tools the in-app AI
uses, registered with the browser). Steven asked how to test it ("do you just point Claude at the
site?"). The answer is no, and the steps are not written down anywhere. Add a maintainer guide
that explains how to test WebMCP by hand.

## Requirements

- New file: `documentation/developer/guides/webmcp-testing.md`. `documentation/developer/guides/`
  is the repo's existing docs folder (today it holds only `releasing.md`). `documentation/` is
  maintainer-only and is not packed into the template (see AGENTS.md "Documentation").
- Link the new guide wherever docs are listed. Today that is the AGENTS.md "Documentation"
  section, which names `documentation/developer/guides/releasing.md`. Add the WebMCP guide next
  to it.
- Follow the repo's writing conventions (AGENTS.md, skills). No calendar estimates.
- The guide must contain every fact in "Required content" below. Light tightening of wording is
  fine, but no fact may be dropped.
- Name source paths in the guide only after checking that they exist on master. Candidates seen
  on 2026-10-09:
  - `source/container-apps/web/projects/web-spa/source/features/web-mcp.ts` (browser registration,
    `document.modelContext` with `navigator.modelContext` fallback)
  - `source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-publisher.cs`
    (per-route, permission-filtered tool list plus `page_context`)
  - `source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs`
  - `source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-approval-gate.cs`
    (in-app confirmation)
  - `source/container-apps/web/projects/web-spa/components/WebMcpAgentSurface.razor`
- Reference task 271 (`kanban/done/271-net-11-agentic-ui-over-the-timewarpstate-action-catalog/`)
  as the place where this was built.
- Docs-only change. No product code.

## Required content

### WebMCP is not a server endpoint

WebMCP runs client-side, in the browser tab. The page registers its tools with the browser through
`document.modelContext` (or the older `navigator.modelContext`), feature-detected. There is no URL
or MCP endpoint to point an agent at. A desktop or CLI agent such as Claude cannot reach the tools
directly; an agent or inspector running inside the same browser calls them.

### Steps

1. **Browser.** Use Chrome 146 or newer (Canary for now) or a Chromium build. Open
   `chrome://flags`, enable **"WebMCP for testing"** (`#enable-webmcp-testing`), and relaunch.
   If that flag is not there, enable **"Experimental Web Platform features"** instead.
2. **Inspector extension.** Install **"WebMCP - Model Context Tool Inspector"** from the Chrome Web
   Store: https://chromewebstore.google.com/detail/gbpdfapgefenggkahomfgkhfehlcenpd (source:
   https://github.com/beaufortfrancois/model-context-tool-inspector). It is by Google's
   François Beaufort.
3. **Run the app.** Start the app as usual from the Aspire AppHost (`dev run`) and open the web app
   over **HTTPS**. WebMCP needs a secure context. **Sign in**, because tools are filtered by your
   permissions.
4. **Inspect and call tools.** Open a page that has tools, such as Counter, Profile or Admin Roles.
   Click the extension icon to open its side panel. It lists the tools that page registered,
   including `page_context`. Run a tool by giving it arguments as JSON. The extension can also let
   Gemini drive the tools if you want a real AI in the loop.
5. **Approve and verify.** Any tool that changes data shows the in-app confirm bar first. Approve it
   there, then check that the change actually happened (counter value, role, and so on). If you
   navigate to another page, the tool list should change to match that page.

### Expected: no tools in normal Chrome

Our code feature-detects WebMCP and does nothing when the browser lacks it. In normal Chrome
without the flag you will see no tools. That is expected, not a bug.

## Checklist

- [ ] Write `documentation/developer/guides/webmcp-testing.md` with all of the required content
- [ ] Verify every source path the guide names exists on master (drop any that do not)
- [ ] Link the guide from the AGENTS.md "Documentation" section (and any other docs index)
- [ ] `ganda repo audit` clean
- [ ] One PR
- [ ] Merge via `ganda pr merge`

## Session

- Created: 1981641 (2026-10-09)

## Notes

- Requested by Steven on 2026-10-09 during a voice call. All work in this repo goes through ganda.
- Related: 271 (done) built WebMCP and the in-app Ask AI.
