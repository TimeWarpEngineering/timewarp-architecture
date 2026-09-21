// ReSharper disable InconsistentNaming
namespace TemplateNupkg_;

public class IsArchitectureTemplateNupkgFileName_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IsArchitectureTemplateNupkgFileName_Given_>();

  public static Task VersionedTemplateId_Should_Match()
  {
    TemplateNupkg.IsArchitectureTemplateNupkgFileName("TimeWarp.Architecture.2.0.0-beta.20.nupkg")
      .ShouldBeTrue();
    TemplateNupkg.IsArchitectureTemplateNupkgFileName("TimeWarp.Architecture.2.0.0.nupkg")
      .ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task PlatformPackagesAndSymbols_Should_NotMatch()
  {
    TemplateNupkg.IsArchitectureTemplateNupkgFileName("TimeWarp.Architecture.Analyzers.2.0.0-beta.20.nupkg")
      .ShouldBeFalse();
    TemplateNupkg.IsArchitectureTemplateNupkgFileName("TimeWarp.Architecture.Generators.2.0.0-beta.20.nupkg")
      .ShouldBeFalse();
    TemplateNupkg.IsArchitectureTemplateNupkgFileName("TimeWarp.Architecture.Attributes.2.0.0-beta.20.nupkg")
      .ShouldBeFalse();
    TemplateNupkg.IsArchitectureTemplateNupkgFileName("TimeWarp.Architecture.2.0.0-beta.20.snupkg")
      .ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class FindArchitectureTemplateNupkg_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FindArchitectureTemplateNupkg_Given_>();

  public static Task MissingDirectory_Should_ReturnNull()
  {
    TemplateNupkg.FindArchitectureTemplateNupkg(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")))
      .ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task NewerPack_Should_WinOverLexicographicallyLaterStaleVersion()
  {
    string directory = Path.Combine(Path.GetTempPath(), "template-nupkg-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
      string stale = Path.Combine(directory, "TimeWarp.Architecture.2.0.0-beta.9.nupkg");
      string newer = Path.Combine(directory, "TimeWarp.Architecture.2.0.0-beta.10.nupkg");
      string analyzers = Path.Combine(directory, "TimeWarp.Architecture.Analyzers.2.0.0-beta.10.nupkg");
      File.WriteAllBytes(stale, [0]);
      File.WriteAllBytes(analyzers, [0]);
      File.SetLastWriteTimeUtc(stale, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
      File.SetLastWriteTimeUtc(analyzers, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
      File.WriteAllBytes(newer, [0]);
      File.SetLastWriteTimeUtc(newer, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

      TemplateNupkg.FindArchitectureTemplateNupkg(directory).ShouldBe(newer);
    }
    finally
    {
      Directory.Delete(directory, recursive: true);
    }

    return Task.CompletedTask;
  }
}
