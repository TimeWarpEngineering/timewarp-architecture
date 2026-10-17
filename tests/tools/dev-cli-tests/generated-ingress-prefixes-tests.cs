#region Purpose
// Pins ingress golden normalization and the host project list the regenerate command builds.
#endregion

namespace GeneratedIngressPrefixes_;

public class Normalize_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Normalize_Given_>();

  public static Task Text_Should_DropBomAndUseLf()
  {
    string normalized = GeneratedIngressPrefixes.Normalize("\uFEFFline\r\nnext\rlast");
    normalized.ShouldBe("line\nnext\nlast");
    return Task.CompletedTask;
  }
}

public class Hosts_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Hosts_Given_>();

  public static Task Projects_Should_BeTheTwoIngressHosts()
  {
    GeneratedIngressPrefixes.HostProjects.ShouldBe(
    [
      "source/container-apps/yarp/yarp.csproj",
      "source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj",
    ]);
    GeneratedIngressPrefixes.SnapshotRelativePath.ShouldBe("goldens/ingress/WebServerApiRoutePrefixes.g.cs");
    return Task.CompletedTask;
  }
}

public class Emit_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Emit_Given_>();

  public static Task OneFile_Should_ReturnNormalizedText()
  {
    string root = Path.Combine(Path.GetTempPath(), "tw-308-ingress-" + Guid.NewGuid().ToString("N"));
    try
    {
      string emit = Path.Combine(root, "generator", GeneratedIngressPrefixes.EmittedFileName);
      Directory.CreateDirectory(Path.GetDirectoryName(emit)!);
      File.WriteAllText(emit, "\uFEFFalpha\r\nbeta\n");

      bool read = GeneratedIngressPrefixes.TryReadEmit(root, out string text, out string error);

      read.ShouldBeTrue();
      error.ShouldBe(string.Empty);
      text.ShouldBe("alpha\nbeta\n");
      return Task.CompletedTask;
    }
    finally
    {
      if (Directory.Exists(root))
      {
        Directory.Delete(root, recursive: true);
      }
    }
  }

  public static Task DisagreeingCopies_Should_Fail()
  {
    string root = Path.Combine(Path.GetTempPath(), "tw-308-ingress-" + Guid.NewGuid().ToString("N"));
    try
    {
      string first = Path.Combine(root, "a", GeneratedIngressPrefixes.EmittedFileName);
      string second = Path.Combine(root, "b", GeneratedIngressPrefixes.EmittedFileName);
      Directory.CreateDirectory(Path.GetDirectoryName(first)!);
      Directory.CreateDirectory(Path.GetDirectoryName(second)!);
      File.WriteAllText(first, "one\n");
      File.WriteAllText(second, "two\n");

      bool read = GeneratedIngressPrefixes.TryReadEmit(root, out string text, out string error);

      read.ShouldBeFalse();
      text.ShouldBe(string.Empty);
      error.ShouldContain(GeneratedIngressPrefixes.EmittedFileName);
      return Task.CompletedTask;
    }
    finally
    {
      if (Directory.Exists(root))
      {
        Directory.Delete(root, recursive: true);
      }
    }
  }
}
