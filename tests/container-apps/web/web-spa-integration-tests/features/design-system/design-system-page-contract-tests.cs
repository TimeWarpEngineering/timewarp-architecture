#region Purpose
// Pin the composed DesignSystemPage: every upstreamed part, in page order, with its sample.
#endregion

#region Design
// Sibling ports added a card and a leaf render test, and left this page contract for task 379-017.
// HtmlRenderer over StaticRenderHost, shared with TimeWarpPage_Should_, shows the catalog
// without a browser. Scope attributes change with the stylesheet, so the contract is structure
// and sample text. A closed drawer omits its title. The form fields are the only stored choice.
// The source scan counts every @bind on the forms sample, rejects any Changed= callback
// on the demos, and requires the ConfirmSample button's OnClick to be ConfirmSampleAsync,
// which calls NoSubDesignSystemState.ConfirmSample(). The catalog filter is the one
// ValueChanged, and it is component-local text, not a stored demo choice. That
// exemption covers only the Changed= and ValueChanged assertions. The product-name
// scan still reads DesignSystemCatalog.razor.
// Group headings are h2 elements. Each nested part list is labelled by that heading id.
// A static render cannot see an unused callback parameter.
// The absent-name check composes its token so this file does not spell the source product name.
// Slugs are lowercase kebab-case. Resolve is ordinal and case-insensitive. /DesignSystem/all
// is not a part. Usage is rendered on the single-part view only.
// The shell header has no actions. twe-page__actions appears once, after the Page header
// example, and a non-header part renders none. That example's title is a div, not an h1.
// All-view card titles are Part.Title in PageOrder. A group heading is the enum name, and the
// catalog shows a group only when it has a part, in Group declaration order.
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
public class DesignSystemPage_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DesignSystemPage_Should_>();

  public static async Task List_Every_Part_In_Page_Order()
  {
    DesignSystemParts.PageOrder.Select(static part => part.Slug).ShouldBeUnique();
    foreach (DesignSystemParts.Part part in DesignSystemParts.PageOrder)
    {
      IsKebabSlug(part.Slug).ShouldBeTrue(part.Slug);
      DesignSystemParts.TryGet(part.Slug, out DesignSystemParts.Part? resolved).ShouldBeTrue(part.Slug);
      resolved!.Title.ShouldBe(part.Title);
      DesignSystemParts.TryGet(part.Slug.ToUpperInvariant(), out DesignSystemParts.Part? ignoredCase).ShouldBeTrue(part.Slug);
      ignoredCase!.Slug.ShouldBe(part.Slug);
      DesignSystemParts.Url(part).ShouldBe("/DesignSystem/" + part.Slug);
    }

    DesignSystemParts.Resolve(null).Kind.ShouldBe(DesignSystemParts.SelectionKind.All);
    DesignSystemParts.Resolve(" ").Kind.ShouldBe(DesignSystemParts.SelectionKind.All);
    DesignSystemParts.Resolve("all").Kind.ShouldBe(DesignSystemParts.SelectionKind.Unknown);
    DesignSystemParts.TryGet("no-such-part", out _).ShouldBeFalse();

    string html = await RenderAsync();

    StaticRenderHost.CountOpen(html, "h1").ShouldBe(1);
    StaticRenderHost.Title(html).ShouldBe("Design System");
    html.ShouldContain("twe-page__description");
    AssertPageHeaderOwnsTheOnlyActions(html, "all");
    html.Contains(SourceProductName(), StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    html.ShouldNotContain("Sample drawer");
    html.ShouldNotContain("data-qa=\"DesignSystemUsage\"");
    html.ShouldNotContain(">Omitted<");
    string catalog = Catalog(html);
    Count(catalog, "aria-current=\"page\"").ShouldBe(1);
    TagContaining(catalog, "data-qa=\"DesignSystemAll\"").ShouldContain("aria-current=\"page\"");
    foreach (DesignSystemParts.Group group in Enum.GetValues<DesignSystemParts.Group>())
    {
      if (!Array.Exists(DesignSystemParts.PageOrder, part => part.Group == group))
      {
        continue;
      }

      string headingId = "ds-catalog-" + group.ToString().ToLowerInvariant();
      string heading = TagContaining(catalog, $"id=\"{headingId}\"");
      heading.StartsWith("<h2", StringComparison.Ordinal).ShouldBeTrue(group.ToString());
      heading.ShouldContain("ds-catalog__heading");
      catalog.ShouldContain($">{group}</h2>");
      string parts = TagContaining(catalog, $"aria-labelledby=\"{headingId}\"");
      parts.StartsWith("<ul", StringComparison.Ordinal).ShouldBeTrue(group.ToString());
    }

    catalog.ShouldNotContain("<p class=\"ds-catalog__heading\"");

    string[] partTitles = [.. DesignSystemParts.PageOrder.Select(static part => part.Title)];
    ContainTitlesInOrder(CardTitles(html), partTitles);

    AppearInOrder
    (
      html,
      "Live brand tokens, type scale, and components",
      "data-qa=\"DesignSystemBrandName\"",
      "TimeWarp.Architecture",
      "data-qa=\"DesignSystemBrandSeed\"",
      "#55409c",
      "Harmonized",
      "Purple (primary)",
      "28 — Page title",
      "value=\"Ada Lovelace\"",
      "data-qa=\"DesignSystemFormField\"",
      "Reports",
      "Summary for the current period.",
      "data-qa=\"DesignSystemPageSplit\"",
      "Search…",
      "aria-label=\"Sample views\"",
      "aria-label=\"Sample period\"",
      "data-qa=\"DesignSystemPeriodStepper\"",
      "href=\"/Profile\"",
      "data-qa=\"DesignSystemUserCard\"",
      "data-qa=\"DesignSystemSignInOne\"",
      "data-qa=\"ContinueWithPasskey\"",
      "data-qa=\"DesignSystemSignInBoth\"",
      "data-qa=\"ContinueWithMicrosoft365\"",
      "No reports yet",
      "No locations selected",
      "No rows match",
      "data-qa=\"DrawerOpen\"",
      "Open items",
      "49.5%",
      "Last 12 months",
      "Exceptions",
      "data-qa=\"DesignSystemDefinitionList\"",
      "data-qa=\"DesignSystemChartCard\"",
      "All locations",
      "twe-line-chart",
      "Series A",
      "Series B",
      "Series C",
      "4.2%",
      "1.8%",
      "98%",
      "90%",
      "78%",
      "Operations",
      "Not mapped",
      "Connected",
      "Needs attention",
      "data-qa=\"DesignSystemIntegrationWarning\"",
      "data-qa=\"DesignSystemGroupedTable\"",
      "twe-numeric",
      ">Alpha<",
      "In progress",
      "data-qa=\"ConfirmSample\"",
      "data-qa=\"DangerOutline\"",
      "Lost",
      "data-qa=\"DesignSystemRoleBadges\"",
      "data-qa=\"DesignSystemPermissionGrid\"",
      "View assigned work",
      "Not granted",
      "data-qa=\"DesignSystemDevNote\"",
      "inline code",
      "data-qa=\"ToastInfo\"",
      "data-qa=\"ToastSuccess\"",
      "data-qa=\"ToastWarning\"",
      "data-qa=\"ToastError\"",
      "data-qa=\"ToastException\""
    );

    string roles = Slice(html, "data-qa=\"DesignSystemRoleCards\"", "data-qa=\"DesignSystemPermissionGrid\"");
    Count(roles, "aria-checked=\"true\"").ShouldBe(1);
    Count(roles, "aria-checked=\"false\"").ShouldBe(2);
    string owner = RoleCard(roles, "Owner");
    string admin = RoleCard(roles, "Admin");
    string staff = RoleCard(roles, "Staff");
    owner.ShouldContain("aria-checked=\"true\"");
    owner.ShouldContain("tabindex=\"0\"");
    admin.ShouldContain("aria-checked=\"false\"");
    admin.ShouldContain("tabindex=\"-1\"");
    admin.ShouldNotContain("aria-checked=\"true\"");
    staff.ShouldContain("aria-checked=\"false\"");
    staff.ShouldContain("tabindex=\"-1\"");
    staff.ShouldNotContain("aria-checked=\"true\"");
    roles.IndexOf("aria-checked=\"true\"", StringComparison.Ordinal)
      .ShouldBeLessThan(roles.IndexOf(">Admin<", StringComparison.Ordinal));
    CardTitles(html).Count.ShouldBe(DesignSystemParts.PageOrder.Length);
  }

  public static async Task Render_Each_Part_With_Its_Usage()
  {
    foreach (DesignSystemParts.Part part in DesignSystemParts.PageOrder)
    {
      string html = await RenderAsync(part.Slug);
      List<string> titles = CardTitles(html);
      titles.Count.ShouldBe(1, part.Slug);
      titles[0].ShouldBe(part.Title);
      html.ShouldContain("data-qa=\"DesignSystemUsage\"");
      html.ShouldContain(part.Usage);
      string catalog = Catalog(html);
      Count(catalog, "aria-current=\"page\"").ShouldBe(1, part.Slug);
      TagContaining(catalog, $"data-part=\"{part.Slug}\"").ShouldContain("aria-current=\"page\"");
      StaticRenderHost.Title(html).ShouldBe("Design System");
      AssertPageHeaderOwnsTheOnlyActions(html, part.Slug);
    }
  }

  public static async Task Render_The_Page_Header_Sample_Without_An_H1()
  {
    string html = await RenderAsync("page-header");
    string example = Slice(html, "data-qa=\"PageHeaderExample\"", "data-qa=\"PageHeaderRule\"");
    example.ShouldContain("twe-page__title");
    example.ShouldContain("Reports");
    example.IndexOf("<h1", StringComparison.Ordinal).ShouldBe(-1);
    StaticRenderHost.CountOpen(html, "h1").ShouldBe(1);
    StaticRenderHost.Title(html).ShouldBe("Design System");
  }

  public static async Task Show_An_Unknown_Slug_Inside_The_Page()
  {
    string html = await RenderAsync("no-such-part");
    StaticRenderHost.CountOpen(html, "h1").ShouldBe(1);
    StaticRenderHost.Title(html).ShouldBe("Design System");
    html.ShouldContain("data-qa=\"DesignSystemUnknown\"");
    html.ShouldContain("No such part.");
    html.ShouldContain("href=\"/DesignSystem\"");
    html.ShouldNotContain("data-qa=\"DesignSystemUsage\"");
    Catalog(html).ShouldNotContain("aria-current=\"page\"");
    CardTitles(html).ShouldBeEmpty();
  }

  public static Task Author_No_Inline_Style_Attributes()
  {
    foreach (string file in FeatureFiles())
    {
      File.ReadAllText(file).Contains("style=", StringComparison.Ordinal).ShouldBeFalse(file);
    }

    return Task.CompletedTask;
  }

  public static Task Keep_Stored_Choices_On_The_Form_Fields_Only()
  {
    string forms = File.ReadAllText(FeatureFile("samples", "FormsSample.razor"));
    Count(forms, "@bind").ShouldBe(2);
    forms.ShouldContain("@bind-Value=\"FormDemoName\"");
    forms.ShouldContain("@bind-Value=\"FormDemoEmail\"");

    string confirmations = File.ReadAllText(FeatureFile("samples", "ConfirmationsSample.razor"));
    TagContaining(confirmations, "data-qa=\"ConfirmSample\"").ShouldContain("OnClick=\"ConfirmSampleAsync\"");
    int handler = confirmations.IndexOf("ConfirmSampleAsync()", StringComparison.Ordinal);
    handler.ShouldBeGreaterThan(-1, "ConfirmSampleAsync()");
    int statementEnd = confirmations.IndexOf(';', handler, StringComparison.Ordinal);
    statementEnd.ShouldBeGreaterThan(handler);
    confirmations.Substring(handler, statementEnd - handler).ShouldContain("NoSubDesignSystemState.ConfirmSample()");

    string drawers = File.ReadAllText(FeatureFile("samples", "DrawersSample.razor"));
    drawers.ShouldContain("ApplicationState.SetActiveModal(DesignSystemDrawerId)");
    drawers.ShouldContain("ApplicationState.CloseModal()");

    foreach (string file in FeatureFiles())
    {
      string source = File.ReadAllText(file);
      // ValueChanged="@…" also contains the Changed=" substring, so both assertions
      // skip the catalog filter. The product-name scan still reads that file.
      if (!file.EndsWith("DesignSystemCatalog.razor", StringComparison.Ordinal))
      {
        source.ShouldNotContain("Changed=\"");
        source.ShouldNotContain("ValueChanged");
      }

      source.IndexOf(SourceProductName(), StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
    }

    return Task.CompletedTask;
  }

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
