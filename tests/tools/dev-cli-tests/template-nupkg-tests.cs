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
