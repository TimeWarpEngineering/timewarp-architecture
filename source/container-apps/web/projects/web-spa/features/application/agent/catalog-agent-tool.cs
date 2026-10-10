#region Purpose
// One catalog entry, or the navigate tool, shaped as a tool an in-app model or a WebMCP caller can be offered.
#endregion

#region Design
// Entry stays on a catalog tool so both drivers execute through ActionCatalogEntry.Execute.
// Navigate has no catalog entry. OffPageRoute is set when a palette command is page-bound and
// the person is not on that page: the tool is listed, and invoking it returns a navigate offer
// instead of running. RequiresApproval is the template's read-only rule, decided once when the
// tool is selected. An off-page offer is not an edit, so that bit is false.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>A catalog action or the navigate tool offered to an agent.</summary>
public sealed record CatalogAgentTool
(
  string Name,
  string Description,
  string InputSchema,
  bool RequiresApproval,
  ActionCatalogEntry? Entry,
  string? OffPageRoute = null
);
