namespace TimeWarp.Architecture.Components;

using Microsoft.FluentUI.AspNetCore.Components;

public class LinkHelper
{

  /// <summary>
  /// Creates a render fragment for a link to the specified page.
  /// </summary>
  /// <param name="pageType">The type of the page.</param>
  /// <returns>A render fragment for a link to the page.</returns>
  public static RenderFragment Link(Type pageType)
  {
    string? policy = pageType.GetProperty("Policy")?.GetValue(null) as string;

    string title = pageType.GetProperty("Title")?.GetValue(null) as string ??
      throw new InvalidOperationException("The page type must have a static Title property.");

    Func<string> getPageUrl = pageType.GetProperty("GetPageUrl")?.GetValue(null) as Func<string> ??
      throw new InvalidOperationException("The page type must have a static GetPageUrl property.");

    Icon? icon = pageType.GetProperty("Icon")?.GetValue(null) as Icon;

    if (policy is null)
    {
      return builder =>
      {
        builder.OpenComponent<FluentNavLink>(0);
        builder.AddAttribute(1, "Href", getPageUrl?.Invoke());
        builder.AddAttribute(2, "Icon", icon);
        builder.AddContent(3, title);
        builder.CloseComponent();
      };
    }
    return builder =>
    {
      builder.OpenComponent<AuthorizedFluentNavLink>(0);
      builder.AddAttribute(1, "Policy", policy);
      builder.AddAttribute(2, "Href", getPageUrl?.Invoke());
      builder.AddAttribute(3, "Icon", icon);
      builder.AddContent(4, title);
      builder.CloseComponent();
    };
  }
}
