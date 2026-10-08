#region Purpose
// The tool shape sent to the browser model context.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>One WebMCP tool. Property names serialize camelCase for the JS module.</summary>
public sealed record WebMcpToolDescriptor(string Name, string Description, string InputSchemaJson);
