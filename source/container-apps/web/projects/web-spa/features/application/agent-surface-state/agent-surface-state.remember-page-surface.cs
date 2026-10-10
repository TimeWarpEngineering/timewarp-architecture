#region Purpose
// Stores a page-body summary so page_context can include headings and text without a browser.
#endregion

#region Design
// SyncWebMcp fills PageSurfaceJson from the browser walk. Tests send this action with fixture
// JSON because the test host's IJSRuntime is not a document. Null clears the summary. The walk's
// caps are applied again when page_context merges the JSON.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class RememberPageSurfaceActionSet
  {
    public sealed class Action : IBaseAction
    {
      public string? Json { get; }

      public Action(string? json)
      {
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
        AgentSurfaceState.PageSurfaceJson = action.Json;
        return default;
      }
    }
  }
}
