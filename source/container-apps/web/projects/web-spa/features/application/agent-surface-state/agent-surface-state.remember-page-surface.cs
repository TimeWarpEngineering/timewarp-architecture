#region Purpose
// Stores a page-body summary so page_context can include headings and text without a browser.
#endregion

#region Design
// SyncWebMcp fills PageSurfaceJson from the browser walk. Tests send this action with fixture
// JSON because the test host's IJSRuntime is not a document. The summary is stored with the
// normalized path it describes; page_context merges it only on that path, so one page's text
// never appears on another. Null clears the summary. The walk's caps, including the per-string
// caps, are applied again when page_context merges the JSON.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class RememberPageSurfaceActionSet
  {
    public sealed class Action : IBaseAction
    {
      public string Path { get; }

      public string? Json { get; }

      public Action(string path, string? json)
      {
        Path = PageAgentScope.Normalize(path);
        Json = json;
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.PageSurfacePath = action.Json is null ? null : action.Path;
        AgentSurfaceState.PageSurfaceJson = action.Json;
        return default;
      }
    }
  }
}
