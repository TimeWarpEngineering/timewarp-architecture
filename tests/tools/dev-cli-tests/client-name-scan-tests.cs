#region Purpose
// Pins the client-name scan to packed trees, the unpacked name list, and the CI jobs that run it.
#endregion

namespace ClientNameScan_;

public class FindHits_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FindHits_Given_>();

  public static Task SourceFile_Should_ReportTheToken()
  {
    string root = Directory.CreateTempSubdirectory("client-name-scan-").FullName;
    try
    {
      string source = Path.Combine(root, "source");
      Directory.CreateDirectory(source);
      File.WriteAllText(Path.Combine(source, "sample.cs"), "prefix ACMECLIENT suffix\nsecond\n");
      IReadOnlyList<ClientNameScan.Hit> hits = ClientNameScan.FindHits(root, ["source"], ["acmeclient"]);
      hits.Count.ShouldBe(1);
      hits[0].RelativePath.ShouldBe("source/sample.cs");
      hits[0].Line.ShouldBe(1);
      hits[0].Name.ShouldBe("acmeclient");
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task AnalysisDirectory_Should_BeSkipped()
  {
    string root = Directory.CreateTempSubdirectory("client-name-scan-").FullName;
    try
    {
      string analysis = Path.Combine(root, "skills", "tw-sample", "analysis");
      Directory.CreateDirectory(analysis);
      File.WriteAllText(Path.Combine(analysis, "note.md"), "acmeclient\n");
      File.WriteAllText(Path.Combine(root, "skills", "tw-sample", "SKILL.md"), "clean\n");
      IReadOnlyList<ClientNameScan.Hit> hits = ClientNameScan.FindHits(
        root,
        ["skills"],
        ["acmeclient"]);
      hits.ShouldBeEmpty();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task SkillFile_Should_ReportTheToken()
  {
    string root = Directory.CreateTempSubdirectory("client-name-scan-").FullName;
    try
    {
      string skill = Path.Combine(root, "skills", "tw-sample");
      Directory.CreateDirectory(skill);
      File.WriteAllText(Path.Combine(skill, "SKILL.md"), "see acmeclient\n");
      IReadOnlyList<ClientNameScan.Hit> hits = ClientNameScan.FindHits(root, ["skills"], ["acmeclient"]);
      hits.Count.ShouldBe(1);
      hits[0].RelativePath.ShouldBe("skills/tw-sample/SKILL.md");
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task BinaryFile_Should_BeSkipped()
  {
    string root = Directory.CreateTempSubdirectory("client-name-scan-").FullName;
    try
    {
      string source = Path.Combine(root, "source");
      Directory.CreateDirectory(source);
      File.WriteAllBytes(
        Path.Combine(source, "blob.bin"),
        [.. "acmeclient"u8, 0x00]);
      IReadOnlyList<ClientNameScan.Hit> hits = ClientNameScan.FindHits(root, ["source"], ["acmeclient"]);
      hits.ShouldBeEmpty();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }
}

public class Unpack_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Unpack_Given_>();

  public static Task DocumentationExclude_Should_CoverTheNamesFile()
  {
    ClientNameScan.GlobCovers("documentation/**", ClientNameScan.NamesFileRelativePath).ShouldBeTrue();
    ClientNameScan.GlobCovers("skills/**/analysis/**", "skills/tw-sample/analysis/note.md").ShouldBeTrue();
    ClientNameScan.GlobCovers("skills/**/analysis/**", "skills/tw-sample/SKILL.md").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task RealPack_Should_LeaveTheNamesFileOut()
  {
    string root = ClientNameScanRepo.Root();
    string template = File.ReadAllText(Path.Combine(root, ClientNameScan.TemplateJsonRelativePath));
    string project = File.ReadAllText(Path.Combine(root, ClientNameScan.TemplateProjectRelativePath));
    ClientNameScan.UnpackedError(template, project).ShouldBeNull();
    ClientNameScan.PackIncludesRepoPath(project, ClientNameScan.ToolsProbeRelativePath).ShouldBeFalse();
    ClientNameScan.PackIncludesRepoPath(project, "source/container-apps/web/projects/web-spa/wwwroot/auth.md")
      .ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task RealPack_Should_CoverAllowListPaths()
  {
    string root = ClientNameScanRepo.Root();
    string project = File.ReadAllText(Path.Combine(root, ClientNameScan.TemplateProjectRelativePath));
    ClientNameScan.TryListPackedFiles(root, project, out IReadOnlyList<string> files, out string? error)
      .ShouldBeTrue();
    error.ShouldBeNull();
    files.ShouldContain("readme.md");
    files.ShouldContain("Directory.Build.props");
    files.ShouldContain("timewarp-architecture.slnx");
    files.ShouldContain(".template.config/template.json");
    files.ShouldContain("kanban/overview.md");
    files.ShouldContain("kanban/task-template.md");
    files.ShouldContain("kanban/to-do/.gitkeep");
    files.ShouldContain("kanban/done/.gitkeep");
    files.ShouldContain(path => path.StartsWith("msbuild/", StringComparison.Ordinal));
    files.ShouldNotContain(ClientNameScan.NamesFileRelativePath);
    files.ShouldNotContain(ClientNameScan.ToolsProbeRelativePath);
    files.ShouldNotContain(path =>
      path.StartsWith("kanban/", StringComparison.Ordinal)
      && path != "kanban/archived/.gitkeep"
      && path != "kanban/backlog/.gitkeep"
      && path != "kanban/done/.gitkeep"
      && path != "kanban/in-progress/.gitkeep"
      && path != "kanban/overview.md"
      && path != "kanban/task-template.md"
      && path != "kanban/to-do/.gitkeep");
    files.ShouldNotContain(path => path.StartsWith("documentation/", StringComparison.Ordinal));
    foreach (string path in files)
    {
      string[] parts = path.Split('/');
      if (parts[0] == "skills")
      {
        parts.ShouldNotContain("analysis");
      }
    }

    return Task.CompletedTask;
  }

  public static Task ToolsInclude_Should_CountAsPacked()
  {
    const string project = """
      <Project>
        <ItemGroup>
          <Content Include="../../../tools/**/*" />
        </ItemGroup>
      </Project>
      """;
    ClientNameScan.PackIncludesRepoPath(project, ClientNameScan.ToolsProbeRelativePath).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task MissingExclude_Should_Fail()
  {
    const string template = """
      { "sources": [ { "modifiers": [ { "condition": "(true)", "exclude": [ "**/bin/**" ] } ] } ] }
      """;
    const string project = """
      <Project>
        <ItemGroup>
          <Content Include="../../../source/**/*" />
        </ItemGroup>
      </Project>
      """;
    string? error = ClientNameScan.UnpackedError(template, project);
    error.ShouldNotBeNull();
    error.ShouldContain(ClientNameScan.NamesFileRelativePath);
    return Task.CompletedTask;
  }
}

public class PackedFiles_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PackedFiles_Given_>();

  public static Task AllowList_Should_ListIncludedFilesAndSkipTheRest()
  {
    string root = Directory.CreateTempSubdirectory("client-name-scan-").FullName;
    try
    {
      Directory.CreateDirectory(Path.Combine(root, "msbuild"));
      File.WriteAllText(Path.Combine(root, "msbuild", "pack.props"), "acmeclient\n");
      Directory.CreateDirectory(Path.Combine(root, "skills", "tw-sample", "analysis"));
      File.WriteAllText(Path.Combine(root, "skills", "tw-sample", "SKILL.md"), "clean\n");
      File.WriteAllText(Path.Combine(root, "skills", "tw-sample", "analysis", "note.md"), "acmeclient\n");
      File.WriteAllText(Path.Combine(root, "readme.md"), "clean\n");
      File.WriteAllText(Path.Combine(root, "secret.md"), "acmeclient\n");
      File.WriteAllText(Path.Combine(root, "icon.txt"), "clean\n");
      Directory.CreateDirectory(Path.Combine(root, "kanban", "done"));
      File.WriteAllText(Path.Combine(root, "kanban", "overview.md"), "clean\n");
      File.WriteAllText(Path.Combine(root, "kanban", "done", "task.md"), "acmeclient\n");
      Directory.CreateDirectory(Path.Combine(root, "source", "bin"));
      File.WriteAllText(Path.Combine(root, "source", "app.cs"), "clean\n");
      File.WriteAllText(Path.Combine(root, "source", "bin", "app.cs"), "acmeclient\n");

      const string project = """
        <Project>
          <ItemGroup>
            <Content Include="../../../msbuild/**/*" />
            <Content Include="../../../source/**/*" Exclude="../../../source/**/bin/**" />
            <Content Include="../../../skills/**/*" Exclude="../../../skills/**/analysis/**" />
            <Content Include="../../../kanban/overview.md" />
            <Content Include="../../../left-out.md" Pack="false" />
            <None Include="../../../readme.md">
              <Pack>True</Pack>
            </None>
            <None Include="../../../icon.txt" Pack="true" />
            <None Include="../../../secret.md" />
            <Compile Remove="**\*" />
          </ItemGroup>
        </Project>
        """;

      ClientNameScan.TryListPackedFiles(root, project, out IReadOnlyList<string> files, out string? error)
        .ShouldBeTrue();
      error.ShouldBeNull();
      files.ShouldBe(
      [
        "icon.txt",
        "kanban/overview.md",
        "msbuild/pack.props",
        "readme.md",
        "skills/tw-sample/SKILL.md",
        "source/app.cs"
      ]);

      IReadOnlyList<ClientNameScan.Hit> hits = ClientNameScan.FindHitsInFiles(root, files, ["acmeclient"]);
      hits.Count.ShouldBe(1);
      hits[0].RelativePath.ShouldBe("msbuild/pack.props");
      hits[0].Line.ShouldBe(1);
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task PropertyInclude_Should_Fail()
  {
    const string project = """
      <Project>
        <ItemGroup>
          <Content Include="../../../$(Extra)/**/*" />
        </ItemGroup>
      </Project>
      """;
    string root = Directory.CreateTempSubdirectory("client-name-scan-").FullName;
    try
    {
      ClientNameScan.TryListPackedFiles(root, project, out _, out string? error).ShouldBeFalse();
      error.ShouldNotBeNull();
      error.ShouldContain("$(Extra)");
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }
}

public class Names_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Names_Given_>();

  public static Task RepoList_Should_Load()
  {
    ClientNameScan.TryReadNames(ClientNameScanRepo.Root(), out IReadOnlyList<string> names, out string? error)
      .ShouldBeTrue();
    error.ShouldBeNull();
    names.Count.ShouldBeGreaterThan(0);
    foreach (string name in names)
    {
      name.Length.ShouldBeGreaterThanOrEqualTo(ClientNameScan.MinimumNameLength);
    }

    return Task.CompletedTask;
  }

  public static Task MissingFile_Should_Fail()
  {
    string root = Directory.CreateTempSubdirectory("client-name-scan-").FullName;
    try
    {
      ClientNameScan.TryReadNames(root, out _, out string? error).ShouldBeFalse();
      error.ShouldNotBeNull();
      error.ShouldContain(ClientNameScan.NamesFileRelativePath);
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }
}

public class Scan_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Scan_Given_>();

  public static Task ThisRepo_Should_BeClean()
  {
    ClientNameScan.ScanOutcome outcome = ClientNameScan.Scan(ClientNameScanRepo.Root());
    outcome.Error.ShouldBeNull();
    outcome.Hits.ShouldBeEmpty();
    return Task.CompletedTask;
  }
}

public class Ci_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Ci_Given_>();

  public static Task PullRequestJobs_Should_RunTheScan()
  {
    string root = ClientNameScanRepo.Root();
    string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/workflow.yml"));
    workflow.ShouldContain("name: fast");
    workflow.ShouldContain("--group fast");
    workflow.ShouldContain("dotnet run tools/dev-cli/dev.cs -- workflow");
    workflow.ShouldContain("dotnet run tools/dev-cli/dev.cs -- lint-skills");
    workflow.ShouldContain("^skills/");

    string build = File.ReadAllText(Path.Combine(root, "tools/dev-cli/endpoints/build-command.cs"));
    build.ShouldContain("ClientNameScan.Report(");

    string lint = File.ReadAllText(Path.Combine(root, "tools/dev-cli/endpoints/lint-skills-command.cs"));
    lint.ShouldContain("ClientNameScan.Report(");
    return Task.CompletedTask;
  }
}

file static class ClientNameScanRepo
{
  internal static string Root()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null
      && !File.Exists(Path.Combine(directory, ClientNameScan.TemplateProjectRelativePath)))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return directory;
  }
}
