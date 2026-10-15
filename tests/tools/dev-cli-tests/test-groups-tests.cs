#region Purpose
// Guards `dev test --group`: every test project under tests/ belongs to exactly one CI test group.
#endregion

namespace TestGroups_;

public class GroupsOf_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GroupsOf_Given_>();

  public static Task EveryTestProject_Should_BelongToExactlyOneGroup()
  {
    string[] misfiled = RepoTestProjects()
      .Where(project => TestGroups.GroupsOf(project).Length != 1)
      .ToArray();

    misfiled.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task EveryGroup_Should_HaveAProjectInTheMonorepo()
  {
    string[] used = RepoTestProjects()
      .SelectMany(TestGroups.GroupsOf)
      .Distinct()
      .ToArray();

    used.ShouldBe(TestGroups.Names, ignoreOrder: true);
    return Task.CompletedTask;
  }

  public static Task ConventionalPaths_Should_MapToTheirGroup()
  {
    TestGroups.GroupsOf("tests/analyzers/timewarp-architecture-analyzers-tests/timewarp-architecture-analyzers-tests.csproj")
      .ShouldBe([TestGroups.Fast]);
    TestGroups.GroupsOf("tests/container-apps/web/web-contracts-tests/web-contracts-tests.csproj")
      .ShouldBe([TestGroups.Fast]);
    TestGroups.GroupsOf("tests/container-apps/web/web-domain-tests/web-domain-tests.csproj")
      .ShouldBe([TestGroups.Fast]);
    TestGroups.GroupsOf("tests/container-apps/web/web-spa-playwright-tests/web-spa-playwright-tests.csproj")
      .ShouldBe([TestGroups.Playwright]);
    TestGroups.GroupsOf("tests/container-apps/aspire/aspire-tests/aspire-tests.csproj")
      .ShouldBe([TestGroups.Integration]);
    TestGroups.GroupsOf(@"tests\container-apps\web\web-infrastructure-tests\web-infrastructure-tests.csproj")
      .ShouldBe([TestGroups.Integration]);
    TestGroups.GroupsOf("tests/container-apps/api/api-jaribu-tests/api-jaribu-tests.csproj")
      .ShouldBe([TestGroups.Integration]);
    return Task.CompletedTask;
  }

  public static Task PlaywrightOutsideContainerApps_Should_MatchTwoGroups()
  {
    TestGroups.GroupsOf("tests/tools/odd-playwright-tests/odd-playwright-tests.csproj").Length.ShouldBe(2);
    return Task.CompletedTask;
  }

  // Repo-relative paths of every project `dev test` would run (the tests/ glob, minus bin/obj).
  private static string[] RepoTestProjects()
  {
    string? root = AppContext.BaseDirectory;
    while (root is not null && !File.Exists(Path.Combine(root, "timewarp-architecture.slnx")))
    {
      root = Path.GetDirectoryName(root);
    }

    root.ShouldNotBeNull();
    return Directory
      .GetFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories)
      .Select(project => Path.GetRelativePath(root, project).Replace('\\', '/'))
      .Where(project => !project.Contains("/bin/", StringComparison.Ordinal) && !project.Contains("/obj/", StringComparison.Ordinal))
      .ToArray();
  }
}
