#region Purpose
// The Ask panel's standing instructions: global tools, navigate, and a bounded page_context.
#endregion

#region Design
// The complete-agent-chat contract rejects instructions longer than 8,000 characters. The preface
// is fixed. The page_context copy is clipped, then the whole string is clipped under that cap,
// so a long surface summary cannot fail the request. The page_context tool still returns the
// full bounded document; the prompt is a copy, not the only source.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Instructions passed to the in-app Ask model.</summary>
public static class AgentAskInstructions
{
  public const int MaxLength = 7_000;

  public const string Preface =
    "You can run global actions and navigate from any page using the supplied tools. "
    + "navigate opens a page the person may open. It is not an edit, so it does not ask for approval. "
    + "If a tool returns executed false and navigateTo, that action lives on another page: "
    + "call navigate with that url and do not claim the action ran. "
    + "On its own page the action runs, and edit mode decides whether it needs approval. "
    + "page_context describes the page on screen, including its title, purpose, headings, and controls. "
    + "Human-only commands are not tools. ";

  public static string For(string pageContext)
  {
    ArgumentNullException.ThrowIfNull(pageContext);
    const int contextCap = 6_000;
    string context = pageContext.Length <= contextCap ? pageContext : pageContext[..contextCap];
    string text = Preface + context;
    return text.Length <= MaxLength ? text : text[..MaxLength];
  }
}
