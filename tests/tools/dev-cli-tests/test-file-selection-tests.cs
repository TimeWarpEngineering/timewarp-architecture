#region Purpose
// Pins `dev test <file…>` mapping: a runfile runs alone, a csproj test file maps to its project and
// one --filter-class per declared test class, a helper or stray file fails naming the file, and a
// class whose filter also selects another class in its project fails naming both.
#endregion

#region Design
// Files live in an in-memory map under a temp root, so no test touches disk. The playwright project
// and its class layout mirror the real tests/container-apps/web/web-spa-playwright-tests folder.
#endregion

namespace TestFileSelection_;

public class Select_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Select_Given_>();

  private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo"));
  private static readonly string PlaywrightDirectory = Path.Combine(Root, "tests/container-apps/web/web-spa-playwright-tests");
  private static readonly string Playwright = Path.Combine(PlaywrightDirectory, "web-spa-playwright-tests.csproj");
  private static readonly string WebDirectory = Path.Combine(Root, "tests/container-apps/web/web-jaribu-tests");
  private static readonly string Web = Path.Combine(WebDirectory, "web-jaribu-tests.csproj");
  private static readonly string Nested = Path.Combine(PlaywrightDirectory, "nested/nested-tests.csproj");
  private static readonly string[] Projects = [Playwright, Web, Nested];

  private const string DarkTheme =
    """
    namespace DarkThemePlaywright_;

    using DesignSystemCatalogPlaywright_;

    public class DarkTheme_Given_Shell
    {
      [System.Runtime.CompilerServices.ModuleInitializer]
      internal static void Register() => RegisterTests<DarkTheme_Given_Shell>();

      public static async Task StoredDark_Should_Darken_EveryPage_And_Ask() { }
    }
    """;

  private const string TwoClasses =
    """
    namespace ShellNav_;

    // This class checks the nav width.
    public class NavWidth_Given_Shell
    {
      internal static void Register() => RegisterTests<NavWidth_Given_Shell>();
    }

    public sealed class NavCollapse_Given_Shell
    {
      internal static void Register() => RegisterTests<ShellNav_.NavCollapse_Given_Shell>();

      private sealed class Session : IAsyncDisposable { }
    }
    """;

  private const string Helper =
    """
    namespace DrawerPlaywright_;

    internal static class DrawerPageAsserts
    {
    }
    """;

  private const string PartialPart =
    """
    namespace FirstPaint_;

    public partial class FirstPaint_Given_PrerenderedShell
    {
      public static Task Other_Should_Pass() => Task.CompletedTask;
    }
    """;

  private const string PartialRegistration =
    """
    namespace FirstPaint_;

    public partial class FirstPaint_Given_PrerenderedShell
    {
      internal static void Register() => RegisterTests<FirstPaint_Given_PrerenderedShell>();
    }
    """;

  private const string NestedClass =
    """
    namespace Outer_;

    public static class Outer
    {
      public class Inner_Given_
      {
        internal static void Register() => RegisterTests<Inner_Given_>();
      }
    }
    """;

  private const string WithFixtureText =
    """"
    namespace Fixtures_;

    public class Fixtures_Given_
    {
      internal static void Register() => RegisterTests<Fixtures_Given_>();

      private const string Sample =
        """
    public class Fake_Given_
    {
      internal static void Register() => RegisterTests<Fake_Given_>();
    }
    """;
    }
    """";

  // Mirrors foundation-domain-tests: entity-tests.cs `Equality` and enumeration-tests.cs
  // `Equality_Operators` in one namespace (task 523 review M1).
  private const string EntityEquality =
    """
    namespace Domain.Tests;

    public class Equality
    {
      internal static void Register() => RegisterTests<Equality>();
    }
    """;

  private const string EnumerationEquality =
    """
    namespace Domain.Tests;

    public class Equality_Operators
    {
      internal static void Register() => RegisterTests<Equality_Operators>();
    }
    """;

  private static readonly string Runfile =
    $"#!/usr/bin/env -S dotnet --\n  {TestFileSelection.RunfileGuard}  \nreturn await TimeWarp.Jaribu.TestRunner.RunAllTests();\n";

  private static readonly Dictionary<string, string> Files = new(StringComparer.Ordinal)
  {
    [Path.Combine(PlaywrightDirectory, "dark-theme-playwright-tests.cs")] = DarkTheme,
    [Path.Combine(PlaywrightDirectory, "shell-nav-playwright-tests.cs")] = TwoClasses,
    [Path.Combine(PlaywrightDirectory, "drawer-page-asserts.cs")] = Helper,
    [Path.Combine(PlaywrightDirectory, "first-paint-part.cs")] = PartialPart,
    [Path.Combine(PlaywrightDirectory, "first-paint-playwright-tests.cs")] = PartialRegistration,
    [Path.Combine(PlaywrightDirectory, "nested/nested-class-tests.cs")] = NestedClass,
    [Path.Combine(PlaywrightDirectory, "fixture-text-tests.cs")] = WithFixtureText,
    [Path.Combine(Root, "source/container-apps/web/platform/abuse/abuse-rate-limiting-tests.cs")] = Runfile,
    [Path.Combine(Root, "tools/stray.cs")] = DarkTheme,
    [Path.Combine(WebDirectory, "entity-tests.cs")] = EntityEquality,
    [Path.Combine(WebDirectory, "enumeration-tests.cs")] = EnumerationEquality,
  };

  public static Task PlaywrightFile_Should_RunItsProjectFilteredToItsClass()
  {
    TestFileSelectionResult result = Select("tests/container-apps/web/web-spa-playwright-tests/dark-theme-playwright-tests.cs");

    result.Errors.ShouldBeEmpty();
    result.Runfiles.ShouldBeEmpty();
    result.ProjectRuns.Length.ShouldBe(1);
    result.ProjectRuns[0].Project.ShouldBe(Playwright);
    result.ProjectRuns[0].ClassFilters.ShouldBe(["DarkThemePlaywright_.DarkTheme_Given_Shell"]);
    return Task.CompletedTask;
  }

  public static Task FileWithSeveralClasses_Should_FilterEachTestClassButNotOthers()
  {
    TestFileSelectionResult result = Select("tests/container-apps/web/web-spa-playwright-tests/shell-nav-playwright-tests.cs");

    result.Errors.ShouldBeEmpty();
    result.ProjectRuns[0].ClassFilters.ShouldBe(["ShellNav_.NavWidth_Given_Shell", "ShellNav_.NavCollapse_Given_Shell"]);
    return Task.CompletedTask;
  }

  public static Task SeveralFilesInOneProject_Should_MergeIntoOneProjectRun()
  {
    TestFileSelectionResult result = Select(
      "tests/container-apps/web/web-spa-playwright-tests/dark-theme-playwright-tests.cs",
      "tests/container-apps/web/web-spa-playwright-tests/shell-nav-playwright-tests.cs",
      "tests/container-apps/web/web-spa-playwright-tests/dark-theme-playwright-tests.cs");

    result.Errors.ShouldBeEmpty();
    result.ProjectRuns.Length.ShouldBe(1);
    result.ProjectRuns[0].ClassFilters.ShouldBe(
      ["DarkThemePlaywright_.DarkTheme_Given_Shell", "ShellNav_.NavWidth_Given_Shell", "ShellNav_.NavCollapse_Given_Shell"]);
    return Task.CompletedTask;
  }

  public static Task PartialPartWithoutRegistration_Should_StillMapToItsClass()
  {
    TestFileSelectionResult result = Select("tests/container-apps/web/web-spa-playwright-tests/first-paint-part.cs");

    result.Errors.ShouldBeEmpty();
    result.ProjectRuns[0].ClassFilters.ShouldBe(["FirstPaint_.FirstPaint_Given_PrerenderedShell"]);
    return Task.CompletedTask;
  }

  public static Task NestedProject_Should_OwnItsFiles_And_NestedClass_Should_UsePlusFilter()
  {
    TestFileSelectionResult result = Select("tests/container-apps/web/web-spa-playwright-tests/nested/nested-class-tests.cs");

    result.Errors.ShouldBeEmpty();
    result.ProjectRuns[0].Project.ShouldBe(Nested);
    result.ProjectRuns[0].ClassFilters.ShouldBe(["+Inner_Given_"]);
    return Task.CompletedTask;
  }

  public static Task RawStringFixtureText_Should_NotAddClasses()
  {
    TestFileSelectionResult result = Select("tests/container-apps/web/web-spa-playwright-tests/fixture-text-tests.cs");

    result.Errors.ShouldBeEmpty();
    result.ProjectRuns[0].ClassFilters.ShouldBe(["Fixtures_.Fixtures_Given_"]);
    return Task.CompletedTask;
  }

  public static Task ClassWhoseFilterIsInAnotherClassName_Should_FailNamingFileClassAndOtherClass()
  {
    TestFileSelectionResult result = Select("tests/container-apps/web/web-jaribu-tests/entity-tests.cs");

    result.ProjectRuns.ShouldBeEmpty();
    result.Errors.Length.ShouldBe(1);
    result.Errors[0].ShouldContain("tests/container-apps/web/web-jaribu-tests/entity-tests.cs");
    result.Errors[0].ShouldContain("Domain.Tests.Equality cannot run alone");
    result.Errors[0].ShouldContain("also selects Domain.Tests.Equality_Operators in web-jaribu-tests");
    return Task.CompletedTask;
  }

  public static Task LongerClassName_Should_StillRunAlone()
  {
    TestFileSelectionResult result = Select("tests/container-apps/web/web-jaribu-tests/enumeration-tests.cs");

    result.Errors.ShouldBeEmpty();
    result.ProjectRuns[0].Project.ShouldBe(Web);
    result.ProjectRuns[0].ClassFilters.ShouldBe(["Domain.Tests.Equality_Operators"]);
    return Task.CompletedTask;
  }

  public static Task GuardedRunfile_Should_RunAsRunfile()
  {
    TestFileSelectionResult result = Select("source/container-apps/web/platform/abuse/abuse-rate-limiting-tests.cs");

    result.Errors.ShouldBeEmpty();
    result.ProjectRuns.ShouldBeEmpty();
    result.Runfiles.ShouldBe([Path.Combine(Root, "source/container-apps/web/platform/abuse/abuse-rate-limiting-tests.cs")]);
    return Task.CompletedTask;
  }

  public static Task HelperFile_Should_FailNamingTheFileAndAskForTheTestClassFile()
  {
    TestFileSelectionResult result = Select("tests/container-apps/web/web-spa-playwright-tests/drawer-page-asserts.cs");

    result.ProjectRuns.ShouldBeEmpty();
    result.Errors.Length.ShouldBe(1);
    result.Errors[0].ShouldContain("tests/container-apps/web/web-spa-playwright-tests/drawer-page-asserts.cs declares no test class");
    result.Errors[0].ShouldContain("pass the test class file");
    return Task.CompletedTask;
  }

  public static Task FileInNoProject_Should_FailNamingTheFile()
  {
    TestFileSelectionResult result = Select("tools/stray.cs");

    result.Errors.Length.ShouldBe(1);
    result.Errors[0].ShouldContain("tools/stray.cs is in no test project under tests/ and is not a Jaribu runfile");
    return Task.CompletedTask;
  }

  public static Task MissingFile_Should_FailNamingTheFile()
  {
    TestFileSelectionResult result = Select("tests/no-such-tests.cs", "tools/stray.cs");

    result.Errors.Length.ShouldBe(2);
    result.Errors[0].ShouldBe("tests/no-such-tests.cs does not exist.");
    return Task.CompletedTask;
  }

  public static Task RelativeArgument_Should_ResolveAgainstTheWorkingDirectory()
  {
    TestFileSelectionResult result = TestFileSelection.Select(
      ["dark-theme-playwright-tests.cs"], PlaywrightDirectory, Root, Projects, Read, Sources);

    result.Errors.ShouldBeEmpty();
    result.ProjectRuns[0].Project.ShouldBe(Playwright);
    return Task.CompletedTask;
  }

  private static TestFileSelectionResult Select(params string[] files) =>
    TestFileSelection.Select(files, Root, Root, Projects, Read, Sources);

  private static string? Read(string path) => Files.GetValueOrDefault(path);

  // Like the dev CLI: every file under the project folder, nested project folders included.
  private static IEnumerable<string> Sources(string projectDirectory) =>
    Files.Where(file => file.Key.StartsWith(projectDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal)).Select(file => file.Value);
}
