#region Purpose
// Render TimeWarpPage with HtmlRenderer and pin the optional description and actions slots.
#endregion

#region Design
// Static render over StaticRenderHost, the SPA's TimeWarp.State container shared with
// DesignSystemPage_Should_. The shell's children (nav, profile, WebMCP surface, passkey prompt)
// resolve from that container; JS import throws so the WebMCP publish treats the browser API as
// absent. CascadingAuthenticationState wraps the page because Profile's AuthorizeView requires
// it and the shell does not host that cascade itself (Routes does).
// Neither slot leaves the title, breadcrumb, and body in page order with no header row.
// Scope attributes change when the stylesheet changes, so the empty-slot contract is that
// structure, not a byte-for-byte snapshot.
#endregion

namespace TimeWarpPageRender_;

using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Profiles;

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
    StaticRenderHost.Title(html).ShouldBe("Staff");
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
    StaticRenderHost.CountOpen(html, "h1").ShouldBe(1);
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
    StaticRenderHost.Title(shown).ShouldBe("Staff");

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

    StaticRenderHost.CountOpen(html, "h1").ShouldBe(1);
    StaticRenderHost.Title(html).ShouldBe("Staff");
    StaticRenderHost.CountOpen(html, "h2").ShouldBe(1);
    html.ShouldContain("Reports");
    html.ShouldContain("Current period");
    html.ShouldContain("Manage the team.");
    html.ShouldContain("Add item");
  }

  public static async Task Render_The_Configured_Logo_On_The_App_Bar_And_The_Focused_Page()
  {
    string html = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["ChildContent"] = Text("Page body")
    });

    html.ShouldContain("data-qa=\"BrandLogo\"");
    html.ShouldContain("src=\"images/timewarp-horizontal.svg\"");
    html.ShouldContain("alt=\"TimeWarp Architecture\"");
    html.ShouldContain("width=\"201\"");
    html.ShouldContain("height=\"48\"");

    BrandOptions brand = new()
    {
      LogoSrc = "https://cdn.example/mark.svg",
      LogoAlt = "Sample Product",
      LogoWidth = 80,
      LogoHeight = 32
    };
    string custom = await RenderAsync
    (
      new Dictionary<string, object>
      {
        ["Title"] = "Staff",
        ["ChildContent"] = Text("Page body")
      },
      brand
    );
    custom.ShouldContain("src=\"https://cdn.example/mark.svg\"");
    custom.ShouldContain("alt=\"Sample Product\"");
    custom.ShouldContain("width=\"80\"");
    custom.ShouldContain("height=\"32\"");

    string focused = await RenderFocusedAsync(brand);
    focused.ShouldContain("twe-focused__logo");
    focused.ShouldContain("src=\"https://cdn.example/mark.svg\"");
    focused.ShouldContain("alt=\"Sample Product\"");
    focused.ShouldContain("width=\"80\"");
    focused.ShouldContain("height=\"32\"");
    focused.ShouldContain("data-qa=\"BrandLogo\"");
  }

  public static async Task Keep_The_Palette_Search_And_Omit_The_Signed_Out_User_Card()
  {
    string html = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["ChildContent"] = Text("Page body")
    });

    html.ShouldContain("twe-appbar__search");
    html.ShouldContain("Search pages and commands (Ctrl-K)");
    html.ShouldContain("data-command-palette-trigger");
    html.ShouldContain("aria-label=\"Primary\"");
    html.ShouldNotContain("aria-label=\"Pinned\"");
    html.ShouldNotContain("twe-user-card");
    html.ShouldNotContain("data-qa=\"ShellUserCard\"");
  }

  public static async Task Keep_External_In_The_Primary_Nav()
  {
    string html = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["ChildContent"] = Text("Page body")
    });

    const string label = "aria-label=\"Primary\"";
    int start = html.IndexOf(label, StringComparison.Ordinal);
    int close = html.IndexOf("</nav>", start, StringComparison.Ordinal);
    int external = html.IndexOf("https://thefreezeteam.com/", StringComparison.Ordinal);
    start.ShouldBeGreaterThanOrEqualTo(0);
    close.ShouldBeGreaterThan(start);
    external.ShouldBeGreaterThan(start);
    external.ShouldBeLessThan(close);
    html[start..close].ShouldContain("https://github.com/TimeWarpEngineering/timewarp-architecture");
    html.ShouldNotContain("aria-label=\"Pinned\"");
    html.ShouldNotContain("twe-nav--pin");
  }

  public static async Task Omit_The_Search_Field_When_SearchMode_Is_Hidden()
  {
    string html = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["SearchMode"] = ShellSearchMode.Hidden,
      ["ChildContent"] = Text("Page body")
    });

    html.ShouldNotContain("twe-appbar__search");
    html.ShouldNotContain("Search pages and commands (Ctrl-K)");
    html.ShouldNotContain("data-command-palette-trigger");
  }

  public static async Task Render_A_Custom_Search_Fragment_Without_The_Palette_Field()
  {
    string html = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["SearchMode"] = ShellSearchMode.Custom,
      ["Search"] = Mark("CustomSearch", "Find a person"),
      ["ChildContent"] = Text("Page body")
    });

    html.ShouldContain("twe-appbar__search");
    html.ShouldContain("data-qa=\"CustomSearch\"");
    html.ShouldContain("Find a person");
    html.ShouldNotContain("Search pages and commands (Ctrl-K)");
    html.ShouldNotContain("data-command-palette-trigger");
  }

  public static async Task Omit_A_Custom_Search_Slot_When_The_Fragment_Is_Unset()
  {
    string html = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["SearchMode"] = ShellSearchMode.Custom,
      ["ChildContent"] = Text("Page body")
    });

    html.ShouldNotContain("twe-appbar__search");
    html.ShouldNotContain("data-command-palette-trigger");
  }

  public static async Task Render_A_User_Card_Fragment_In_Place_Of_The_Default()
  {
    string html = await RenderAsync(new Dictionary<string, object>
    {
      ["Title"] = "Staff",
      ["UserCard"] = Mark("SlotUserCard", "Slot person"),
      ["ChildContent"] = Text("Page body")
    });

    html.ShouldContain("twe-nav-column__user");
    html.ShouldContain("data-qa=\"SlotUserCard\"");
    html.ShouldContain("Slot person");
    html.ShouldNotContain("data-qa=\"ShellUserCard\"");
  }

  public static async Task Render_A_Static_User_Card_For_A_Signed_In_Person_Without_Profile_Permission()
  {
    ClaimsPrincipal user = new(new ClaimsIdentity(
      [new Claim(ClaimTypes.Name, "Ada Lovelace")],
      authenticationType: "Test"));

    string html = await RenderAsync
    (
      new Dictionary<string, object>
      {
        ["Title"] = "Staff",
        ["ChildContent"] = Text("Page body")
      },
      user: user
    );

    string root = UserCardTag(html);
    root.ShouldStartWith("<div ");
    root.ShouldContain("twe-user-card");
    root.ShouldContain("data-qa=\"ShellUserCard\"");
    root.ShouldNotContain("href");
    html.ShouldContain("Ada Lovelace");
  }

  public static async Task Link_The_User_Card_When_The_Profile_Policy_Allows_It()
  {
    ClaimsIdentity identity = new("Test");
    identity.AddClaim(new Claim(ClaimTypes.Name, "Ada Lovelace"));
    identity.AddClaim(new Claim(PermissionIds.ClaimType, PermissionIds.ProfileRead));

    string html = await RenderAsync
    (
      new Dictionary<string, object>
      {
        ["Title"] = "Staff",
        ["ChildContent"] = Text("Page body")
      },
      user: new ClaimsPrincipal(identity)
    );

    string root = UserCardTag(html);
    root.ShouldStartWith("<a ");
    root.ShouldContain("href=\"/Profile\"");
    root.ShouldContain("data-qa=\"ShellUserCard\"");
    html.ShouldContain("Ada Lovelace");
    html.ShouldNotContain("title=\"GitHub\"");
  }

  public static async Task Show_The_Profile_Alias_When_It_Arrives_After_The_First_Render()
  {
    ClaimsPrincipal user = new(new ClaimsIdentity(
      [new Claim(ClaimTypes.Name, "Ada Lovelace")],
      authenticationType: "Test"));

    await using ServiceProvider provider = StaticRenderHost.CreateServices(new BrandOptions(), user).BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    HtmlRootComponent root = await renderer.Dispatcher.InvokeAsync(() =>
      renderer.RenderComponentAsync<ShellHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
      {
        ["PageParameters"] = new Dictionary<string, object>
        {
          ["Title"] = "Staff",
          ["ChildContent"] = Text("Page body")
        }
      })));

    string first = WebUtility.HtmlDecode(await renderer.Dispatcher.InvokeAsync(root.ToHtmlString));
    first.ShouldContain("Ada Lovelace");
    first.ShouldNotContain("Countess");

    ProfileState profile = provider.GetRequiredService<IStore>().GetState<ProfileState>();
    SetProperty(profile, nameof(ProfileState.Alias), "Countess");
    SetProperty(profile, nameof(ProfileState.Email), "ada@example.test");
    await renderer.Dispatcher.InvokeAsync(() =>
      provider.GetRequiredService<Subscriptions>().ReRenderSubscribers<ProfileState>());

    string second = "";
    DateTime deadline = DateTime.UtcNow.AddSeconds(2);
    while (DateTime.UtcNow < deadline)
    {
      second = WebUtility.HtmlDecode(await renderer.Dispatcher.InvokeAsync(root.ToHtmlString));
      if (second.Contains("Countess", StringComparison.Ordinal))
      {
        break;
      }

      await Task.Delay(20);
    }

    second.ShouldContain("Countess");
    second.ShouldContain("ada@example.test");
    second.ShouldNotContain("Ada Lovelace");
  }

  public static async Task Omit_The_User_Card_When_ShowUserCard_Is_False()
  {
    ClaimsPrincipal user = new(new ClaimsIdentity(
      [new Claim(ClaimTypes.Name, "Ada Lovelace")],
      authenticationType: "Test"));

    string html = await RenderAsync
    (
      new Dictionary<string, object>
      {
        ["Title"] = "Staff",
        ["ShowUserCard"] = false,
        ["ChildContent"] = Text("Page body")
      },
      user: user
    );

    html.ShouldNotContain("twe-user-card");
    html.ShouldNotContain("data-qa=\"ShellUserCard\"");
    html.ShouldNotContain("Ada Lovelace");
  }

  private static void SetProperty(object target, string name, object? value)
  {
    PropertyInfo? property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    property.ShouldNotBeNull(name);
    property.SetValue(target, value);
  }

  private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

  private static RenderFragment Mark(string dataQa, string text) => builder =>
  {
    builder.OpenElement(0, "span");
    builder.AddAttribute(1, "data-qa", dataQa);
    builder.AddContent(2, text);
    builder.CloseElement();
  };

  private static async Task<string> RenderAsync
  (
    Dictionary<string, object> parameters,
    BrandOptions? brand = null,
    ClaimsPrincipal? user = null
  )
  {
    await using ServiceProvider provider = StaticRenderHost.CreateServices(brand ?? new BrandOptions(), user).BuildServiceProvider();
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

  private static async Task<string> RenderFocusedAsync(BrandOptions brand)
  {
    await using ServiceProvider provider = StaticRenderHost.CreateServices(brand).BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<TimeWarpFocusedPage>(ParameterView.Empty)).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static string UserCardTag(string html)
  {
    int classAt = html.IndexOf("twe-user-card", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "shell rendered no user card");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
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
}
