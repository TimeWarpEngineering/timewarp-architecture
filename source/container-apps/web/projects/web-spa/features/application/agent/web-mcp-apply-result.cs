#region Purpose
// Tells the caller whether a browser model context accepted a tool list.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Result of replacing the page's WebMCP tools.</summary>
/// <param name="Available">False when the browser has no model context.</param>
/// <param name="Registered">How many tools were registered. Zero when the context is absent.</param>
public readonly record struct WebMcpApplyResult(bool Available, int Registered);
