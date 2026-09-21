#region Purpose
// Pack the architecture template and install it for local `dotnet new`.
#endregion

#region Design
// Replaces timewarp-templates/.../build-and-install-template.ps1. Packs to artifacts/template-install/
// (not the template source folder). Uninstall of a missing identity is ignored. Nupkg selection
// requires TimeWarp.Architecture.{version}.nupkg whose version segment starts with a digit so
// TimeWarp.Architecture.Analyzers.*.nupkg cannot win. --dry-run prints pack + install paths.
// Wipes the pack directory before pack so a leftover older nupkg cannot win after a version bump.
// Complements template-smoke (isolated 2.0.0-smoke packs) — this writes the user's template cache.
#endregion

namespace DevCli.Commands;

[NuruRoute("template-install", Description = "Pack the architecture template and install it for local dotnet new")]
[NuruRouteExample("template-install", Description = "Pack TimeWarp.Architecture and install it as a local template")]
[NuruRouteExample("template-install --dry-run", Description = "Print pack output and nupkg glob without packing")]
internal sealed class TemplateInstallCommand : ICommand<Unit>
{
  internal const string TemplateProject =
    "timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj";

  internal const string TemplatePackageId = TemplateNupkg.PackageId;

  [Option("dry-run", Description = "Print pack output and install steps without running them")]
  public bool DryRun { get; set; }

  internal sealed class Handler : ICommandHandler<TemplateInstallCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private TemplateInstallCommand Command = null!;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private string PackagesDir = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(TemplateInstallCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;

      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return Value;
      }

      RepoRoot = root;
      PackagesDir = Path.Combine(RepoRoot, "artifacts", "template-install");
      string project = Path.Combine(RepoRoot, TemplateProject);

      if (!File.Exists(project))
      {
        Terminal.WriteErrorLine($"Template project not found: {TemplateProject}".Red());
        Environment.ExitCode = 1;
        return Value;
      }

      if (Command.DryRun)
      {
        Terminal.WriteLine($"Would pack {TemplateProject}");
        Terminal.WriteLine($"  output: {PackagesDir}");
        Terminal.WriteLine($"Would uninstall {TemplatePackageId} (ignore if not installed)");
        Terminal.WriteLine($"Would install the newest {TemplatePackageId}.{{version}}.nupkg from that output");
        return Value;
      }

      if (Directory.Exists(PackagesDir))
      {
        Directory.Delete(PackagesDir, recursive: true);
      }

      Directory.CreateDirectory(PackagesDir);

      Terminal.WriteLine($"Packing template → {PackagesDir}...");
      int packExit = await DotNet.Pack(project)
        .WithConfiguration("Release")
        .WithOutput(PackagesDir)
        .WithNoValidation()
        .RunAsync(Ct)
        .ConfigureAwait(false);

      if (packExit != 0)
      {
        Terminal.WriteErrorLine("Template pack failed.".Red());
        Environment.ExitCode = packExit;
        return Value;
      }

      string? nupkg = TemplateNupkg.FindArchitectureTemplateNupkg(PackagesDir);
      if (nupkg is null)
      {
        Terminal.WriteErrorLine($"No {TemplatePackageId}.{{version}}.nupkg found in {PackagesDir}.".Red());
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine($"Installing template from {nupkg}...");
      CommandOutput uninstall = await Shell.Builder("dotnet")
        .WithArguments("new", "uninstall", TemplatePackageId)
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct)
        .ConfigureAwait(false);
      _ = uninstall;

      int installExit = await Shell.Builder("dotnet")
        .WithArguments("new", "install", nupkg)
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .RunAsync(Ct)
        .ConfigureAwait(false);

      if (installExit != 0)
      {
        Terminal.WriteErrorLine("dotnet new install failed.".Red());
        Environment.ExitCode = installExit;
        return Value;
      }

      Terminal.WriteLine($"Installed {Path.GetFileName(nupkg)} for `dotnet new timewarp-architecture`.".Green());
      return Value;
    }
  }
}
