#region Purpose
// Shared private helpers and the PageHost double for the DesignSystemPage_Should_ partial class.
#endregion

namespace DesignSystemPageContract_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.DesignSystem;

[TestTag("Unit")]
public partial class DesignSystemPage_Should_
{

  private static string Catalog(string html) =>
    Slice(html, "data-qa=\"DesignSystemCatalog\"", "data-qa=\"DesignSystemStage\"");

  private static void AppearInOrder(string html, params string[] markers)
  {
    int cursor = -1;
    foreach (string marker in markers)
    {
      int found = html.IndexOf(marker, cursor + 1, StringComparison.Ordinal);
      found.ShouldBeGreaterThan(cursor, marker);
      cursor = found;
    }
  }

  private static void ContainTitlesInOrder(IReadOnlyList<string> actual, params string[] expected)
  {
    int cursor = -1;
    foreach (string title in expected)
    {
      int found = -1;
      for (int index = cursor + 1; index < actual.Count; index++)
      {
        if (actual[index] == title)
        {
          found = index;
          break;
        }
      }

      found.ShouldBeGreaterThan(cursor, title);
      cursor = found;
    }
  }

  private static string RoleCard(string roles, string title)
  {
    int searchFrom = 0;
    while (searchFrom < roles.Length)
    {
      int classAt = roles.IndexOf("twe-role-card__title", searchFrom, StringComparison.Ordinal);
      classAt.ShouldBeGreaterThan(-1, title);
      int content = roles.IndexOf('>', classAt, StringComparison.Ordinal);
      content.ShouldBeGreaterThan(classAt, title);
      int close = roles.IndexOf('<', content + 1, StringComparison.Ordinal);
      close.ShouldBeGreaterThan(content, title);
      string actual = roles.Substring(content + 1, close - content - 1).Trim();
      if (actual == title)
      {
        int start = roles.LastIndexOf("<button", classAt, StringComparison.Ordinal);
        start.ShouldBeGreaterThan(-1, title);
        int end = roles.IndexOf("</button>", close, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(close, title);
        return roles.Substring(start, end - start);
      }

      searchFrom = close + 1;
    }

    throw new InvalidOperationException("Role card not found: " + title);
  }

  private static string TagContaining(string source, string marker)
  {
    int at = source.IndexOf(marker, StringComparison.Ordinal);
    at.ShouldBeGreaterThan(-1, marker);
    int start = source.LastIndexOf('<', at, StringComparison.Ordinal);
    int end = source.IndexOf('>', at, StringComparison.Ordinal);
    end.ShouldBeGreaterThan(at, marker);
    return source.Substring(start, end - start + 1);
  }

  private static string Slice(string html, string startMarker, string endMarker)
  {
    int start = html.IndexOf(startMarker, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, startMarker);
    int end = html.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
    end.ShouldBeGreaterThan(start, endMarker);
    return html.Substring(start, end - start);
  }

  private static List<string> CardTitles(string html)
  {
    List<string> titles = [];
    const string marker = "twe-card__title";
    int index = 0;
    while (index < html.Length)
    {
      int found = html.IndexOf(marker, index, StringComparison.Ordinal);
      if (found < 0)
      {
        break;
      }

      int content = html.IndexOf('>', found, StringComparison.Ordinal);
      int end = html.IndexOf("</h2", content, StringComparison.Ordinal);
      titles.Add(html.Substring(content + 1, end - content - 1).Trim());
      index = end;
    }

    return titles;
  }

  private static void AssertPageHeaderOwnsTheOnlyActions(string html, string slug)
  {
    int count = Count(html, "twe-page__actions");
    if (slug is "all" or "page-header")
    {
      count.ShouldBe(1, slug);
      int example = html.IndexOf("data-qa=\"PageHeaderExample\"", StringComparison.Ordinal);
      int actions = html.IndexOf("twe-page__actions", StringComparison.Ordinal);
      example.ShouldBeGreaterThan(-1, slug);
      actions.ShouldBeGreaterThan(example, slug);
      html.LastIndexOf("twe-page__actions", StringComparison.Ordinal).ShouldBe(actions, slug);
    }
    else
    {
      count.ShouldBe(0, slug);
    }
  }

  private static int Count(string source, string marker)
  {
    int count = 0;
    int index = 0;
    while (index < source.Length)
    {
      int found = source.IndexOf(marker, index, StringComparison.Ordinal);
      if (found < 0)
      {
        break;
      }

      count++;
      index = found + marker.Length;
    }

    return count;
  }

  private static string FeatureRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      string candidate = Path.Combine(
        dir.FullName,
        "source",
        "container-apps",
        "web",
        "projects",
        "web-spa",
        "features",
        "design-system");
      if (Directory.Exists(candidate))
      {
        return candidate;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate features/design-system from " + AppContext.BaseDirectory);
  }

  private static string FeatureFile(params string[] relative)
  {
    string[] parts = new string[relative.Length + 1];
    parts[0] = FeatureRoot();
    Array.Copy(relative, 0, parts, 1, relative.Length);
    return Path.Combine(parts);
  }

  private static bool IsKebabSlug(string slug)
  {
    if (slug.Length == 0 || slug[0] == '-' || slug[^1] == '-')
    {
      return false;
    }

    bool hyphen = false;
    foreach (char character in slug)
    {
      if (character == '-')
      {
        if (hyphen)
        {
          return false;
        }

        hyphen = true;
        continue;
      }

      hyphen = false;
      bool letter = character is >= 'a' and <= 'z';
      bool digit = character is >= '0' and <= '9';
      if (!letter && !digit)
      {
        return false;
      }
    }

    return true;
  }

  private static IEnumerable<string> FeatureFiles() =>
    Directory.EnumerateFiles(FeatureRoot(), "*", SearchOption.AllDirectories)
      .Where(static file => file.EndsWith(".razor", StringComparison.Ordinal)
        || file.EndsWith(".cs", StringComparison.Ordinal)
        || file.EndsWith(".css", StringComparison.Ordinal));

  private static string SourceProductName() => string.Concat("cru", "nchit");

  private static async Task<string> RenderAsync(string? part = null)
  {
    await using ServiceProvider provider = StaticRenderHost.CreateServices().BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    Dictionary<string, object?> parameters = new()
    {
      [nameof(PageHost.Part)] = part,
    };
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<PageHost>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private sealed class PageHost : ComponentBase
  {
    [Parameter] public string? Part { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<CascadingAuthenticationState>(0);
      builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
      {
        child.OpenComponent<DesignSystemPage>(0);
        child.AddComponentParameter(1, nameof(DesignSystemPage.Part), Part);
        child.CloseComponent();
      }));
      builder.CloseComponent();
    }
  }
}
