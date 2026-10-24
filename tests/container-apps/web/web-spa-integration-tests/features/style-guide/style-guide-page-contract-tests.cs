#region Purpose
// Pin the composed StyleGuidePage: every upstreamed part, in page order, with its sample.
#endregion

#region Design
// Sibling ports added a card and a leaf render test, and left this page contract for task 379-017.
// HtmlRenderer over StaticRenderHost, shared with TimeWarpPage_Should_, shows the catalog
// without a browser. Scope attributes change with the stylesheet, so the contract is structure
// and sample text. A closed drawer omits its title. The form fields are the only stored choice.
// The source scan counts every @bind, rejects any Changed= callback, and requires the
// ConfirmSample button's OnClick to be ConfirmSampleAsync, which calls
// NoSubStyleGuideState.ConfirmSample(). A static render cannot see an unused callback parameter.
// The absent-name check composes its token so this file does not spell the source product name.
#endregion

namespace StyleGuidePageContract_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.StyleGuide;

[TestTag("Unit")]
public class StyleGuidePage_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StyleGuidePage_Should_>();

  public static async Task Show_Every_Upstreamed_Part_In_Page_Order()
  {
    string html = await RenderAsync();

    StaticRenderHost.CountOpen(html, "h1").ShouldBe(1);
    StaticRenderHost.Title(html).ShouldBe("Style Guide");
    html.ShouldContain("twe-page__description");
    html.ShouldContain("twe-page__actions");
    html.Contains(SourceProductName(), StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    html.ShouldNotContain("Sample drawer");

    ContainTitlesInOrder
    (
      CardTitles(html),
      "Brand",
      "Color Tokens",
      "Type Scale",
      "Forms",
      "Section",
      "Section Header",
      "Filter Bar",
      "Tabs",
      "Segmented Control",
      "Avatars",
      "User card",
      "Sign-in",
      "Empty States",
      "Drawers",
      "KPI Cards",
      "Chart frame",
      "Trend, SLA, and class pills",
      "Integration Cards",
      "Grouped Table",
      "Count Chip",
      "Confirmations",
      "Buttons (FluentUI, themed)",
      "Status Badges",
      "Role badges",
      "Role cards",
      "Permission grid",
      "Dev note",
      "Message bars (FluentUI)",
      "Notifications (message bars)"
    );

    AppearInOrder
    (
      html,
      "Live brand tokens, type scale, and components",
      "data-qa=\"StyleGuideBrandName\"",
      "TimeWarp.Architecture",
      "data-qa=\"StyleGuideBrandSeed\"",
      "#55409c",
      "Harmonized",
      "Purple (primary)",
      "28 — Page title",
      "value=\"Ada Lovelace\"",
      "data-qa=\"StyleGuideFormField\"",
      "Reports",
      "Summary for the current period.",
      "Search…",
      "aria-label=\"Sample views\"",
      "aria-label=\"Sample period\"",
      "href=\"/Profile\"",
      "data-qa=\"StyleGuideUserCard\"",
      "data-qa=\"StyleGuideSignInOne\"",
      "data-qa=\"ContinueWithPasskey\"",
      "data-qa=\"StyleGuideSignInBoth\"",
      "data-qa=\"ContinueWithMicrosoft365\"",
      "No reports yet",
      "No locations selected",
      "No rows match",
      "data-qa=\"DrawerOpen\"",
      "Open items",
      "49.5%",
      "Last 12 months",
      "Exceptions",
      "data-qa=\"StyleGuideChartCard\"",
      "All locations",
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
      "data-qa=\"StyleGuideGroupedTable\"",
      ">Alpha<",
      "In progress",
      "data-qa=\"ConfirmSample\"",
      "data-qa=\"DangerOutline\"",
      "Lost",
      "data-qa=\"StyleGuideRoleBadges\"",
      "data-qa=\"StyleGuidePermissionGrid\"",
      "View assigned work",
      "Not granted",
      "data-qa=\"StyleGuideDevNote\"",
      "inline code",
      "data-qa=\"ToastInfo\"",
      "data-qa=\"ToastSuccess\"",
      "data-qa=\"ToastWarning\"",
      "data-qa=\"ToastError\"",
      "data-qa=\"ToastException\""
    );

    string roles = Slice(html, "data-qa=\"StyleGuideRoleCards\"", "data-qa=\"StyleGuidePermissionGrid\"");
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
  }

  public static Task Keep_Stored_Choices_On_The_Form_Fields_Only()
  {
    string source = File.ReadAllText(PagePath());
    Count(source, "@bind").ShouldBe(2);
    source.ShouldContain("@bind-Value=\"FormDemoName\"");
    source.ShouldContain("@bind-Value=\"FormDemoEmail\"");
    source.ShouldNotContain("Changed=\"");
    source.ShouldNotContain("ValueChanged");
    TagContaining(source, "data-qa=\"ConfirmSample\"").ShouldContain("OnClick=\"ConfirmSampleAsync\"");
    int handler = source.IndexOf("ConfirmSampleAsync()", StringComparison.Ordinal);
    handler.ShouldBeGreaterThan(-1, "ConfirmSampleAsync()");
    int statementEnd = source.IndexOf(';', handler, StringComparison.Ordinal);
    statementEnd.ShouldBeGreaterThan(handler);
    source.Substring(handler, statementEnd - handler).ShouldContain("NoSubStyleGuideState.ConfirmSample()");
    source.ShouldContain("ApplicationState.SetActiveModal(StyleGuideDrawerId)");
    source.ShouldContain("ApplicationState.CloseModal()");
    source.IndexOf(SourceProductName(), StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
    return Task.CompletedTask;
  }

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

  private static string PagePath()
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
        "style-guide",
        "pages",
        "StyleGuidePage.razor");
      if (File.Exists(candidate))
      {
        return candidate;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate StyleGuidePage.razor from " + AppContext.BaseDirectory);
  }

  private static string SourceProductName() => string.Concat("cru", "nchit");

  private static async Task<string> RenderAsync()
  {
    await using ServiceProvider provider = StaticRenderHost.CreateServices().BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<PageHost>(ParameterView.Empty)).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private sealed class PageHost : ComponentBase
  {
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<CascadingAuthenticationState>(0);
      builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
      {
        child.OpenComponent<StyleGuidePage>(0);
        child.CloseComponent();
      }));
      builder.CloseComponent();
    }
  }
}
