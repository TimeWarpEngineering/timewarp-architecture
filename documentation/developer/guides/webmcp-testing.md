# Testing WebMCP by hand

Maintainer guide for exercising WebMCP in a browser. Generated apps do not
ship a `documentation/` tree — this file is for operators of the template repo
itself. WebMCP was built in task 271
(`kanban/done/271-net-11-agentic-ui-over-the-timewarpstate-action-catalog/`):
the same permission-filtered, page-scoped catalog tools the in-app AI uses,
registered with the browser.

## WebMCP is not a server endpoint

WebMCP runs client-side, in the browser tab. The page registers its tools with
the browser through `document.modelContext` (or the older
`navigator.modelContext`), feature-detected. There is no URL or MCP endpoint to
point an agent at. A desktop or CLI agent such as Claude cannot reach the tools
directly; an agent or inspector running inside the same browser calls them.

## Steps

1. **Browser.** Use Chrome 146 or newer (Canary for now) or a Chromium build.
   Open `chrome://flags`, enable **"WebMCP for testing"**
   (`#enable-webmcp-testing`), and relaunch. If that flag is not there, enable
   **"Experimental Web Platform features"** instead.
2. **Inspector extension.** Install **"WebMCP - Model Context Tool Inspector"**
   from the Chrome Web Store:
   https://chromewebstore.google.com/detail/gbpdfapgefenggkahomfgkhfehlcenpd
   (source: https://github.com/beaufortfrancois/model-context-tool-inspector).
   It is by Google's François Beaufort.
3. **Run the app.** Start the app as usual from the Aspire AppHost (`dev run`)
   and open the web app over **HTTPS**. WebMCP needs a secure context.
   **Sign in**, because tools are filtered by your permissions.
4. **Inspect and call tools.** Open a page that has tools, such as Counter,
   Profile or Admin Roles. Click the extension icon to open its side panel. It
   lists the tools that page registered, including `page_context`. Run a tool
   by giving it arguments as JSON. The extension can also let Gemini drive the
   tools if you want a real AI in the loop.
5. **Approve and verify.** Any tool that changes data shows the in-app confirm
   bar first. The bar asks `Allow <tool>?` and offers **Approve** and
   **Reject**. Approve it there, then check that the change actually happened
   (counter value, role, and so on). If you navigate to another page, the tool
   list should change to match that page.

## Expected: no tools in normal Chrome

Our code feature-detects WebMCP and does nothing when the browser lacks it. In
normal Chrome without the flag you will see no tools. That is expected, not a
bug.

## Where the code lives

- `source/container-apps/web/projects/web-spa/source/features/web-mcp.ts` —
  browser registration. Uses `document.modelContext`, with
  `navigator.modelContext` as the fallback. When neither API is present the
  call reports the context unavailable and registers nothing.
- `source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-publisher.cs` —
  per-route, permission-filtered tool list plus `page_context`. The publisher
  asks the browser to replace the previous page's tools.
- `source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs` —
  runs a tool call through the same page, permission, approval, and store path
  as the in-app ask UI. `page_context` returns the page facts and does not ask
  for approval.
- `source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-approval-gate.cs` —
  in-app confirmation. Holds one mutating call until the person approves or
  rejects that exact call in the shell.
- `source/container-apps/web/projects/web-spa/components/WebMcpAgentSurface.razor` —
  the confirm bar (`Allow <tool>?`, Approve / Reject). The component only
  dispatches, and it republishes the page's tools when the route changes.
