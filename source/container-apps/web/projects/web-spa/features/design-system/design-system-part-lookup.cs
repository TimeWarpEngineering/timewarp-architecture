#region Purpose
// Slug lookup, canonical URL, and render fragment for a design-system part.
#endregion

#region Design
// DesignSystemParts.PageOrder is the authority; this type only reads it. Split from
// DesignSystemParts in task 512 so the part table stays under the TWA0038 line cap.
// Resolve compares ordinal and case-insensitive. An empty or blank slug is All, not a
// part named all. An unknown slug is Unknown. Url emits the canonical lowercase slug under
// /DesignSystem/. Render opens the part's sample component with no parameters.
#endregion

namespace TimeWarp.Architecture.Features.DesignSystem;

/// <summary>Finds a design-system part by slug and renders or links it.</summary>
internal static class DesignSystemPartLookup
{
  internal static DesignSystemParts.Selection Resolve(string? slug)
  {
    if (string.IsNullOrWhiteSpace(slug))
    {
      return new DesignSystemParts.Selection(DesignSystemParts.SelectionKind.All, null);
    }

    foreach (DesignSystemParts.Part part in DesignSystemParts.PageOrder)
    {
      if (part.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
      {
        return new DesignSystemParts.Selection(DesignSystemParts.SelectionKind.Part, part);
      }
    }

    return new DesignSystemParts.Selection(DesignSystemParts.SelectionKind.Unknown, null);
  }

  internal static bool TryGet(string slug, out DesignSystemParts.Part? part)
  {
    DesignSystemParts.Selection selection = Resolve(slug);
    part = selection.Part;
    return selection.Kind == DesignSystemParts.SelectionKind.Part;
  }

  internal static string Url(DesignSystemParts.Part part) =>
    string.Concat(DesignSystemPage.GetPageUrl(), "/", part.Slug);

  internal static RenderFragment Render(DesignSystemParts.Part part) => builder =>
  {
    builder.OpenComponent(0, part.Sample);
    builder.CloseComponent();
  };
}
