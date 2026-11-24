#region Purpose
// Pins `dev test --project` name matching: exact stem first, else one unique substring of the stem
// or repo-relative path; no match and several matches fail with the names to pass instead.
#endregion

namespace TestProjectSelection_;

public class Select_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Select_Given_>();

  private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo"));
  private static readonly string DevCli = Project("tests/tools/dev-cli-tests/dev-cli-tests.csproj");
  private static readonly string WebDomain = Project("tests/container-apps/web/web-domain-tests/web-domain-tests.csproj");
  private static readonly string WebServer = Project("tests/container-apps/web/web-server-integration-tests/web-server-integration-tests.csproj");
  private static readonly string WebJaribu = Project("tests/container-apps/web/web-jaribu-tests/web-jaribu-tests.csproj");
  private static readonly string Identity = Project("tests/libraries/timewarp-identity-tests/timewarp-identity-tests.csproj");
  private static readonly string Identity2 = Project("tests/libraries/timewarp-identity-tests-extra/timewarp-identity-tests-extra.csproj");
  private static readonly string[] Projects = [DevCli, WebDomain, WebServer, WebJaribu, Identity, Identity2];

  public static Task ExactStem_Should_SelectThatProject()
  {
    TestProjectSelectionResult result = TestProjectSelection.Select(Projects, Root, ["dev-cli-tests"]);

    result.Error.ShouldBeNull();
    result.Projects.ShouldBe([DevCli]);
    return Task.CompletedTask;
  }

  public static Task ExactStem_Should_WinOverLongerNamesThatContainIt()
  {
    TestProjectSelectionResult result = TestProjectSelection.Select(Projects, Root, ["TimeWarp-Identity-Tests.csproj"]);

    result.Error.ShouldBeNull();
    result.Projects.ShouldBe([Identity]);
    return Task.CompletedTask;
  }

  public static Task UniqueSubstring_Should_SelectThatProject()
  {
    TestProjectSelectionResult result = TestProjectSelection.Select(Projects, Root, ["server-integration"]);

    result.Error.ShouldBeNull();
    result.Projects.ShouldBe([WebServer]);
    return Task.CompletedTask;
  }

  public static Task RelativePathSubstring_Should_SelectThatProject()
  {
    TestProjectSelectionResult result = TestProjectSelection.Select(Projects, Root, [@"tools\dev-cli"]);

    result.Error.ShouldBeNull();
    result.Projects.ShouldBe([DevCli]);
    return Task.CompletedTask;
  }

  public static Task SeveralNames_Should_SelectEachInDiscoveryOrder()
  {
    string[] names = TestProjectSelection.Names(["web-jaribu-tests, dev-cli-tests", "web-domain", "dev-cli-tests"]);
    TestProjectSelectionResult result = TestProjectSelection.Select(Projects, Root, names);

    names.ShouldBe(["web-jaribu-tests", "dev-cli-tests", "web-domain"]);
    result.Error.ShouldBeNull();
    result.Projects.ShouldBe([DevCli, WebDomain, WebJaribu]);
    return Task.CompletedTask;
  }

  public static Task UnknownName_Should_FailNamingEveryTestProject()
  {
    TestProjectSelectionResult result = TestProjectSelection.Select(Projects, Root, ["no-such-tests"]);

    result.Projects.ShouldBeEmpty();
    result.Error.ShouldNotBeNull();
    result.Error.ShouldContain("'no-such-tests' matches no test project");
    result.Error.ShouldContain("dev-cli-tests");
    result.Error.ShouldContain("web-server-integration-tests");
    return Task.CompletedTask;
  }

  public static Task AmbiguousSubstring_Should_FailNamingTheCandidates()
  {
    TestProjectSelectionResult result = TestProjectSelection.Select(Projects, Root, ["web-"]);

    result.Projects.ShouldBeEmpty();
    result.Error.ShouldNotBeNull();
    result.Error.ShouldContain("matches several test projects: web-domain-tests, web-server-integration-tests, web-jaribu-tests");
    result.Error.ShouldNotContain("dev-cli-tests");
    return Task.CompletedTask;
  }

  private static string Project(string relativePath) => Path.Combine(Root, relativePath);
}
