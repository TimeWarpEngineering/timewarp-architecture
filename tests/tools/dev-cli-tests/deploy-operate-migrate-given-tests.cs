#region Purpose
// Gates `dev deploy migrate` (task 287) flow and refusal texts without a cluster, a container runtime or Azure.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public class Migrate_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Migrate_Given_>();

  public static Task MissingPublishedOutput_Should_RefuseWithTheExactPublishCommand()
  {
    string message = DeployOperate.MissingPublishedOutputMessage(AspireDeploy.Kubernetes, "/repo/artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql");
    message.ShouldContain("/repo/artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql does not exist");
    message.ShouldContain("Publishing them now");
    message.ShouldEndWith("dev publish kubernetes");
    message.ShouldNotContain("never publishes");
    return Task.CompletedTask;
  }

  public static Task Confirmation_Should_RefuseWithoutATerminal()
  {
    DeployOperate.MigrateConfirmationRefusal.ShouldContain("--yes");
    DeployOperate.MigrateDeclined.ShouldContain("Nothing was run");
    return Task.CompletedTask;
  }

  public static Task Summary_Should_SayWhatRanAgainstWhichTarget()
  {
    DeployOperate.BuildMigrateSummary(AspireDeploy.Kubernetes, "web-migrations.sql with psql", "context kind-app, namespace my-app", 0)
      .ShouldBe("dev deploy migrate: applied web-migrations.sql with psql to kubernetes (context kind-app, namespace my-app).");
    DeployOperate.BuildMigrateSummary(AspireDeploy.Compose, "web-migrations.sql with psql", "Compose project p", 3)
      .ShouldBe("dev deploy migrate: web-migrations.sql with psql against compose (Compose project p) failed (exit 3).");
    return Task.CompletedTask;
  }

  public static Task History_Should_ReadTheIdsAndTreatAMissingTableAsNoneApplied()
  {
    DeployMigrationHistory.ParseMigrationHistory(0, "20260101_Init\n20260201_Todo\n", "").ShouldBe(["20260101_Init", "20260201_Todo"]);
    DeployMigrationHistory.ParseMigrationHistory(0, "", "").ShouldBeEmpty();
    DeployMigrationHistory.ParseMigrationHistory(1, "", "ERROR:  42P01\n").ShouldNotBeNull().ShouldBeEmpty();
    DeployMigrationHistory.ParseMigrationHistory(2, "", "psql: error: connection to server failed").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task NewlyApplied_Should_BeTheAfterIdsNotInBefore()
  {
    DeployMigrationHistory.NewlyAppliedMigrations(["a", "b"], ["a", "b", "c", "d"]).ShouldBe(["c", "d"]);
    DeployMigrationHistory.NewlyAppliedMigrations(["a", "b"], ["a", "b"]).ShouldBeEmpty();
    DeployMigrationHistory.NewlyAppliedMigrations([], ["a"]).ShouldBe(["a"]);
    return Task.CompletedTask;
  }

  public static Task Report_Should_ListTheAppliedMigrations()
  {
    DeployMigrationHistory.BuildMigrationReport(["20260101_Init"], ["20260101_Init", "20260201_Todo", "20260301_Roles"], 0).ShouldBe(
    [
      "Applied 2 migration(s):",
      "  20260201_Todo",
      "  20260301_Roles",
      "Already applied: 1 · Database now at 20260301_Roles",
    ]);
    return Task.CompletedTask;
  }

  public static Task Report_Should_SayUpToDateWhenNothingApplied()
  {
    DeployMigrationHistory.BuildMigrationReport(["20260101_Init", "20260201_Todo"], ["20260101_Init", "20260201_Todo"], 0)
      .ShouldBe(["Up to date: no pending migrations (2 applied, latest 20260201_Todo)"]);
    return Task.CompletedTask;
  }

  public static Task Report_Should_ApplyEverythingOnAFreshDatabase()
  {
    string[]? fresh = DeployMigrationHistory.ParseMigrationHistory(1, "", "ERROR:  42P01");
    DeployMigrationHistory.BuildMigrationReport(fresh, ["20260101_Init", "20260201_Todo"], 0).ShouldBe(
    [
      "Applied 2 migration(s):",
      "  20260101_Init",
      "  20260201_Todo",
      "Already applied: 0 · Database now at 20260201_Todo",
    ]);
    return Task.CompletedTask;
  }

  public static Task Report_Should_SayWhereAFailureStopped()
  {
    DeployMigrationHistory.BuildMigrationReport(["a"], ["a", "b"], 3).ShouldBe(
    [
      "Applied 1 migration(s):",
      "  b",
      "Failed while applying migrations after b",
    ]);
    DeployMigrationHistory.BuildMigrationReport(["a"], ["a"], 3).ShouldBe(["Failed while applying migrations after a"]);
    DeployMigrationHistory.BuildMigrationReport([], [], 3).ShouldBe(["Failed before any migration was applied"]);
    DeployMigrationHistory.BuildMigrationReport(["a"], null, 3).Single().ShouldContain("could not be read after the failure");
    DeployMigrationHistory.BuildMigrationReport(null, ["a"], 0).Single().ShouldContain("could not be read before the run");
    return Task.CompletedTask;
  }

  public static Task BundleReport_Should_ComeFromTheBundlesApplyingLines()
  {
    const string applied = "Build started...\nApplying migration '20260201_Todo'.\nApplying migration '20260301_Roles'.\nDone.\n";
    DeployMigrationHistory.BuildBundleReport(applied, 0).ShouldBe(
    [
      "Applied 2 migration(s):",
      "  20260201_Todo",
      "  20260301_Roles",
      "Database now at 20260301_Roles",
    ]);
    DeployMigrationHistory.BuildBundleReport("No migrations were applied. The database is already up to date.\nDone.\n", 0)
      .ShouldBe(["Up to date: no pending migrations"]);
    DeployMigrationHistory.BuildBundleReport(applied, 1).ShouldBe(
    [
      "Applied 1 migration(s):",
      "  20260201_Todo",
      "Failed while applying 20260301_Roles",
    ]);
    DeployMigrationHistory.BuildBundleReport("Applying migration '20260201_Todo'.\n", 1).ShouldBe(["Failed while applying 20260201_Todo"]);
    DeployMigrationHistory.BuildBundleReport("error\n", 1).ShouldBe(["Failed before any migration was applied"]);
    DeployAca.BuildBundleArguments("cs", verbose: true).ShouldBe(["--connection", "cs", "--verbose"]);
    DeployAca.BuildBundleArguments("cs").ShouldBe(["--connection", "cs"]);
    return Task.CompletedTask;
  }
}
