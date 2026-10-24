#region Purpose
// Pin the composed StyleGuidePage: every upstreamed part, in page order, with its sample.
#endregion

#region Design
// Sibling ports added a card and a leaf render test, and left this page contract for task 379-017.
// HtmlRenderer over the same shell host as TimeWarpPage_Should_ shows the catalog without a browser.
// FilterBar injects TimeProvider, so the host registers the system clock. The debounce does not run.
// Scope attributes change with the stylesheet, so the contract is structure and sample text.
// A closed drawer omits its title. The form fields are the only stored choice; that rule is a
// source scan because a static render cannot see an unused ValueChanged parameter.
// The absent-name check composes its token so this file does not spell the source product name.
#endregion

namespace StyleGuidePageContract_;

using System.Net;
using System.Security.Claims;
using Blazored.SessionStorage;
using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.StyleGuide;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;
using TimeWarp.State.Plus;

[TestTag("Unit")]
public class StyleGuidePage_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StyleGuidePage_Should_>();

  public static async Task Show_Every_Upstreamed_Part_In_Page_Order()
  {
    string html = await RenderAsync();

    CountOpen(html, "h1").ShouldBe(1);
    Title(html).ShouldBe("Style Guide");
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
      "data-qa=\"StyleGuideSignInMode\"",
      ">Passkey<",
      "data-qa=\"StyleGuideStaffNote\"",
      "Staff only.",
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
    roles.ShouldContain("aria-checked=\"true\"");
    roles.ShouldContain("tabindex=\"0\"");
    roles.ShouldContain("tabindex=\"-1\"");
    roles.ShouldContain("Owner");
    roles.ShouldContain("Admin");
    roles.ShouldContain("Staff");
  }

  public static Task Keep_Stored_Choices_On_The_Form_Fields_Only()
  {
    string source = File.ReadAllText(PagePath());
    Count(source, "@bind-Value").ShouldBe(2);
    source.ShouldContain("@bind-Value=\"FormDemoName\"");
    source.ShouldContain("@bind-Value=\"FormDemoEmail\"");
    source.ShouldNotContain("ValueChanged");
    source.ShouldContain("ApplicationState.SetActiveModal(StyleGuideDrawerId)");
    source.ShouldContain("ApplicationState.CloseModal()");
    source.ShouldContain("ConfirmSampleAsync");
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

  private static string Title(string html)
  {
    const string open = "<h1";
    int start = html.IndexOf(open, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, "page rendered no h1");
    int content = html.IndexOf('>', start, StringComparison.Ordinal);
    int end = html.IndexOf("</h1", content, StringComparison.Ordinal);
    return html.Substring(content + 1, end - content - 1).Trim();
  }

  private static int CountOpen(string html, string tagName)
  {
    int count = 0;
    string open = "<" + tagName;
    int index = 0;
    while (index < html.Length)
    {
      int found = html.IndexOf(open, index, StringComparison.Ordinal);
      if (found < 0)
      {
        break;
      }

      int after = found + open.Length;
      if (after >= html.Length || !char.IsAsciiLetterOrDigit(html[after]))
      {
        count++;
      }

      index = after;
    }

    return count;
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
    await using ServiceProvider provider = CreateServices().BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<PageHost>(ParameterView.Empty)).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static ServiceCollection CreateServices()
  {
    ServiceCollection services = new();
    services.AddLogging();
    services.AddSingleton(TimeProvider.System);
    services.AddFluentUIComponents();
    services.AddAuthorizationCore(TimeWarp.Architecture.PolicyRegistration.AddPolicies);
    services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider());
    services.AddScoped<IJSRuntime, HeadlessJsRuntime>();
    services.AddScoped(_ => A.Fake<ISessionStorageService>());
    services.AddSingleton<IOptions<BrandOptions>>(Options.Create(new BrandOptions()));
    services.AddSingleton<IOptions<SignInOptions>>(Options.Create(new SignInOptions()));
    services.AddWebSpaGeneratedMediator();
    services.AddTimeWarpState(options =>
    {
      options.Assemblies =
      [
        typeof(TimeWarpPage).Assembly,
        typeof(AssemblyMarker).Assembly
      ];
    });
    services.AddTimeWarpStateBlazor();
    services.AddActionCatalog(typeof(TimeWarpPage).Assembly);
    services.AddScoped<PageAgentRoute>();
    services.AddScoped<AgentCallOutcome>();
    services.AddScoped<WebMcpApprovalGate>();
    services.AddScoped<WebMcpDispatcher>();
    services.AddScoped<JsWebMcpModelContext>();
    services.AddScoped<WebMcpPublisher>();
    services.AddScoped<NavigationManager, TestNavigationManager>();
    return services;
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

  private sealed class FixedAuthenticationStateProvider : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
  }

  private sealed class HeadlessJsRuntime : IJSRuntime
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
      if (identifier == "import")
      {
        throw new JSException("no browser");
      }

      return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      if (identifier == "import")
      {
        throw new JSException("no browser");
      }

      return ValueTask.FromResult(default(TValue)!);
    }
  }
}
