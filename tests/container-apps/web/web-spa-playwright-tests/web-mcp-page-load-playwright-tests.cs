#region Purpose
// Headless proof that navigate is registered during HTML parse when a model context already exists.
#endregion

#region Design
// Mirrors the scanner: a model context is installed before page scripts, then / is loaded.
// The first navigate registration must happen before window.Blazor exists, which is the
// page-load classic script, not the later Blazor publish. The tool has an execute function.
// DOMContentLoaded is the deadline, well inside a six-second scan window.
#endregion

namespace WebMcpPageLoadPlaywright_;

using System.Text.Json;

public class PageLoad_Given_ModelContext
{
  private const string ProbeScript =
"""
(() => {
  const tools = [];
  const api = {
    registerTool(tool, options) {
      tools.push({
        name: tool && tool.name,
        blazor: typeof window.Blazor,
        hasExecute: typeof (tool && tool.execute) === "function",
        signal: !!(options && options.signal),
      });
      return Promise.resolve();
    },
    provideContext(payload) {
      const list = (payload && payload.tools) || [];
      for (const tool of list) {
        tools.push({
          name: tool && tool.name,
          blazor: typeof window.Blazor,
          via: "provideContext",
          hasExecute: typeof (tool && tool.execute) === "function",
        });
      }
    },
  };
  Object.defineProperty(document, "modelContext", { configurable: true, get: () => api });
  Object.defineProperty(navigator, "modelContext", { configurable: true, get: () => api });
  globalThis.__probeTools = tools;
})();
""";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PageLoad_Given_ModelContext>();

  public static async Task Navigate_Should_RegisterBeforeBlazor()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    _ = graph;
    PlaywrightBrowserSession session = await PlaywrightBrowserSession.GetAsync();
    await using IBrowserContext context = await session.Inner.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
    });
    await context.AddInitScriptAsync(ProbeScript);
    IPage page = await context.NewPageAsync();
    await page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 60_000,
    });

    string html = await page.ContentAsync();
    int pageLoad = html.IndexOf("web-mcp-page-load.js", StringComparison.Ordinal);
    int blazor = html.IndexOf("blazor.web.js", StringComparison.Ordinal);
    pageLoad.ShouldBeGreaterThanOrEqualTo(0);
    blazor.ShouldBeGreaterThan(pageLoad);
    html.ShouldContain("timewarp-webmcp-page-load");

    string toolsJson = await page.EvaluateAsync<string>("() => JSON.stringify(globalThis.__probeTools || [])");
    using JsonDocument document = JsonDocument.Parse(toolsJson);
    JsonElement navigate = default;
    bool found = false;
    foreach (JsonElement tool in document.RootElement.EnumerateArray())
    {
      if (tool.GetProperty("name").GetString() == "navigate")
      {
        navigate = tool;
        found = true;
        break;
      }
    }

    found.ShouldBeTrue("navigate was not registered by DOMContentLoaded");
    navigate.GetProperty("blazor").GetString().ShouldBe("undefined");
    navigate.GetProperty("hasExecute").GetBoolean().ShouldBeTrue();
    navigate.GetProperty("signal").GetBoolean().ShouldBeTrue();
  }
}
