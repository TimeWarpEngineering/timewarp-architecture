#region Purpose
// Lets a state add its own facts to page_context while one of its actions' pages is on screen.
#endregion

#region Design
// page_context has no per-route cases (task 300). PageAgentContext asks PageAgentScope which
// states own the actions the current page declares ([Page(AgentActions = ...)]) and calls this on
// each one that implements it. A state adds what the agent needs to call those actions correctly:
// item ids, server flags, and the current values a replace-whole command must echo. The state
// owns its facts, so a new page with agent actions needs no edit in Applications.
// path is the normalized route on screen, for facts that depend on it (one feedback item). Facts
// are page facts: PageAgentContext never drops them to meet its length cap, so keep them bounded.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json.Nodes;

/// <summary>A state that contributes page_context facts for the pages that declare its actions.</summary>
public interface IAgentPageFacts
{
  /// <summary>Adds this state's facts to the page_context <paramref name="document"/>.</summary>
  void AddAgentPageFacts(JsonObject document, string path);
}
