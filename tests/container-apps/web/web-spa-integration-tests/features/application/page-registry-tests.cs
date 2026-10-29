#region Purpose
// Verifies the generated web-spa PageRegistry lists the expected navigation destinations and only those.
#endregion

#region Design
// Task 239-001: PageRegistry is the single destination source for NavMenu and the Ctrl-K palette.
// NavMenu renders the registry itself (NavigationDestinations), so this suite pins the registry contents themselves (including that every INavigationDestination type
// is registered, closing the hand-implemented-marker gap): the unconditional demo/product pages are present,
// parameterized and auth-ceremony pages are absent, and every entry is a well-formed static
// destination. Names are compared as strings so the test needs no per-slice usings.
// ContainsChildPath opens a category for a path under an item Url without making that item active.
#endregion

namespace PageRegistry_;

using Microsoft.AspNetCore.Components.Routing;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Common.Interfaces;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Applications;

[TestTag("Unit")]
public class All_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<All_Should_>();

  private static string[] PageNames() => [.. PageRegistry.All.Select(static e => e.PageType.Name)];

  public static Task List_Expected_Destinations()
  {
    string[] names = PageNames();
    foreach (string expected in new[]
    {
      "HomePage", "SettingsPage", "AgentLinksPage", "ProfilePage",
      "RolesListPage", "PrincipalsPage", "FeedbackAdminListPage", "AuthenticationPage",
      "CounterPage", "ChatPage", "PasskeysPage",
      "DesignSystemPage", "TestPage", "ServicesPage", "ClaimsPage",
    })
    {
      names.ShouldContain(expected);
    }

    PageRegistry.All.Single(static e => e.PageType.Name == "CounterPage").RouteTemplate.ShouldBe("/Counter");
    return Task.CompletedTask;
  }

  public static Task Exclude_Parameterized_And_Ceremony_Pages()
  {
    string[] names = PageNames();
    foreach (string excluded in new[]
    {
      "TodoItemPage", "RoleDetailPage", "FeedbackAdminItemPage", "LoginPage", "LogoutPage", "ChooseMicrosoft365Page",
    })
    {
      names.ShouldNotContain(excluded);
    }

    return Task.CompletedTask;
  }

  public static Task Carry_Well_Formed_Static_Entries()
  {
    PageRegistry.All.ShouldNotBeEmpty();
    PageNames().ShouldBeUnique();
    foreach (PageRegistryEntry entry in PageRegistry.All)
    {
      entry.Url.ShouldBe(entry.RouteTemplate, entry.PageType.Name);
      entry.RouteTemplate.ShouldNotContain("{", Case.Sensitive, entry.PageType.Name);
      entry.Title.ShouldNotBeNullOrWhiteSpace(entry.PageType.Name);
      entry.Description.ShouldNotBeNullOrWhiteSpace(entry.PageType.Name);
      entry.Policy.ShouldNotBeNullOrWhiteSpace(entry.PageType.Name);
      typeof(INavigationDestination).IsAssignableFrom(entry.PageType).ShouldBeTrue(entry.PageType.Name);
    }

    PageRegistry.All.Single(static e => e.PageType.Name == "SettingsPage").Policy.ShouldBe(PermissionIds.SettingsRead);
    return Task.CompletedTask;
  }

  public static Task Keep_The_Design_System_On_Its_Static_Route_Only()
  {
    PageRegistryEntry designSystem = PageRegistry.All.Single(static entry => entry.PageType.Name == "DesignSystemPage");
    designSystem.RouteTemplate.ShouldBe("/DesignSystem");
    designSystem.Url.ShouldBe("/DesignSystem");
    designSystem.RouteTemplates.ShouldBe(["/DesignSystem", "/DesignSystem/{Part}"]);
    PageRegistry.All.ShouldNotContain(static entry =>
      entry.Url.StartsWith("/DesignSystem/", StringComparison.OrdinalIgnoreCase)
      || entry.RouteTemplate.Contains("{Part}", StringComparison.Ordinal));
    return Task.CompletedTask;
  }

  public static Task Match_Prefix_Only_When_A_Template_Has_A_Parameter()
  {
    PageRegistryEntry designSystem = PageRegistry.All.Single(static entry => entry.PageType.Name == "DesignSystemPage");
    PageRegistryEntry roles = PageRegistry.All.Single(static entry => entry.PageType.Name == "RolesListPage");
    PageRegistryEntry home = PageRegistry.All.Single(static entry => entry.PageType.Name == "HomePage");
    NavigationMatch.For(designSystem).ShouldBe(NavLinkMatch.Prefix);
    NavigationMatch.For(roles).ShouldBe(NavLinkMatch.All);
    NavigationMatch.For(home).ShouldBe(NavLinkMatch.All);
    home.RouteTemplate.ShouldBe("/");
    roles.RouteTemplates.ShouldBe(["/Admin/Roles"]);
    // Role detail is its own non-navigable page, so the Roles list item stays an exact match.
    PageRegistry.All.ShouldAllBe(entry =>
      entry.PageType.Name == "DesignSystemPage"
      || entry.RouteTemplates.All(template => !template.Contains('{', StringComparison.Ordinal)));
    return Task.CompletedTask;
  }

  public static Task Open_A_Group_On_A_Child_Of_An_Item_Without_Activating_It()
  {
    PageRegistryEntry roles = PageRegistry.All.Single(static entry => entry.Url == "/Admin/Roles");
    PageRegistryEntry principals = PageRegistry.All.Single(static entry => entry.Url == "/Admin/Principals");
    PageRegistryEntry feedback = PageRegistry.All.Single(static entry => entry.Url == "/Admin/Feedback");
    PageRegistryEntry home = PageRegistry.All.Single(static entry => entry.Url == "/");
    PageRegistryEntry[] admin = [roles, principals, feedback];
    const string roleId = "11111111-1111-1111-1111-111111111111";

    NavigationMatch.For(roles).ShouldBe(NavLinkMatch.All);
    NavigationMatch.ContainsChildPath(admin, "/Admin/Roles/" + roleId).ShouldBeTrue();
    NavigationMatch.ContainsChildPath(admin, "/Admin/Roles/New").ShouldBeTrue();
    NavigationMatch.ContainsChildPath(admin, "/Admin/Principals/" + roleId).ShouldBeTrue();
    NavigationMatch.ContainsChildPath(admin, "/Admin/Feedback/" + roleId).ShouldBeTrue();
    NavigationMatch.ContainsChildPath(admin, "/Admin/Roles").ShouldBeFalse();
    NavigationMatch.ContainsChildPath(admin, "/Admin/RolesExtra").ShouldBeFalse();
    NavigationMatch.ContainsChildPath([home], "/Admin/Roles/" + roleId).ShouldBeFalse();
    NavigationMatch.ContainsChildPath(admin, "/admin/roles/" + roleId + "?x=1").ShouldBeTrue();
    NavigationMatch.ContainsChildPath(admin, "/ADMIN/ROLES/NEW").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Include_Every_Navigation_Destination()
  {
    // Reverse direction: a hand-written INavigationDestination without Navigable = true would
    // claim to be a destination yet be missing from the registry.
    Type[] registered = [.. PageRegistry.All.Select(static e => e.PageType)];
    foreach (Type destination in typeof(PageRegistry).Assembly.GetTypes()
      .Where(static t => t is { IsClass: true, IsAbstract: false } && typeof(INavigationDestination).IsAssignableFrom(t)))
    {
      registered.ShouldContain(destination, destination.Name);
    }

    return Task.CompletedTask;
  }
}
