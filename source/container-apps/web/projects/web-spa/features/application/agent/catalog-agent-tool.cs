#region Purpose
// One catalog entry shaped as a tool an in-app model or a WebMCP caller can be offered.
#endregion

#region Design
// The entry stays on the tool so both drivers execute through ActionCatalogEntry.Execute.
// RequiresApproval is the template's read-only rule, decided once when the tool is selected.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>A page-scoped catalog action offered to an agent.</summary>
public sealed record CatalogAgentTool
(
  string Name,
  string Description,
  string InputSchema,
  bool RequiresApproval,
  ActionCatalogEntry Entry
);
