#region Purpose
// Import path and export name for the WebMCP browser module.
#endregion

#region Design
// Marked SideEffectService so a component cannot call the module. The model-context adapter
// imports it when a store handler publishes the page's tools.
#endregion

namespace TimeWarp.Architecture.Services;

/// <summary>Constants for <c>wwwroot/js/features/web-mcp.js</c>.</summary>
[SideEffectService]
internal static class WebMcpJsModule
{
  internal const string Specifier = "./js/features/web-mcp.js";

  internal const string ReplaceExport = "Replace";
}
