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

  public static Task Name_Should_BeFourthArgumentAfterEfMigrationsAdd()
  {
    string[] arguments = DbEf.BuildAddMigrationArguments("AddOrders");
    arguments.ShouldBe(
    [
      "ef",
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
