#region Purpose
// Scaffold an EF Core migration for PostgresDbContext (`dev db add-migration`).
#endregion

#region Design
// Replaces scripts/postgres/add-migration.ps1. Design-time `dotnet ef` against kebab paths
// (web-infrastructure + web-server startup). Not the Aspire web-migrations resource — that
// path is `dev db update` / drop / reset / status. --dry-run prints the exact invocation.
// Requires a C# identifier name; `dotnet tool restore` runs first so local `dotnet-ef` is present.
#endregion

namespace DevCli.Commands;

[NuruRoute("add-migration", Description = "Scaffold an EF Core migration for PostgresDbContext")]
[NuruRouteExample("db add-migration AddOrders", Description = "Scaffold after changing the model")]
[NuruRouteExample("db add-migration AddOrders --dry-run", Description = "Print the dotnet ef invocation without running it")]
internal sealed class DbAddMigrationCommand : DbGroup, ICommand<Unit>
{
  [Parameter(Description = "EF migration class name (C# identifier)")]
  public string Name { get; set; } = string.Empty;

  [Option("dry-run", Description = "Print the dotnet ef invocation without running it")]
  public bool DryRun { get; set; }

  internal sealed class Handler : ICommandHandler<DbAddMigrationCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(DbAddMigrationCommand command, CancellationToken ct)
    {
      if (!DbEf.IsValidMigrationName(command.Name))
      {
        Terminal.WriteErrorLine(
          "Migration name must be a C# identifier (letter or underscore, then letters, digits, underscore).".Red());
        Terminal.WriteLine("Usage: dev db add-migration AddOrders");
        Environment.ExitCode = 1;
        return Value;
      }

      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return Value;
      }

      string infrastructureProject = Path.Combine(root, DbEf.InfrastructureProject);
      string startupProject = Path.Combine(root, DbEf.StartupProject);
      if (!File.Exists(infrastructureProject) || !File.Exists(startupProject))
      {
        Terminal.WriteErrorLine(
          "web-infrastructure or web-server project not found. `dev db add-migration` requires the postgres template flag.".Red());
        Environment.ExitCode = 1;
        return Value;
      }

      string[] arguments = DbEf.BuildAddMigrationArguments(command.Name);
      if (command.DryRun)
      {
        Terminal.WriteLine($"dotnet {string.Join(" ", arguments)}");
        return Value;
      }

      Terminal.WriteLine("Restoring local tools (dotnet-ef)...");
      CommandOutput restore = await Shell.Builder("dotnet")
        .WithArguments("tool", "restore")
        .WithWorkingDirectory(root)
        .WithNoValidation()
        .CaptureAsync(ct)
        .ConfigureAwait(false);

      if (!restore.Success)
      {
        Terminal.WriteErrorLine(restore.Combined);
        Terminal.WriteErrorLine("dotnet tool restore failed.".Red());
        Environment.ExitCode = restore.ExitCode == 0 ? 1 : restore.ExitCode;
        return Value;
      }

      Terminal.WriteLine($"Scaffolding migration '{command.Name}'...");
      int exitCode = await Shell.Builder("dotnet")
        .WithArguments(arguments)
        .WithWorkingDirectory(root)
        .WithNoValidation()
        .RunAsync(ct)
        .ConfigureAwait(false);

      if (exitCode != 0)
      {
        Terminal.WriteErrorLine("dotnet ef migrations add failed.".Red());
        Environment.ExitCode = exitCode;
        return Value;
      }

      Terminal.WriteLine($"Migration '{command.Name}' added under source/container-apps/web/platform/postgres/migrations/.".Green());
      Terminal.WriteLine("Apply with `dev db update` against a running AppHost (`dev run`).");
      return Value;
    }
  }
}
