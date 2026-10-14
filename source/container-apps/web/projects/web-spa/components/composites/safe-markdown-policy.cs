#region Purpose
// Parse rules and URL allow-lists for SafeMarkdown: which links render as anchors, which images
// render inline, and which sources a body shows inline (so a page can skip listing them again).
#endregion

#region Design
// Markdig parses with raw HTML disabled, so an HTML tag in the text stays a literal. SafeMarkdown
// renders the AST itself through the render tree. No HTML string from Markdig reaches the DOM.
// A link is safe when it is http(s) absolute or app-relative (/path, #fragment). Anything with
// whitespace, a control character, or a backslash is refused: browsers strip tabs and newlines
// from URLs, so "java\tscript:" would otherwise run. An image renders only when its URL is in the
// caller's allow-list, compared ordinally. The caller passes the item's own attachment paths.
#endregion

namespace TimeWarp.Architecture.Components;

using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

public static class SafeMarkdownPolicy
{
  private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().DisableHtml().Build();

  public static MarkdownDocument Parse(string? markdown) => Markdown.Parse(markdown ?? "", Pipeline);

  public static bool IsSafeLink(string? destination)
  {
    if (string.IsNullOrWhiteSpace(destination))
    {
      return false;
    }

    foreach (char character in destination)
    {
      if (char.IsWhiteSpace(character) || char.IsControl(character) || character == '\\')
      {
        return false;
      }
    }

    if (destination[0] == '#')
    {
      return true;
    }

    if (destination[0] == '/')
    {
      return !destination.StartsWith("//", StringComparison.Ordinal);
    }

    return (destination.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        destination.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) &&
      Uri.TryCreate(destination, UriKind.Absolute, out Uri? parsed) &&
      (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
  }

  public static bool IsAllowedImage(string? destination, IReadOnlySet<string> allowedImageSources) =>
    destination is not null && allowedImageSources.Contains(destination);

  /// <summary>
  /// Which of <paramref name="sources"/> the markdown shows inline, as an image or a link.
  /// </summary>
  public static IReadOnlySet<string> ReferencedSources(string? markdown, IReadOnlySet<string> sources)
  {
    HashSet<string> referenced = new(StringComparer.Ordinal);
    if (sources.Count == 0)
    {
      return referenced;
    }

    foreach (LinkInline link in Parse(markdown).Descendants<LinkInline>())
    {
      if (link.Url is { } destination && sources.Contains(destination) && IsSafeLink(destination) &&
        !IsInsideImage(link))
      {
        referenced.Add(destination);
      }
    }

    return referenced;
  }

  // SafeMarkdown flattens an image description to alt text, so a link nested in it never renders.
  private static bool IsInsideImage(Inline inline)
  {
    for (ContainerInline? parent = inline.Parent; parent is not null; parent = parent.Parent)
    {
      if (parent is LinkInline { IsImage: true })
      {
        return true;
      }
    }

    return false;
  }
}
