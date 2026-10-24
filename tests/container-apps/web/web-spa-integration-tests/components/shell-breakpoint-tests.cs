#region Purpose
// Pin the shell nav widths that CSS media queries cannot read from tokens.
#endregion

#region Design
// 240px is the --twe-shell-nav-width token. The navigation item's Width is that
// variable. 68px is the icon rail. 880px is MobileBreakdownWidth (the drawer).
// 1279px is the last rail width. Those numbers are repeated in tokens.css and the
// shell styles. This test reads the files; HtmlRenderer does not size the rail.
#endregion

namespace ShellBreakpoint_;

[TestTag("Unit")]
public class ShellBreakpoints_Should_
{
  private const string MediaQuery = "@media (max-width: 1279px) and (min-width: 880px)";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ShellBreakpoints_Should_>();

  public static Task Keep_The_Nav_Rail_And_Drawer_Numbers_Together()
  {
    string root = RepoRoot();
    string tokens = File.ReadAllText(Path.Combine(root, "source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css"));
    tokens.ShouldContain("--twe-shell-nav-width: 240px;");
    tokens.ShouldContain("--twe-shell-rail-width: 68px;");

    string page = File.ReadAllText(Path.Combine(root, "source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor"));
    page.ShouldContain("MobileBreakdownWidth=\"880\"");
    page.ShouldContain("Width=\"var(--twe-shell-nav-width)\"");

    string pageCss = File.ReadAllText(Path.Combine(root, "source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor.css"));
    pageCss.ShouldContain(MediaQuery);
    pageCss.ShouldContain("var(--twe-shell-rail-width)");
    pageCss.ShouldContain("area=nav");

    string menuCss = File.ReadAllText(Path.Combine(root, "source/container-apps/web/projects/web-spa/components/NavMenu.razor.css"));
    menuCss.ShouldContain(MediaQuery);
    menuCss.ShouldContain("overflow-y: auto");
    menuCss.ShouldContain("flex: none");
    menuCss.ShouldContain("overflow: visible");
    menuCss.ShouldContain("clip: rect(0, 0, 0, 0);");

    string cardCss = File.ReadAllText(Path.Combine(root, "source/container-apps/web/projects/web-spa/components/composites/UserCard.razor.css"));
    cardCss.ShouldContain(MediaQuery);
    cardCss.ShouldContain("clip: rect(0, 0, 0, 0);");

    string rail = File.ReadAllText(Path.Combine(root, "source/container-apps/web/projects/web-spa/source/features/shell-rail.ts"));
    rail.ShouldNotContain("export function stop");
    rail.ShouldContain("stop(): void");
    rail.ShouldNotContain("let query");

    string menu = File.ReadAllText(Path.Combine(root, "source/container-apps/web/projects/web-spa/components/NavMenu.razor"));
    menu.ShouldContain("\"watch\"");
    menu.ShouldContain("InvokeVoidAsync(\"stop\")");
    return Task.CompletedTask;
  }

  private static string RepoRoot()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return directory;
  }
}
