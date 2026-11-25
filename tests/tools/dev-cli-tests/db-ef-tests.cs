#region Purpose
// Gates `dev db add-migration` paths, name validation, dotnet-ef arguments, and the post-scaffold
// model snapshot reconciliation, without running EF.
#endregion

// ReSharper disable InconsistentNaming
namespace DbEf_;

public class Paths_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Paths_Given_>();

  public static Task ScaffoldingPaths_Should_UseKebabContainerAppsLayout()
  {
    DbEf.InfrastructureProject.ShouldBe(
      "source/container-apps/web/projects/web-infrastructure/web-infrastructure.csproj");
    DbEf.StartupProject.ShouldBe(
      "source/container-apps/web/projects/web-server/web-server.csproj");
    DbEf.ContextName.ShouldBe("PostgresDbContext");
    DbEf.MigrationsOutputDir.ShouldBe("../../platform/postgres/migrations");
    DbEf.MigrationsNamespace.ShouldBe("TimeWarp.Architecture.Persistence.Migrations");
    DbEf.SnapshotFileName.ShouldBe("PostgresDbContextModelSnapshot.cs");
    return Task.CompletedTask;
  }

  public static Task MigrationsDirectory_Should_ResolveFromOutputDir()
  {
    string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo"));
    string projectDirectory = Path.GetDirectoryName(Path.Combine(root, DbEf.InfrastructureProject))!;
    Path.GetFullPath(Path.Combine(projectDirectory, DbEf.MigrationsOutputDir))
      .ShouldBe(Path.GetFullPath(Path.Combine(root, DbEf.MigrationsDirectory)));
    return Task.CompletedTask;
  }
}

public class IsValidMigrationName_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IsValidMigrationName_Given_>();

  public static Task PascalCaseIdentifier_Should_BeValid()
  {
    DbEf.IsValidMigrationName("AddOrders").ShouldBeTrue();
    DbEf.IsValidMigrationName("_private").ShouldBeTrue();
    DbEf.IsValidMigrationName("A1").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task EmptyOrNonIdentifier_Should_BeInvalid()
  {
    DbEf.IsValidMigrationName("").ShouldBeFalse();
    DbEf.IsValidMigrationName(" ").ShouldBeFalse();
    DbEf.IsValidMigrationName("1Start").ShouldBeFalse();
    DbEf.IsValidMigrationName("Add-Orders").ShouldBeFalse();
    DbEf.IsValidMigrationName("Add Orders").ShouldBeFalse();
    DbEf.IsValidMigrationName("../Escape").ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class BuildAddMigrationArguments_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<BuildAddMigrationArguments_Given_>();

  public static Task Name_Should_BeThirdArgumentAfterMigrationsAdd()
  {
    DbEf.EfToolCommand.ShouldBe("dotnet-ef");
    string[] arguments = DbEf.BuildAddMigrationArguments("AddOrders");
    arguments.ShouldBe(
    [
      "migrations",
      "add",
      "AddOrders",
      "--project",
      DbEf.InfrastructureProject,
      "--startup-project",
      DbEf.StartupProject,
      "--context",
      DbEf.ContextName,
      "--output-dir",
      DbEf.MigrationsOutputDir,
      "--namespace",
      DbEf.MigrationsNamespace
    ]);
    return Task.CompletedTask;
  }
}

public class ReconcileSnapshot_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ReconcileSnapshot_Given_>();

  private const string Before = "// old snapshot";
  private const string After = "// new snapshot";

  public static Task SnapshotUnderNamespacePath_Should_MoveOverTrackedAndRemoveEmptyDirectories()
  {
    using Fixture fixture = new();
    fixture.WriteTracked(Before);
    string misplacedDirectory = Path.Combine(fixture.Project, "TimeWarp", "Architecture", "Persistence", "Migrations");
    string misplaced = fixture.Write(misplacedDirectory, After);

    SnapshotReconciliation result = DbEf.ReconcileSnapshot(fixture.Project, fixture.Migrations, Before);

    result.Outcome.ShouldBe(SnapshotOutcome.Relocated);
    result.MisplacedSnapshots.ShouldBe([misplaced]);
    File.ReadAllText(fixture.Tracked).ShouldBe(After);
    File.Exists(misplaced).ShouldBeFalse();
    Directory.Exists(Path.Combine(fixture.Project, "TimeWarp")).ShouldBeFalse();
    result.RemovedDirectories.ShouldBe(
    [
      misplacedDirectory,
      Path.Combine(fixture.Project, "TimeWarp", "Architecture", "Persistence"),
      Path.Combine(fixture.Project, "TimeWarp", "Architecture"),
      Path.Combine(fixture.Project, "TimeWarp")
    ]);
    Directory.Exists(fixture.Project).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task NonEmptyParent_Should_StopCleanupThere()
  {
    using Fixture fixture = new();
    fixture.WriteTracked(Before);
    string misplacedDirectory = Path.Combine(fixture.Project, "TimeWarp", "Architecture", "Persistence", "Migrations");
    fixture.Write(misplacedDirectory, After);
    string keep = Path.Combine(fixture.Project, "TimeWarp", "Architecture", "keep.cs");
    File.WriteAllText(keep, "// keep");

    SnapshotReconciliation result = DbEf.ReconcileSnapshot(fixture.Project, fixture.Migrations, Before);

    result.Outcome.ShouldBe(SnapshotOutcome.Relocated);
    Directory.Exists(Path.Combine(fixture.Project, "TimeWarp", "Architecture", "Persistence")).ShouldBeFalse();
    File.Exists(keep).ShouldBeTrue();
    result.RemovedDirectories.Count.ShouldBe(2);
    return Task.CompletedTask;
  }

  public static Task NoTrackedSnapshotYet_Should_CreateIt()
  {
    using Fixture fixture = new();
    fixture.Write(Path.Combine(fixture.Project, "App", "Persistence", "Migrations"), After);

    SnapshotReconciliation result = DbEf.ReconcileSnapshot(fixture.Project, fixture.Migrations, null);

    result.Outcome.ShouldBe(SnapshotOutcome.Relocated);
    File.ReadAllText(fixture.Tracked).ShouldBe(After);
    return Task.CompletedTask;
  }

  public static Task TrackedSnapshotChangedInPlace_Should_ReportAlreadyInPlace()
  {
    using Fixture fixture = new();
    fixture.WriteTracked(After);

    SnapshotReconciliation result = DbEf.ReconcileSnapshot(fixture.Project, fixture.Migrations, Before);

    result.Outcome.ShouldBe(SnapshotOutcome.AlreadyInPlace);
    result.RemovedDirectories.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task NothingChanged_Should_ReportNotUpdated()
  {
    using Fixture fixture = new();
    fixture.WriteTracked(Before);

    DbEf.ReconcileSnapshot(fixture.Project, fixture.Migrations, Before).Outcome.ShouldBe(SnapshotOutcome.NotUpdated);
    return Task.CompletedTask;
  }

  public static Task NoSnapshotAnywhere_Should_ReportNotUpdated()
  {
    using Fixture fixture = new();

    DbEf.ReconcileSnapshot(fixture.Project, fixture.Migrations, null).Outcome.ShouldBe(SnapshotOutcome.NotUpdated);
    return Task.CompletedTask;
  }

  public static Task SnapshotsInBinAndObj_Should_BeIgnored()
  {
    using Fixture fixture = new();
    fixture.WriteTracked(Before);
    fixture.Write(Path.Combine(fixture.Project, "bin", "Debug"), After);
    fixture.Write(Path.Combine(fixture.Project, "obj", "Debug"), After);

    DbEf.ReconcileSnapshot(fixture.Project, fixture.Migrations, Before).Outcome.ShouldBe(SnapshotOutcome.NotUpdated);
    File.ReadAllText(fixture.Tracked).ShouldBe(Before);
    return Task.CompletedTask;
  }

  public static Task TwoMisplacedSnapshots_Should_ReportAmbiguousAndMoveNothing()
  {
    using Fixture fixture = new();
    fixture.WriteTracked(Before);
    string first = fixture.Write(Path.Combine(fixture.Project, "A"), After);
    string second = fixture.Write(Path.Combine(fixture.Project, "B"), After);

    SnapshotReconciliation result = DbEf.ReconcileSnapshot(fixture.Project, fixture.Migrations, Before);

    result.Outcome.ShouldBe(SnapshotOutcome.Ambiguous);
    result.MisplacedSnapshots.ShouldBe([first, second]);
    File.ReadAllText(fixture.Tracked).ShouldBe(Before);
    File.Exists(first).ShouldBeTrue();
    File.Exists(second).ShouldBeTrue();
    return Task.CompletedTask;
  }

  /// <summary>Temp repo slice: <c>projects/web-infrastructure</c> beside <c>platform/postgres/migrations</c>.</summary>
  private sealed class Fixture : IDisposable
  {
    private readonly string Root =
      Path.Combine(Path.GetTempPath(), "tw-525-snapshot-" + Guid.NewGuid().ToString("N"));

    public Fixture()
    {
      Project = Path.Combine(Root, "projects", "web-infrastructure");
      Migrations = Path.Combine(Root, "platform", "postgres", "migrations");
      Directory.CreateDirectory(Project);
      Directory.CreateDirectory(Migrations);
      Tracked = Path.Combine(Migrations, DbEf.SnapshotFileName);
    }

    public string Project { get; }

    public string Migrations { get; }

    public string Tracked { get; }

    public void WriteTracked(string text) => File.WriteAllText(Tracked, text);

    public string Write(string directory, string text)
    {
      Directory.CreateDirectory(directory);
      string path = Path.Combine(directory, DbEf.SnapshotFileName);
      File.WriteAllText(path, text);
      return path;
    }

    public void Dispose()
    {
      if (Directory.Exists(Root))
      {
        Directory.Delete(Root, recursive: true);
      }
    }
  }
}
