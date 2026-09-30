#region Purpose
// Verifies the generated web-spa PageRegistry lists the expected navigation destinations and only those.
#endregion

#region Design
// Task 239-001: PageRegistry is the single destination source for NavMenu and the Ctrl-K palette.
// NavMenu drift is a compile error (TimeWarpNavLink requires INavigationDestination), so this
// suite pins the registry contents themselves (including that every INavigationDestination type
// is registered, closing the hand-implemented-marker gap): the unconditional demo/product pages are present,
// parameterized and auth-ceremony pages are absent, and every entry is a well-formed static
// destination. Names are compared as strings so the test needs no per-slice usings.
#endregion

namespace PageRegistry_;

using TimeWarp.Architecture;
using TimeWarp.Architecture.Common.Interfaces;
using TimeWarp.Architecture.Features;

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
      "RolesListPage", "PrincipalsPage", "AuthenticationPage",
      "CounterPage", "ChatPage", "PasskeysPage",
      "StyleGuidePage", "TestPage", "ServicesPage", "UserClaimsPage",
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
      "TodoItemPage", "RoleDetailPage", "LoginPage", "LogoutPage", "ChooseMicrosoft365Page",
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
      entry.Policy.ShouldNotBeNullOrWhiteSpace(entry.PageType.Name);
      typeof(INavigationDestination).IsAssignableFrom(entry.PageType).ShouldBeTrue(entry.PageType.Name);
    }

    PageRegistry.All.Single(static e => e.PageType.Name == "SettingsPage").Policy.ShouldBe(PermissionIds.SettingsRead);
    return Task.CompletedTask;
  }

  public static Task Include_Every_Navigation_Destination()
  {
    // Reverse direction: a hand-written INavigationDestination without Navigable = true would
    // satisfy TimeWarpNavLink yet be missing from the registry.
    Type[] registered = [.. PageRegistry.All.Select(static e => e.PageType)];
    foreach (Type destination in typeof(PageRegistry).Assembly.GetTypes()
      .Where(static t => t is { IsClass: true, IsAbstract: false } && typeof(INavigationDestination).IsAssignableFrom(t)))
    {
      registered.ShouldContain(destination, destination.Name);
    }

    return Task.CompletedTask;
  }
}
