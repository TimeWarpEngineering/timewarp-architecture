#region Purpose
// One typed @ token the Ask input can insert for a resource on the current page.
#endregion

#region Design
// The label is what the menu shows. The token is the text inserted into the draft, shaped so a
// later tool argument can carry the same id the page context already published.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>A page resource the person can tag in Ask.</summary>
public sealed record AskResourceReference(string Label, string Token);
