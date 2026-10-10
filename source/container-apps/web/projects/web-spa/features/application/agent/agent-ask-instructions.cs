#region Purpose
// The Ask panel's standing instructions: global tools, navigate, and a bounded page_context.
#endregion

#region Design
// The complete-agent-chat contract rejects instructions longer than 8,000 characters. The preface
// is fixed. Callers build the page_context copy with PageAgentContext.Describe and ContextCap, which
// drops surface text whole so the JSON stays valid. For still clips the result under MaxLength as a
// last guard (page facts are not dropped by Describe), so a long page cannot fail the request. The
// copy is from the last SyncWebMcp walk; the page_context tool walks the page again when called.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Instructions passed to the in-app Ask model.</summary>
public static class AgentAskInstructions
{
  public const int MaxLength = 7_000;

  public const int ContextCap = 6_000;

  public const string Preface =
    "You can run global actions and navigate from any page using the supplied tools. "
    + "navigate opens a page the person may open. It is not an edit, so it does not ask for approval. "
    + "If a tool returns executed false and navigateTo, that action lives on another page: "
    + "call navigate with that url and do not claim the action ran. "
    + "On its own page the action runs, and edit mode decides whether it needs approval. "
    + "page_context describes the page on screen, including its title, purpose, headings, and controls; "
    + "call it for the current text. The copy below may be older than the page. "
    + "Human-only commands are not tools. ";

  public static string For(string pageContext)
  {
    ArgumentNullException.ThrowIfNull(pageContext);
    string text = Preface + pageContext;
    return text.Length <= MaxLength ? text : text[..MaxLength];
  }
}
