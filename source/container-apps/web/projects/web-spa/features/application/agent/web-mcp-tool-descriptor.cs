#region Purpose
// The tool shape sent to the browser model context.
#endregion

#region Design
// RequiresApproval matches the in-app function wrapper for the same page, principal, and edit mode.
// The browser module reads name, description, and inputSchemaJson. The extra field is ignored there.
// Invoke recomputes the bit from the current edit mode; this value is the published list.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>One WebMCP tool. Property names serialize camelCase for the JS module.</summary>
public sealed record WebMcpToolDescriptor(
  string Name,
  string Description,
  string InputSchemaJson,
  bool RequiresApproval);
