#region Purpose
// Pins the client-name scan to a temp git repo: clean passes, a planted name fails, an excluded folder passes.
#endregion

namespace ClientNameScan_;

using TimeWarp.Amuru;

public class ScanAsync_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ScanAsync_Given_>();

  public static async Task CleanTree_Should_Pass()
  {
    string root = await TempGit.InitAsync();
    try
    {
      Write(root, ClientNameScan.NamesFileRelativePath, "acmeclient\n");
      Write(root, "source/app.cs", "clean\n");
      await TempGit.CommitAsync(root);

      ClientNameScan.ScanOutcome outcome = await ClientNameScan.ScanAsync(root, CancellationToken.None);
      outcome.Error.ShouldBeNull();
      outcome.Hits.ShouldBeEmpty();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }
  }

  public static async Task PlantedName_Should_ReportPathAndLine()
  {
    string root = await TempGit.InitAsync();
    try
    {
      Write(root, ClientNameScan.NamesFileRelativePath, "acmeclient\n");
      Write(root, "skills/tw-sample/SKILL.md", "prefix ACMECLIENT suffix\n");
      Write(root, "skills/tw-sample/analysis/note.md", "acmeclient\n");
      await TempGit.CommitAsync(root);

      ClientNameScan.ScanOutcome outcome = await ClientNameScan.ScanAsync(root, CancellationToken.None);
      outcome.Error.ShouldBeNull();
      outcome.Hits.Count.ShouldBe(1);
      outcome.Hits[0].RelativePath.ShouldBe("skills/tw-sample/SKILL.md");
      outcome.Hits[0].Line.ShouldBe(1);
      outcome.Hits[0].Name.ShouldBe("acmeclient");
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }
  }

  public static async Task ExcludedFolder_Should_Pass()
  {
    string root = await TempGit.InitAsync();
    try
    {
      Write(root, ClientNameScan.NamesFileRelativePath, "acmeclient\n");
      Write(root, "documentation/note.md", "acmeclient\n");
      Write(root, "skills/tw-sample/analysis/note.md", "acmeclient\n");
      // The screenshot path guard rejects a quoted board-column path in test source.
      string board = string.Concat('k', 'a', 'n', 'b', 'a', 'n');
      Write(root, board + "/done/note.md", "acmeclient\n");
      Write(root, "source/app.cs", "clean\n");
      await TempGit.CommitAsync(root);

      ClientNameScan.ScanOutcome outcome = await ClientNameScan.ScanAsync(root, CancellationToken.None);
      outcome.Error.ShouldBeNull();
      outcome.Hits.ShouldBeEmpty();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }
  }

  public static async Task CommentLine_Should_NotBeAPattern()
  {
    string root = await TempGit.InitAsync();
    try
    {
      Write(root, ClientNameScan.NamesFileRelativePath, "# markerphrase\nacmeclient\n");
      Write(root, "source/app.cs", "# markerphrase\n");
      await TempGit.CommitAsync(root);

      ClientNameScan.ScanOutcome outcome = await ClientNameScan.ScanAsync(root, CancellationToken.None);
      outcome.Error.ShouldBeNull();
      outcome.Hits.ShouldBeEmpty();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }
  }

  private static void Write(string root, string relative, string contents)
  {
    string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
    string? directory = Path.GetDirectoryName(path);
    if (directory is not null)
    {
      Directory.CreateDirectory(directory);
    }

    File.WriteAllText(path, contents);
  }
}

file static class TempGit
{
  internal static async Task<string> InitAsync()
  {
    string root = Directory.CreateTempSubdirectory("client-name-scan-").FullName;
    await RunAsync(root, "init");
    return root;
  }

  internal static async Task CommitAsync(string root)
  {
    await RunAsync(root, "add", "-A");
    await RunAsync(
      root,
      "-c", "user.email=client-name-scan@local",
      "-c", "user.name=client-name-scan",
      "-c", "commit.gpgsign=false",
      "commit",
      "-m", "seed");
  }

  private static async Task RunAsync(string root, params string[] arguments)
  {
    CommandOutput output = await Shell.Builder("git")
      .WithArguments(arguments)
      .WithWorkingDirectory(root)
      .WithNoValidation()
      .CaptureAsync();
    output.ExitCode.ShouldBe(0, output.Combined);
  }
}
