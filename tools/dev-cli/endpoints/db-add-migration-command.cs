#region Purpose
// Scaffold an EF Core migration for PostgresDbContext (`dev db add-migration`).
#endregion

#region Design
// Replaces scripts/postgres/add-migration.ps1. Design-time `dotnet ef` against kebab paths
// (web-infrastructure + web-server startup). Not the Aspire web-migrations resource — that
// path is `dev db update` / drop / reset / status. --dry-run prints the exact invocation.
// Requires a C# identifier name; `dotnet tool restore` runs first so local `dotnet-ef` is present.
// Both calls use the DotNet.Tool() builders (AMURU0001); `dotnet tool run
// dotnet-ef` is the same local tool `dotnet ef` resolves.
// After a successful scaffold, DbEf.ReconcileSnapshot moves the model snapshot EF writes under
// web-infrastructure/<namespace path>/ back over the tracked one in platform/postgres/migrations
// (EF cannot target the snapshot directory for compile-linked migrations; see DbEf Design). The
// command fails when the snapshot was not updated or more than one misplaced copy exists.
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

  internal sealed class Handler : IRequestHandler<DbAddMigrationCommand, Unit>
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
        return Unit.Value;
      }

      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      string infrastructureProject = Path.Combine(root, DbEf.InfrastructureProject);
      string startupProject = Path.Combine(root, DbEf.StartupProject);
      if (!File.Exists(infrastructureProject) || !File.Exists(startupProject))
      {
        Terminal.WriteErrorLine(
          "web-infrastructure or web-server project not found. `dev db add-migration` requires the postgres template flag.".Red());
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      string[] arguments = DbEf.BuildAddMigrationArguments(command.Name);
      if (command.DryRun)
      {
        Terminal.WriteLine($"dotnet tool run {DbEf.EfToolCommand} {string.Join(" ", arguments)}");
        return Unit.Value;
      }

      Terminal.WriteLine("Restoring local tools (dotnet-ef)...");
      CommandOutput restore = await DotNet.Tool()
        .WithWorkingDirectory(root)
        .Restore()
        .WithNoValidation()
        .CaptureAsync(ct)
        .ConfigureAwait(false);

      if (!restore.Success)
      {
        Terminal.WriteErrorLine(restore.Combined);
        Terminal.WriteErrorLine("dotnet tool restore failed.".Red());
        Environment.ExitCode = restore.ExitCode == 0 ? 1 : restore.ExitCode;
        return Unit.Value;
      }

      string migrationsDirectory = Path.Combine(root, DbEf.MigrationsDirectory);
      string trackedSnapshot = Path.Combine(migrationsDirectory, DbEf.SnapshotFileName);
      string? trackedSnapshotBefore = File.Exists(trackedSnapshot)
        ? await File.ReadAllTextAsync(trackedSnapshot, ct).ConfigureAwait(false)
        : null;

      Terminal.WriteLine($"Scaffolding migration '{command.Name}'...");
      int exitCode = await DotNet.Tool()
        .WithWorkingDirectory(root)
        .Run(DbEf.EfToolCommand)
        .WithArguments(arguments)
        .WithNoValidation()
        .RunAsync(ct)
        .ConfigureAwait(false);

      if (exitCode != 0)
      {
        Terminal.WriteErrorLine("dotnet ef migrations add failed.".Red());
        Environment.ExitCode = exitCode;
        return Unit.Value;
      }

      SnapshotReconciliation reconciliation = DbEf.ReconcileSnapshot(
        Path.GetDirectoryName(infrastructureProject)!,
        migrationsDirectory,
        trackedSnapshotBefore);

      switch (reconciliation.Outcome)
      {
        case SnapshotOutcome.Relocated:
          Terminal.WriteLine(
            $"Moved {Path.GetRelativePath(root, reconciliation.MisplacedSnapshots[0])} to {DbEf.MigrationsDirectory}/{DbEf.SnapshotFileName}.");
          foreach (string directory in reconciliation.RemovedDirectories)
          {
            Terminal.WriteLine($"Removed empty {Path.GetRelativePath(root, directory)}/");
          }

          break;
        case SnapshotOutcome.Ambiguous:
          Terminal.WriteErrorLine(
            $"More than one {DbEf.SnapshotFileName} under web-infrastructure; move the right one to {DbEf.MigrationsDirectory}/ and delete the rest:".Red());
          foreach (string path in reconciliation.MisplacedSnapshots)
          {
            Terminal.WriteErrorLine($"  {Path.GetRelativePath(root, path)}");
          }

          Environment.ExitCode = 1;
          return Unit.Value;
        case SnapshotOutcome.NotUpdated:
          Terminal.WriteErrorLine(
            $"dotnet ef succeeded but {DbEf.MigrationsDirectory}/{DbEf.SnapshotFileName} was not updated and no misplaced snapshot was found.".Red());
          Environment.ExitCode = 1;
          return Unit.Value;
        case SnapshotOutcome.AlreadyInPlace:
        default:
          break;
      }

      Terminal.WriteLine($"Migration '{command.Name}' added under {DbEf.MigrationsDirectory}/.".Green());
      Terminal.WriteLine("Apply with `dev db update` against a running AppHost (`dev run`).");
      return Unit.Value;
    }
  }
}
