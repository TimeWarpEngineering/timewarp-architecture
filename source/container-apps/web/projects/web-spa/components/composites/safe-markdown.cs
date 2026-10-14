#region Purpose
// Renders user-written markdown (feedback bodies) safely: CommonMark structure, no raw HTML,
// links only when SafeMarkdownPolicy allows them, images only from the caller's allow-list.
#endregion

#region Design
// The component walks the Markdig AST into the render tree. Text always goes through
// AddContent and URLs through AddAttribute, so Blazor escapes both. There is no MarkupString.
// Raw HTML is off at parse time; an HtmlInline or HtmlBlock that still appears renders as text.
// A refused link renders its text only. A refused image renders as a plain link when the URL
// is a safe link, else as its alt text. An allowed image is a MarkdownImage, which swaps to an
// "image unavailable" placeholder when the load fails. Soft line breaks render as <br> because
// feedback bodies are typed as plain text, where a newline means a new line.
// Each child renders inside its own region so the fixed sequence numbers diff correctly.
#endregion

namespace TimeWarp.Architecture.Components;

using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class SafeMarkdown : ComponentBase
{
  private static readonly IReadOnlySet<string> NoImages = new HashSet<string>();

  [Parameter] public string? Markdown { get; set; }

  /// <summary>Exact image URLs that may render inline. Every other image is refused.</summary>
  [Parameter] public IReadOnlySet<string>? AllowedImageSources { get; set; }

  [Parameter] public string? DataQa { get; set; }

  private IReadOnlySet<string> AllowedImages => AllowedImageSources ?? NoImages;

  protected override void BuildRenderTree(RenderTreeBuilder builder)
  {
    builder.OpenElement(0, "div");
    builder.AddAttribute(1, "class", "safe-markdown");
    builder.AddAttribute(2, "data-qa", DataQa);
    RenderBlocks(builder, SafeMarkdownPolicy.Parse(Markdown));
    builder.CloseElement();
  }

  private void RenderBlocks(RenderTreeBuilder builder, ContainerBlock container)
  {
    foreach (Block block in container)
    {
      builder.OpenRegion(10);
      RenderBlock(builder, block);
      builder.CloseRegion();
    }
  }

  private void RenderBlock(RenderTreeBuilder builder, Block block)
  {
    switch (block)
    {
      case HeadingBlock heading:
        builder.OpenElement(20, "h" + Math.Clamp(heading.Level + 2, 3, 6));
        RenderInlines(builder, heading.Inline);
        builder.CloseElement();
        break;
      case ParagraphBlock paragraph:
        builder.OpenElement(21, "p");
        RenderInlines(builder, paragraph.Inline);
        builder.CloseElement();
        break;
      case QuoteBlock quote:
        builder.OpenElement(22, "blockquote");
        RenderBlocks(builder, quote);
        builder.CloseElement();
        break;
      case ListBlock list:
        builder.OpenElement(23, list.IsOrdered ? "ol" : "ul");
        if (list.IsOrdered && int.TryParse(list.OrderedStart, out int start) && start != 1)
        {
          builder.AddAttribute(24, "start", start);
        }

        RenderBlocks(builder, list);
        builder.CloseElement();
        break;
      case ListItemBlock item:
        builder.OpenElement(25, "li");
        RenderBlocks(builder, item);
        builder.CloseElement();
        break;
      case ThematicBreakBlock:
        builder.OpenElement(26, "hr");
        builder.CloseElement();
        break;
      case CodeBlock code:
        builder.OpenElement(27, "pre");
        builder.OpenElement(28, "code");
        builder.AddContent(29, string.Join('\n', code.Lines.Lines.Take(code.Lines.Count).Select(static line => line.ToString())));
        builder.CloseElement();
        builder.CloseElement();
        break;
      case LeafBlock leaf when leaf.Inline is not null:
        builder.OpenElement(30, "p");
        RenderInlines(builder, leaf.Inline);
        builder.CloseElement();
        break;
      case ContainerBlock nested:
        RenderBlocks(builder, nested);
        break;
      default:
        // Link reference definitions and blank lines render nothing.
        break;
    }
  }

  private void RenderInlines(RenderTreeBuilder builder, ContainerInline? container)
  {
    if (container is null)
    {
      return;
    }

    foreach (Inline inline in container)
    {
      builder.OpenRegion(40);
      RenderInline(builder, inline);
      builder.CloseRegion();
    }
  }

  private void RenderInline(RenderTreeBuilder builder, Inline inline)
  {
    switch (inline)
    {
      case LiteralInline literal:
        builder.AddContent(50, literal.Content.ToString());
        break;
      case CodeInline code:
        builder.OpenElement(51, "code");
        builder.AddContent(52, code.Content);
        builder.CloseElement();
        break;
      case LineBreakInline:
        builder.OpenElement(53, "br");
        builder.CloseElement();
        break;
      case HtmlEntityInline entity:
        builder.AddContent(54, entity.Transcoded.ToString());
        break;
      case HtmlInline html:
        builder.AddContent(55, html.Tag);
        break;
      case AutolinkInline autolink:
        RenderAutolink(builder, autolink);
        break;
      case EmphasisInline emphasis:
        builder.OpenElement(56, emphasis.DelimiterCount >= 2 ? "strong" : "em");
        RenderInlines(builder, emphasis);
        builder.CloseElement();
        break;
      case LinkInline { IsImage: true } image:
        RenderImage(builder, image);
        break;
      case LinkInline link:
        RenderLink(builder, link);
        break;
      case ContainerInline nested:
        RenderInlines(builder, nested);
        break;
      default:
        builder.AddContent(57, inline.ToString());
        break;
    }
  }

  private static void RenderAutolink(RenderTreeBuilder builder, AutolinkInline autolink)
  {
    if (!autolink.IsEmail && SafeMarkdownPolicy.IsSafeLink(autolink.Url))
    {
      OpenAnchor(builder, autolink.Url);
      builder.AddContent(60, autolink.Url);
      builder.CloseElement();
    }
    else
    {
      builder.AddContent(61, autolink.Url);
    }
  }

  private void RenderLink(RenderTreeBuilder builder, LinkInline link)
  {
    bool safe = SafeMarkdownPolicy.IsSafeLink(link.Url);
    if (safe)
    {
      OpenAnchor(builder, link.Url!);
    }

    RenderInlines(builder, link);
    if (safe)
    {
      builder.CloseElement();
    }
  }

  private void RenderImage(RenderTreeBuilder builder, LinkInline image)
  {
    string alt = AltText(image);
    if (SafeMarkdownPolicy.IsAllowedImage(image.Url, AllowedImages))
    {
      builder.OpenComponent<MarkdownImage>(70);
      builder.AddComponentParameter(71, nameof(MarkdownImage.Src), image.Url);
      builder.AddComponentParameter(72, nameof(MarkdownImage.Alt), alt);
      builder.CloseComponent();
    }
    else if (SafeMarkdownPolicy.IsSafeLink(image.Url))
    {
      OpenAnchor(builder, image.Url!);
      builder.AddContent(73, alt.Length > 0 ? alt : image.Url);
      builder.CloseElement();
    }
    else
    {
      builder.AddContent(74, alt);
    }
  }

  private static void OpenAnchor(RenderTreeBuilder builder, string url)
  {
    builder.OpenElement(80, "a");
    builder.AddAttribute(81, "href", url);
    builder.AddAttribute(82, "rel", "nofollow noopener noreferrer");
  }

  private static string AltText(ContainerInline container)
  {
    System.Text.StringBuilder text = new();
    foreach (Inline inline in container.Descendants<Inline>())
    {
      switch (inline)
      {
        case LiteralInline literal:
          text.Append(literal.Content.ToString());
          break;
        case CodeInline code:
          text.Append(code.Content);
          break;
        case HtmlEntityInline entity:
          text.Append(entity.Transcoded.ToString());
          break;
        default:
          break;
      }
    }

    return text.ToString();
  }
}
