#region Purpose
// Gates the compose per-target command construction for `dev open` / `dev deploy migrate`.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public class Compose_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Compose_Given_>();

  private const string Env = "# Parameter ingress-port\nINGRESS_PORT=\n\n# Parameter postgres-password\nPOSTGRES_PASSWORD=secret\n";

  public static Task Port_Should_PreferOptionThenEnvThenParameter()
  {
    ResolvedDeployParameter parameter = new("ingress-port", "9200", "AppHost user secret");
    DeployCompose.ChooseComposePort("7000", "9100", parameter).ShouldBe(("7000", "--port"));
    DeployCompose.ChooseComposePort(null, "9100", parameter).Port.ShouldBe("9100");
    DeployCompose.ChooseComposePort(null, null, parameter).ShouldBe(("9200", "ingress-port from the AppHost user secret"));
    DeployCompose.ChooseComposePort(null, null, null).ShouldBe((null, null));
    return Task.CompletedTask;
  }

  public static Task PublishedEnv_Should_TreatTheBlankIngressPortAsUnset()
  {
    DeployCompose.ParseEnvValue(Env, "INGRESS_PORT").ShouldBeNull();
    DeployCompose.ParseEnvValue("INGRESS_PORT=\"9300\"\n", "INGRESS_PORT").ShouldBe("9300");
    DeployCompose.ParseEnvValue("# INGRESS_PORT=1\n", "INGRESS_PORT").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task ComposeLs_Should_Parse()
  {
    ComposeProject[] projects = DeployCompose.ParseComposeProjects(
      """[{"Name":"aspire-abc","Status":"running(5)","ConfigFiles":"/repo/aspire-output/docker-compose.yaml"},{"Name":"other","Status":"running(1)","ConfigFiles":"/x/a.yaml,/x/b.yaml"}]""");
    projects.Length.ShouldBe(2);
    projects[0].Name.ShouldBe("aspire-abc");
    projects[1].ConfigFiles.ShouldBe(["/x/a.yaml", "/x/b.yaml"]);
    DeployCompose.ParseComposeProjects("garbage").ShouldBeEmpty();
    DeployCompose.BuildComposeListArguments().ShouldBe(["compose", "ls", "--format", "json"]);
    return Task.CompletedTask;
  }

  public static Task ProjectChoice_Should_PreferTheNamedThenTheCheckoutsThenTheOnlyOne()
  {
    ComposeProject mine = new("aspire-abc", "running(5)", ["/repo/aspire-output/docker-compose.yaml"]);
    ComposeProject other = new("other", "running(1)", ["/elsewhere/compose.yaml"]);

    DeployCompose.ChooseComposeProject([mine, other], "other", "/repo", "podman").Project.ShouldBe(other);
    DeployCompose.ChooseComposeProject([mine, other], null, "/repo", "podman").Project.ShouldBe(mine);
    DeployCompose.ChooseComposeProject([other], null, "/repo", "podman").Project.ShouldBe(other);
    DeployCompose.ChooseComposeProject([mine], null, "/repo-two", "podman").Project.ShouldBe(mine);
    return Task.CompletedTask;
  }

  public static Task ProjectChoice_Should_RefuseWithTheCandidates()
  {
    ComposeProject first = new("one", "running(1)", ["/a/compose.yaml"]);
    ComposeProject second = new("two", "running(1)", ["/b/compose.yaml"]);

    ComposeProjectChoice none = DeployCompose.ChooseComposeProject([], null, "/repo", "podman");
    none.Project.ShouldBeNull();
    none.Refusal.ShouldNotBeNull().ShouldContain("dev deploy --target compose");
    none.Refusal.ShouldContain("podman compose ls");

    ComposeProjectChoice several = DeployCompose.ChooseComposeProject([first, second], null, "/repo", "docker");
    several.Refusal.ShouldNotBeNull().ShouldContain("--project-name <name>");
    several.Refusal.ShouldContain("one, two");

    DeployCompose.ChooseComposeProject([first], "missing", "/repo", "docker").Refusal.ShouldNotBeNull().ShouldContain("No running Compose project named 'missing'");
    return Task.CompletedTask;
  }

  public static Task Migrate_Should_ExecPsqlInThePostgresServiceThroughTheRuntime()
  {
    ComposeProject project = new("aspire-abc", "running(5)", ["/repo/aspire-output/docker-compose.yaml"]);
    DeployCompose.BuildComposeMigrateArguments(project, DeployMigrationHistory.PsqlQuietCommand).ShouldBe(
    [
      "compose", "--project-name", "aspire-abc", "--file", "/repo/aspire-output/docker-compose.yaml",
      "exec", "-T", "postgres", "sh", "-c",
      "PGPASSWORD=\"$POSTGRES_PASSWORD\" PGOPTIONS='-c client_min_messages=warning' psql -U postgres -d postgres-db -q -v ON_ERROR_STOP=1",
    ]);
    DeployCompose.BuildComposeMigrateArguments(project, DeployMigrationHistory.PsqlHistoryCommand)[^1].ShouldContain("-At ");
    DeployCompose.BuildComposeMigrateArguments(project, DeployMigrationHistory.PsqlHistoryCommand)[^1].ShouldContain("FROM \"__EFMigrationsHistory\"");

    // The runtime is the executable the command chooses (ASPIRE_CONTAINER_RUNTIME); the arguments never name one.
    DeployCompose.BuildComposeMigrateArguments(project, DeployMigrationHistory.PsqlQuietCommand).ShouldNotContain("docker");
    DeployOperate.MigrationArtifact(AspireDeploy.Compose).ShouldBe("efmigrations/web-migrations.sql");
    return Task.CompletedTask;
  }
}
