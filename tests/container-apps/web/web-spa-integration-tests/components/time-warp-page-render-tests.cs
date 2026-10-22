#region Purpose
// Render TimeWarpPage with HtmlRenderer and pin the optional description and actions slots.
#endregion

#region Design
// Static render over the SPA's TimeWarp.State container. The shell's children (nav, profile,
// WebMCP surface, passkey prompt) resolve from that container; JS import throws so the WebMCP
// publish treats the browser API as absent. CascadingAuthenticationState wraps the page because
// Profile's AuthorizeView requires it and the shell does not host that cascade itself (Routes does).
// Neither slot leaves the title, breadcrumb, and body in today's order with no header row.
// Scope attributes change when the stylesheet changes, so the empty-slot contract is that
// structure, not a byte-for-byte snapshot.
#endregion

namespace TimeWarpPageRender_;

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
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;
using TimeWarp.State.Plus;

[TestTag("Unit")]
public class TimeWarpPage_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TimeWarpPage_Should_>();

  public static async Task Render_Title_And_Body_Without_Header_Slots()
  {
    string html = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["ChildContent"] = Text("Page body")
    });

    html.ShouldNotContain("twe-page__header");
    html.ShouldNotContain("twe-page__description");
    html.ShouldNotContain("twe-page__actions");
    Title(html).ShouldBe("Staff");
    html.ShouldContain("twe-page__crumbs");
    html.ShouldContain("Page body");
    int titleAt = html.IndexOf("twe-page__title", StringComparison.Ordinal);
    int crumbsAt = html.IndexOf("twe-page__crumbs", StringComparison.Ordinal);
    int bodyAt = html.IndexOf("twe-page__body", StringComparison.Ordinal);
    int contentAt = html.IndexOf("Page body", StringComparison.Ordinal);
    titleAt.ShouldBeGreaterThan(-1);
    crumbsAt.ShouldBeGreaterThan(titleAt);
    bodyAt.ShouldBeGreaterThan(crumbsAt);
    contentAt.ShouldBeGreaterThan(bodyAt);
    CountOpen(html, "h1").ShouldBe(1);
  }

  public static async Task Show_Description_When_Set_And_Omit_It_When_Unset()
  {
    string shown = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["Description"] = Text("Manage the team."),
      ["ChildContent"] = Text("Page body")
    });

    shown.ShouldContain("twe-page__header");
    shown.ShouldContain("twe-page__description");
    shown.ShouldContain("Manage the team.");
    shown.ShouldNotContain("twe-page__actions");
    Title(shown).ShouldBe("Staff");

    string hidden = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["Actions"] = Mark("PageAction", "Add item"),
      ["ChildContent"] = Text("Page body")
    });

    hidden.ShouldNotContain("twe-page__description");
    hidden.ShouldNotContain("Manage the team.");
  }

  public static async Task Show_Actions_When_Set_And_Omit_Them_When_Unset()
  {
    string shown = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["Actions"] = Mark("PageAction", "Add item"),
      ["ChildContent"] = Text("Page body")
    });

    shown.ShouldContain("twe-page__header");
    shown.ShouldContain("twe-page__actions");
    shown.ShouldContain("data-qa=\"PageAction\"");
    shown.ShouldContain("Add item");

    string hidden = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["Description"] = Text("Manage the team."),
      ["ChildContent"] = Text("Page body")
    });

    hidden.ShouldNotContain("twe-page__actions");
    hidden.ShouldNotContain("data-qa=\"PageAction\"");
  }

  public static async Task Render_Exactly_One_H1_With_Slots_And_A_Section_Header()
  {
    string html = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["Description"] = Text("Manage the team."),
      ["Actions"] = Mark("PageAction", "Add item"),
      ["ChildContent"] = (RenderFragment)(builder =>
      {
        builder.OpenComponent<SectionHeader>(0);
        builder.AddAttribute(1, "Title", "Reports");
        builder.AddAttribute(2, "Subtitle", "Current period");
        builder.CloseComponent();
      })
    });

    CountOpen(html, "h1").ShouldBe(1);
    Title(html).ShouldBe("Staff");
    CountOpen(html, "h2").ShouldBe(1);
    html.ShouldContain("Reports");
    html.ShouldContain("Current period");
    html.ShouldContain("Manage the team.");
    html.ShouldContain("Add item");
  }

  private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

  private static RenderFragment Mark(string dataQa, string text) => builder =>
  {
    builder.OpenElement(0, "span");
    builder.AddAttribute(1, "data-qa", dataQa);
    builder.AddContent(2, text);
    builder.CloseElement();
  };

  private static async Task<string> RenderAsync(Dictionary<string, object> parameters)
  {
    ServiceCollection services = new();
    services.AddLogging();
    services.AddFluentUIComponents();
    services.AddAuthorizationCore(TimeWarp.Architecture.PolicyRegistration.AddPolicies);
    services.AddScoped<AuthenticationStateProvider, FixedAuthenticationStateProvider>();
    services.AddScoped<IJSRuntime, HeadlessJsRuntime>();
    services.AddScoped(_ => A.Fake<ISessionStorageService>());
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

    await using ServiceProvider provider = services.BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
    {
      Dictionary<string, object?> hostParameters = new()
      {
        ["PageParameters"] = parameters
      };
      return (await renderer.RenderComponentAsync<ShellHost>(ParameterView.FromDictionary(hostParameters))).ToHtmlString();
    });
    return WebUtility.HtmlDecode(html);
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

  private sealed class ShellHost : ComponentBase
  {
    [Parameter] public Dictionary<string, object> PageParameters { get; set; } = new();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<CascadingAuthenticationState>(0);
      builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
      {
        child.OpenComponent<TimeWarpPage>(0);
        child.AddMultipleAttributes(1, PageParameters);
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
