#region Purpose
// Parent resolution for the location breadcrumb: longest literal prefix, a parameterized child, and a page with no parent.
#endregion

#region Design
// Parents are PageRegistry route templates, walked at run time. These cases use a small route
// list so a longer lookalike (/Admin/RolesNew) cannot win, then the generated registry so
// /StyleGuide/{Part} and /Admin/Roles/{id} resolve to the pages the shell actually links.
#endregion

namespace PageBreadcrumbTrail_;

using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;

[TestTag("Unit")]
public class PageBreadcrumbTrail_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PageBreadcrumbTrail_Should_>();

  public static Task Resolve_The_Longest_Literal_Prefix()
  {
    string[] routes = ["/", "/Admin", "/Admin/Roles", "/Admin/RolesNew", "/Other"];

    PageBreadcrumbTrail.ParentRoute("/Admin/Roles/Extra", routes).ShouldBe("/Admin/Roles");
    PageBreadcrumbTrail.ParentRoute("/Admin/RolesNew", routes).ShouldBe("/Admin");
    return Task.CompletedTask;
  }

  public static Task Resolve_A_Parameterized_Child()
  {
    string[] routes = ["/", "/StyleGuide", "/Admin/Roles"];

    PageBreadcrumbTrail.ParentRoute("/StyleGuide/{Part}", routes).ShouldBe("/StyleGuide");
    PageBreadcrumbTrail.ParentRoute("/Admin/Roles/{RoleId:Guid}", routes).ShouldBe("/Admin/Roles");
    PageBreadcrumbTrail.ParentRoute("/StyleGuide/color-tokens", routes).ShouldBe("/StyleGuide");
    return Task.CompletedTask;
  }

  public static Task Resolve_A_Page_With_No_Parent()
  {
    PageBreadcrumbTrail.ParentRoute("/Counter", ["/Counter", "/Admin/Roles"]).ShouldBeNull();
    PageBreadcrumbTrail.ParentRoute("/", ["/", "/Counter"]).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Name_Style_Guide_Parts_And_The_Role_Editor()
  {
    string[] routes = [.. PageRegistry.All.Select(static entry => entry.RouteTemplate)];
    PageBreadcrumbTrail.ParentRoute("/StyleGuide/color-tokens", routes).ShouldBe("/StyleGuide");
    PageBreadcrumbTrail.ParentRoute("/Admin/Roles/" + RoleIds.Administrator, routes).ShouldBe("/Admin/Roles");
    PageBreadcrumbTrail.ParentRoute("/", routes).ShouldBeNull();

    IReadOnlyList<PageBreadcrumbTrail.Crumb> part = PageBreadcrumbTrail.Build
    (
      "/StyleGuide/color-tokens",
      "Color Tokens",
      "Style Guide",
      PageRegistry.All
    );
    Labels(part).ShouldBe(["Home", "Style Guide", "Color Tokens"]);
    part[0].Href.ShouldBe("/");
    part[1].Href.ShouldBe("/StyleGuide");
    part[2].Href.ShouldBeNull();

    IReadOnlyList<PageBreadcrumbTrail.Crumb> role = PageBreadcrumbTrail.Build
    (
      "/Admin/Roles/" + RoleIds.Administrator,
      null,
      "Administrator",
      PageRegistry.All
    );
    Labels(role).ShouldBe(["Home", "Roles", "Administrator"]);
    role[1].Href.ShouldBe("/Admin/Roles");
    role[2].Href.ShouldBeNull();

    IReadOnlyList<PageBreadcrumbTrail.Crumb> home = PageBreadcrumbTrail.Build("/", null, "TimeWarp", PageRegistry.All);
    Labels(home).ShouldBe(["Home"]);

    IReadOnlyList<PageBreadcrumbTrail.Crumb> counter = PageBreadcrumbTrail.Build("/Counter", null, "Counter", PageRegistry.All);
    Labels(counter).ShouldBe(["Home", "Counter"]);
    return Task.CompletedTask;
  }

  private static string[] Labels(IReadOnlyList<PageBreadcrumbTrail.Crumb> crumbs) =>
    [.. crumbs.Select(static crumb => crumb.Label)];
}
