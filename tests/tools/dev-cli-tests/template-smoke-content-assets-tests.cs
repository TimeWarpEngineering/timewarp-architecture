// ReSharper disable InconsistentNaming
namespace TemplateSmokeContentAssets_;

public class TryResolve_Given_
{
  private const string Version = "12.0.0-beta.10";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryResolve_Given_>();

  public static Task StateAssetOnlyInBlazorPackage_Should_ResolveBlazorStaticWebAsset()
  {
    string packagesRoot = NewPackagesRoot();
    try
    {
      string asset = WriteAsset(packagesRoot, "TimeWarp.State.Blazor", "js/logger.js");
      WriteAsset(packagesRoot, "TimeWarp.State.Blazor", "js/constants.js");

      bool resolved = TemplateSmokeContentAssets.TryResolve(
        "/_content/TimeWarp.State/js/logger.js",
        Pins(),
        packagesRoot,
        out string? resolvedPath,
        out string? error);

      resolved.ShouldBeTrue(error);
      resolvedPath.ShouldBe(asset);
      error.ShouldBeNull();

      bool constantsResolved = TemplateSmokeContentAssets.TryResolve(
        "/_content/TimeWarp.State/js/constants.js",
        Pins(),
        packagesRoot,
        out string? constantsPath,
        out string? constantsError);

      constantsResolved.ShouldBeTrue(constantsError);
      constantsPath.ShouldNotBeNull();
      constantsPath.ShouldContain(
        $"{Path.DirectorySeparatorChar}timewarp.state.blazor{Path.DirectorySeparatorChar}");
    }
    finally
    {
      Directory.Delete(packagesRoot, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task StatePackageHasAsset_Should_PreferUrlPackage()
  {
    string packagesRoot = NewPackagesRoot();
    try
    {
      string stateAsset = WriteAsset(packagesRoot, "TimeWarp.State", "js/logger.js");
      WriteAsset(packagesRoot, "TimeWarp.State.Blazor", "js/logger.js");

      bool resolved = TemplateSmokeContentAssets.TryResolve(
        "/_content/TimeWarp.State/js/logger.js",
        Pins(),
        packagesRoot,
        out string? resolvedPath,
        out string? error);

      resolved.ShouldBeTrue(error);
      resolvedPath.ShouldBe(stateAsset);
    }
    finally
    {
      Directory.Delete(packagesRoot, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task StatePlusSpecifier_Should_ResolvePlusPackage()
  {
    string packagesRoot = NewPackagesRoot();
    try
    {
      string asset = WriteAsset(packagesRoot, "TimeWarp.State.Plus", "js/download-file.js");

      bool resolved = TemplateSmokeContentAssets.TryResolve(
        "/_content/TimeWarp.State.Plus/js/download-file.js",
        Pins(),
        packagesRoot,
        out string? resolvedPath,
        out string? error);

      resolved.ShouldBeTrue(error);
      resolvedPath.ShouldBe(asset);
    }
    finally
    {
      Directory.Delete(packagesRoot, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task BlazorPinWithoutStatePin_Should_ResolveBlazorPackage()
  {
    string packagesRoot = NewPackagesRoot();
    try
    {
      string asset = WriteAsset(packagesRoot, "TimeWarp.State.Blazor", "js/logger.js");
      Dictionary<string, string> versions = new(StringComparer.OrdinalIgnoreCase)
      {
        ["TimeWarp.State.Blazor"] = Version,
      };

      bool resolved = TemplateSmokeContentAssets.TryResolve(
        "/_content/TimeWarp.State/js/logger.js",
        versions,
        packagesRoot,
        out string? resolvedPath,
        out string? error);

      resolved.ShouldBeTrue(error);
      resolvedPath.ShouldBe(asset);
    }
    finally
    {
      Directory.Delete(packagesRoot, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task BothPackagesMiss_Should_ReportBothPaths()
  {
    string packagesRoot = NewPackagesRoot();
    try
    {
      bool resolved = TemplateSmokeContentAssets.TryResolve(
        "/_content/TimeWarp.State/js/logger.js",
        Pins(),
        packagesRoot,
        out string? resolvedPath,
        out string? error);

      resolved.ShouldBeFalse();
      resolvedPath.ShouldBeNull();
      error.ShouldNotBeNull();
      error.ShouldContain($"{Path.DirectorySeparatorChar}timewarp.state{Path.DirectorySeparatorChar}");
      error.ShouldContain($"{Path.DirectorySeparatorChar}timewarp.state.blazor{Path.DirectorySeparatorChar}");
      error.ShouldContain("logger.js");
    }
    finally
    {
      Directory.Delete(packagesRoot, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task UnpinnedPackage_Should_ReportMissingPin()
  {
    bool resolved = TemplateSmokeContentAssets.TryResolve(
      "/_content/Not.A.Package/js/missing.js",
      Pins(),
      "unused-packages-root",
      out string? resolvedPath,
      out string? error);

    resolved.ShouldBeFalse();
    resolvedPath.ShouldBeNull();
    error.ShouldBe("/_content/Not.A.Package/js/missing.js (PackageVersion for Not.A.Package not found)");
    return Task.CompletedTask;
  }

  public static Task RelativeSpecifier_Should_Skip()
  {
    bool resolved = TemplateSmokeContentAssets.TryResolve(
      "./spa.js",
      Pins(),
      "unused-packages-root",
      out string? resolvedPath,
      out string? error);

    resolved.ShouldBeFalse();
    resolvedPath.ShouldBeNull();
    error.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task MalformedSpecifier_Should_Error()
  {
    bool resolved = TemplateSmokeContentAssets.TryResolve(
      "/_content/TimeWarp.State",
      Pins(),
      "unused-packages-root",
      out string? resolvedPath,
      out string? error);

    resolved.ShouldBeFalse();
    resolvedPath.ShouldBeNull();
    error.ShouldBe("Malformed _content specifier: /_content/TimeWarp.State");
    return Task.CompletedTask;
  }

  private static Dictionary<string, string> Pins()
  {
    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["TimeWarp.State"] = Version,
      ["TimeWarp.State.Blazor"] = Version,
      ["TimeWarp.State.Plus"] = Version,
    };
  }

  private static string NewPackagesRoot()
  {
    string root = Path.Combine(AppContext.BaseDirectory, "temp-content-assets", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    return root;
  }

  private static string WriteAsset(string packagesRoot, string packageId, string relativeAsset)
  {
    string path = Path.Combine(
      packagesRoot,
      packageId.ToLowerInvariant(),
      Version,
      "staticwebassets",
      relativeAsset.Replace('/', Path.DirectorySeparatorChar));
    string? directory = Path.GetDirectoryName(path);
    ArgumentException.ThrowIfNullOrEmpty(directory);
    Directory.CreateDirectory(directory);
    File.WriteAllText(path, "export {};\n");
    return path;
  }
}
