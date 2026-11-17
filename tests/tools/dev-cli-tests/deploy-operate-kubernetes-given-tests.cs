#region Purpose
// Gates the kubernetes per-target command construction (port-forward, exec + script) and the LoadBalancer-vs-forward decision.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public class Kubernetes_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Kubernetes_Given_>();

  private const string ClusterIpService = """{ "spec": { "type": "NodePort" }, "status": { "loadBalancer": {} } }""";
  private const string PendingLoadBalancer = """{ "spec": { "type": "LoadBalancer" }, "status": { "loadBalancer": {} } }""";
  private const string IpLoadBalancer = """{ "spec": { "type": "LoadBalancer" }, "status": { "loadBalancer": { "ingress": [ { "ip": "20.1.2.3" } ] } } }""";
  private const string HostnameLoadBalancer = """{ "spec": { "type": "LoadBalancer" }, "status": { "loadBalancer": { "ingress": [ { "hostname": "lb.example.com", "ip": "" } ] } } }""";

  public static Task PortForward_Should_TargetTheControllerServicePort80InTheContext()
  {
    DeployKubernetes.BuildPortForwardArguments("kind-app", "ingress-nginx", "ingress-nginx-controller", 36371)
      .ShouldBe(["--context", "kind-app", "port-forward", "--namespace", "ingress-nginx", "service/ingress-nginx-controller", "36371:80"]);
    DeployKubernetes.BuildControllerServiceArguments("kind-app", "edge", "traefik")
      .ShouldBe(["--context", "kind-app", "get", "service", "traefik", "--namespace", "edge", "--output", "json"]);
    return Task.CompletedTask;
  }

  public static Task Defaults_Should_BeTheKindRecipesController()
  {
    DeployKubernetes.IngressControllerNamespace.ShouldBe("ingress-nginx");
    DeployKubernetes.IngressControllerService.ShouldBe("ingress-nginx-controller");
    return Task.CompletedTask;
  }

  public static Task NoExternalAddress_Should_Forward()
  {
    foreach (string json in (string[])[ClusterIpService, PendingLoadBalancer, "not json", "[]"])
    {
      string? address = DeployKubernetes.ParseLoadBalancerAddress(json);
      address.ShouldBeNull(json);
      OpenPlan plan = DeployKubernetes.DecideKubernetesOpen(address, "ingress-nginx", "ingress-nginx-controller", 9090);
      plan.PortForward.ShouldBeTrue();
      plan.Url.ShouldBe("http://localhost:9090");
      plan.Detail.ShouldContain("Ctrl+C");
    }

    return Task.CompletedTask;
  }

  public static Task ExternalAddress_Should_OpenItWithoutForwarding()
  {
    OpenPlan ip = DeployKubernetes.DecideKubernetesOpen(DeployKubernetes.ParseLoadBalancerAddress(IpLoadBalancer), "ingress-nginx", "ingress-nginx-controller", 36371);
    ip.PortForward.ShouldBeFalse();
    ip.Url.ShouldBe("http://20.1.2.3");
    ip.Detail.ShouldContain("no port-forward");

    DeployKubernetes.DecideKubernetesOpen(DeployKubernetes.ParseLoadBalancerAddress(HostnameLoadBalancer), "n", "s", 36371).Url.ShouldBe("http://lb.example.com");
    DeployKubernetes.DecideKubernetesOpen("2001:db8::1", "n", "s", 36371).Url.ShouldBe("http://[2001:db8::1]");
    return Task.CompletedTask;
  }

  public static Task Migrate_Should_ExecPsqlInThePostgresStatefulSetWithStdin()
  {
    DeployKubernetes.BuildKubectlMigrateArguments("kind-app", "my-app", DeployMigrationHistory.PsqlScriptCommand(verbose: true)).ShouldBe(
    [
      "--context", "kind-app", "exec", "-i", "--namespace", "my-app", "statefulset/postgres-statefulset", "--", "sh", "-c",
      "PGPASSWORD=\"$POSTGRES_PASSWORD\" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1",
    ]);
    DeployKubernetes.BuildKubectlMigrateArguments("kind-app", "my-app", DeployMigrationHistory.PsqlScriptCommand(verbose: false))[^1].ShouldBe(
      "PGPASSWORD=\"$POSTGRES_PASSWORD\" PGOPTIONS='-c client_min_messages=warning' psql -U postgres -d postgres-db -q -v ON_ERROR_STOP=1");
    DeployKubernetes.BuildKubectlMigrateArguments("kind-app", "my-app", DeployMigrationHistory.PsqlHistoryCommand)[^1].ShouldBe(
      "PGPASSWORD=\"$POSTGRES_PASSWORD\" psql -U postgres -d postgres-db -At -v VERBOSITY=sqlstate "
      + "-c 'SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\"'");
    DeployOperate.MigrationArtifact(AspireDeploy.Kubernetes).ShouldBe("efmigrations/web-migrations.sql");
    DeployOperate.PublishedOutputDirectory(AspireDeploy.Kubernetes).ShouldBe("artifacts/aspire-output/kubernetes");
    return Task.CompletedTask;
  }

  public static Task Refusals_Should_NameTheFix()
  {
    DeployKubernetes.PortInUseMessage(36371).ShouldContain("--port <n>");
    string controller = DeployKubernetes.ControllerServiceMissingMessage("kind-app", "ingress-nginx", "ingress-nginx-controller", "NotFound");
    controller.ShouldContain("--controller-namespace <namespace> --controller-service <service>");
    controller.ShouldContain("kind recipe");
    return Task.CompletedTask;
  }
}
