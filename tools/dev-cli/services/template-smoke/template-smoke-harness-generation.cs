#region Purpose
// TemplateSmokeHarness partial: dotnet new generation, SmokeOneAsync orchestration and process environment.
#endregion
#region Design
// SmokeOneAsync is the shared generate -> package-id assert -> callback -> restore -> build path;
// smoke-only package-mode / local-feed setup and publish-only pin/git/nuget.org steps stay as
// AfterGenerateAsync callbacks on the commands. Process calls stay Amuru wrappers of the real tools
// (dotnet new / restore / build).
#endregion

namespace DevCli.Services;

internal sealed partial class TemplateSmokeHarness
{
  /// <summary>
  /// Generate only (no restore/build): `dotnet new` into <paramref name="workDir"/>/<paramref name="name"/>. Returns the output
  /// directory, or null after printing the failure.
  /// </summary>
  public async Task<string?> GenerateAsync(
    string name,
    string workDir,
    string[] extraArgs,
    string templateShortName,
    CancellationToken ct,
    IReadOnlyDictionary<string, string>? environment = null)
  {
    string outputDir = Path.Combine(workDir, name);
    Terminal.WriteLine($"\n── Generate {name} ──".Cyan());

    List<string> args =
    [
      "new", templateShortName,
      "-n", name,
      "-o", outputDir,
      "--force",
    ];
    args.AddRange(extraArgs);

    ShellBuilder generateBuilder = Shell.Builder("dotnet")
      .WithArguments([.. args])
      .WithWorkingDirectory(RepoRoot)
      .WithNoValidation();
    generateBuilder = ApplyShellEnvironment(generateBuilder, environment);

    CommandOutput generate = await generateBuilder.CaptureAsync(ct);

    if (!generate.Success)
    {
      Terminal.WriteErrorLine(generate.Combined);
      Terminal.WriteErrorLine($"dotnet new failed for {name}!".Red());
      return null;
    }

    Terminal.WriteLine(generate.Combined);
    return outputDir;
  }

  /// <summary>
  /// Shared generate → package-id assert → optional prepare → find solution → restore → build.
  /// </summary>
  /// <param name="restoreNarration">
  /// Optional label after "Restoring {solution}"; e.g. " (local TimeWarp.* feed)" or
  /// " (nuget.org only)". Null → no suffix.
  /// </param>
  /// <param name="environment">
  /// Optional process env for generate / restore / build (publish isolation hive).
  /// </param>
  public async Task<bool> SmokeOneAsync(
    string name,
    string workDir,
    string[] extraArgs,
    string templateShortName,
    CancellationToken ct,
    Func<string, Task<bool>>? afterGenerateAsync = null,
    string? restoreNarration = null,
    IReadOnlyDictionary<string, string>? environment = null)
  {
    string? outputDir = await GenerateAsync(name, workDir, extraArgs, templateShortName, ct, environment);
    if (outputDir is null)
      return false;

    if (!AssertPackageIdsNotRewritten(name, outputDir))
      return false;

    if (afterGenerateAsync is not null && !await afterGenerateAsync(outputDir))
      return false;

    string? solution = Directory
      .GetFiles(outputDir, "*.slnx", SearchOption.TopDirectoryOnly)
      .Concat(Directory.GetFiles(outputDir, "*.sln", SearchOption.TopDirectoryOnly))
      .FirstOrDefault();

    if (solution is null)
    {
      Terminal.WriteErrorLine($"No solution file found under {outputDir}.".Red());
      return false;
    }

    Terminal.WriteLine($"Restoring {solution}{restoreNarration}...");

    DotNetRestoreBuilder restoreBuilder = DotNet.Restore()
      .WithProject(solution)
      .WithWorkingDirectory(outputDir)
      .WithNoValidation();
    restoreBuilder = ApplyCommandEnvironment(restoreBuilder, environment);

    int restoreExit = await restoreBuilder.RunAsync(ct);
    if (restoreExit != 0)
    {
      Terminal.WriteErrorLine($"Restore failed for {name}!".Red());
      return false;
    }

    Terminal.WriteLine($"Building {solution} (Release)...");
    DotNetBuildBuilder buildBuilder = DotNet.Build()
      .WithProject(solution)
      .WithConfiguration("Release")
      .WithNoRestore()
      .WithWorkingDirectory(outputDir)
      .WithNoValidation();
    buildBuilder = ApplyCommandEnvironment(buildBuilder, environment);

    int buildExit = await buildBuilder.RunAsync(ct);
    if (buildExit != 0)
    {
      Terminal.WriteErrorLine($"Build failed for {name}!".Red());
      return false;
    }

    Terminal.WriteLine($"{name} OK".Green());
    return true;
  }

  private static ShellBuilder ApplyShellEnvironment(
    ShellBuilder builder,
    IReadOnlyDictionary<string, string>? environment)
  {
    if (environment is null)
      return builder;

    foreach ((string key, string value) in environment)
      builder = builder.WithEnvironmentVariable(key, value);

    return builder;
  }

  private static TBuilder ApplyCommandEnvironment<TBuilder>(
    TBuilder builder,
    IReadOnlyDictionary<string, string>? environment)
    where TBuilder : ICommandBuilder<TBuilder>
  {
    if (environment is null)
      return builder;

    foreach ((string key, string value) in environment)
      builder = builder.WithEnvironmentVariable(key, value);

    return builder;
  }
}
