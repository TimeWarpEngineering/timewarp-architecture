#region Purpose
// The Ask panel's standing instructions: the whole site map, navigate, and a bounded page_context.
#endregion

#region Design
// The complete-agent-chat contract rejects instructions longer than 8,000 characters; MaxLength
// stays well under it. The preface is fixed. Ask is not limited to the page on screen (task 300):
// the preface tells the model it may answer about and move to any permitted destination, to link
// a page as [Title](url), and that navigate asks first in Ask before editing. The site map
// (AgentSiteMap.Summary, bounded by SiteMapCap) is the menu's destinations for this principal,
// the same rows the site_map tool returns, with Ctrl-K's deep links (the design-system parts,
// task 446) as one "{parent}/<part>" line per page. Callers build the page_context copy with
// PageAgentContext.Describe and ContextCapFor, which is what is left of MaxLength after the
// identity, preface and site map (never below MinContextCap); Describe drops surface text whole
// so the JSON stays valid. For still clips the result under MaxLength as a last guard (page facts
// are not dropped by Describe), so a long page cannot fail the request. The copy is from the last
// SyncWebMcp walk; the page_context tool walks the page again when called.
// The preface explains @prefix:id tokens (task 345): page_context.references resolves them. It
// also explains tagged elements (task 346): a message may end with the elements the person picked
// on the page, and page_context.tags lists the ones sent earlier.
// The first line names the assistant (task 361): AssistantName from SiteSettings, so the model
// introduces itself by the name the person sees on the button and header.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Instructions passed to the in-app Ask model.</summary>
public static class AgentAskInstructions
{
  public const int MaxLength = 7_000;

  public const int ContextCap = 6_000;

  public const int MinContextCap = 1_500;

  public const int SiteMapCap = 2_000;

  public const string SiteMapHeading = "Site map (pages this person may open):\n";

  public const string PageHeading = "Page on screen (page_context): ";

  public const string Preface =
    "You can answer about, and move to, any page in the site map below, not only the page on screen. "
    + "site_map returns the same list. When the answer is on another page, name that page as a markdown "
    + "link to its url, like [Settings](/Settings), and offer to take the person there. "
    + "navigate opens a site map page; in Ask before editing the person approves the move first. "
    + "Never navigate to a url that is not in the site map. "
    + "You can run global actions from any page using the supplied tools. "
    + "If a tool returns executed false and navigateTo, that action lives on another page: "
    + "call navigate with that url and do not claim the action ran. "
    + "On its own page the action runs, and edit mode decides whether it needs approval. "
    + "page_context describes the page on screen, including its title, purpose, headings, and controls; "
    + "call it for the current text. The copy below may be older than the page. "
    + "Human-only commands are not tools. "
    + "A token like @feedback:<id> in a message is a resource the person referenced; "
    + "page_context.references gives each token's label, type, idType and id. "
    + "Pass that id to a tool parameter of the same id type. A reference does not grant access. "
    + "A message may end with elements the person tagged on the page (kind, label, visible text, route, "
    + "and a reference when the element is a resource); page_context.tags lists the ones sent earlier. "
    + "Treat a tagged element as what the person means by this or that. ";

  /// <summary>The line that names the assistant, ahead of the preface.</summary>
  public static string Identity(string assistantName) =>
    $"You are {assistantName}, the assistant built into this app. When you introduce yourself, use that name. ";

  /// <summary>The page_context budget left after the identity, preface and <paramref name="siteMap"/>.</summary>
  public static int ContextCapFor(string assistantName, string siteMap)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(assistantName);
    ArgumentNullException.ThrowIfNull(siteMap);
    int used = Identity(assistantName).Length + Preface.Length + SiteMapHeading.Length + siteMap.Length
      + PageHeading.Length;
    return Math.Clamp(MaxLength - used, MinContextCap, ContextCap);
  }

  public static string For(string assistantName, string pageContext) => For(assistantName, "", pageContext);

  public static string For(string assistantName, string siteMap, string pageContext)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(assistantName);
    ArgumentNullException.ThrowIfNull(siteMap);
    ArgumentNullException.ThrowIfNull(pageContext);
    string map = siteMap.Length == 0 ? "" : SiteMapHeading + siteMap;
    string text = Identity(assistantName) + Preface + map + PageHeading + pageContext;
    return text.Length <= MaxLength ? text : text[..MaxLength];
  }
}
