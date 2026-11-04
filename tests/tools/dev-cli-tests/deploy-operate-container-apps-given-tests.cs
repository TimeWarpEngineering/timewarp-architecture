#region Purpose
// Gates the aca per-target command construction (bundle + firewall rule) for `dev open` / `dev deploy migrate`.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public class ContainerApps_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ContainerApps_Given_>();

  private const string Subscription = "00000000-0000-0000-0000-000000000001";

  public static Task ResourceGroup_Should_PreferOptionThenEnvThenUserSecret()
  {
    DeployAca.ChooseResourceGroup("opt", "env", "secret").Name.ShouldBe("opt");
    DeployAca.ChooseResourceGroup(null, "env", "secret").Detail.ShouldContain("Azure__ResourceGroup environment variable");
    DeployAca.ChooseResourceGroup(" ", null, "secret").Detail.ShouldContain("AppHost user secret Azure:ResourceGroup");
    DeployAca.ChooseResourceGroup(null, null, null).Name.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task NoResourceGroup_Should_BeRefusedWithPwshCommands()
  {
    string message = DeployAca.NoResourceGroupMessage("dev open", "/repo/app-host.csproj");
    message.ShouldContain("dev open --target aca --resource-group <name>");
    message.ShouldContain("${env:Azure__ResourceGroup} = '<name>'");
    message.ShouldContain("dotnet user-secrets set 'Azure:ResourceGroup' '<name>' --project '/repo/app-host.csproj'");
    return Task.CompletedTask;
  }

  public static Task Open_Should_QueryTheIngressContainerAppsFqdn()
  {
    DeployAca.BuildIngressFqdnArguments("rg", Subscription).ShouldBe(
    [
      "containerapp", "show", "--name", "ingress", "--resource-group", "rg", "--subscription", Subscription,
      "--query", "properties.configuration.ingress.fqdn", "--output", "tsv",
    ]);
    DeployAca.ParseSingleValue(true, "ingress.happy.eastus.azurecontainerapps.io\n").ShouldBe("ingress.happy.eastus.azurecontainerapps.io");
    DeployAca.ParseSingleValue(false, "x").ShouldBeNull();
    DeployAca.ParseSingleValue(true, "a\nb").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Migrate_Should_BuildTheFirewallRuleAndBundleCommands()
  {
    DeployAca.BuildFirewallRuleCreateArguments("rg", "pg1", Subscription, "203.0.113.7").ShouldBe(
    [
      "postgres", "flexible-server", "firewall-rule", "create", "--resource-group", "rg", "--name", "pg1", "--subscription", Subscription,
      "--rule-name", "operator-migrate", "--start-ip-address", "203.0.113.7", "--end-ip-address", "203.0.113.7",
    ]);
    DeployAca.BuildFirewallRuleDeleteArguments("rg", "pg1", Subscription).ShouldBe(
    [
      "postgres", "flexible-server", "firewall-rule", "delete", "--resource-group", "rg", "--name", "pg1", "--subscription", Subscription,
      "--rule-name", "operator-migrate", "--yes",
    ]);
    DeployAca.BuildBundleArguments("Host=h;Password=p").ShouldBe(["--connection", "Host=h;Password=p"]);
    DeployAca.BuildKeyVaultListArguments("rg", Subscription).ShouldContain("[?tags.\"aspire-resource-name\"=='postgres-kv'].name");
    DeployAca.BuildConnectionStringArguments("kv1", Subscription).ShouldContain("connectionstrings--postgres-db");
    DeployOperate.MigrationArtifact(AspireDeploy.ContainerApps).ShouldBe("efmigrations/web-migrations");
    DeployOperate.MigrationArtifact(AspireDeploy.ContainerAppsNeon).ShouldBe("efmigrations/web-migrations");
    return Task.CompletedTask;
  }

  public static Task PrintedArguments_Should_NeverShowTheConnectionString()
  {
    string[] printed = DeployAca.RedactConnection(DeployAca.BuildBundleArguments("Host=h;Password=hunter2"));
    printed.ShouldBe(["--connection", "<connection string from Key Vault>"]);
    string.Join(' ', printed).ShouldNotContain("hunter2");
    string[] neon = DeployAca.RedactConnection(
      DeployAca.BuildBundleArguments("Host=ep-example;Password=hunter2"), DeployAca.NeonDirectConnectionPlaceholder);
    neon.ShouldBe(["--connection", "<direct Neon connection string>"]);
    string.Join(' ', neon).ShouldNotContain("hunter2");
    string.Join(' ', neon).ShouldNotContain("ep-example");
    return Task.CompletedTask;
  }

  public static Task ClientIp_Should_PreferTheOptionAndRequireIpv4()
  {
    DeployAca.ChooseClientIp("198.51.100.4", "203.0.113.7").ShouldBe("198.51.100.4");
    DeployAca.ChooseClientIp(null, "203.0.113.7\n").ShouldBe("203.0.113.7");
    DeployAca.ChooseClientIp(null, "<html>").ShouldBeNull();
    DeployAca.ChooseClientIp("2001:db8::1", null).ShouldBeNull();
    DeployAca.NoClientIpMessage(null).ShouldContain("--client-ip <ipv4>");
    DeployAca.NoClientIpMessage("nope").ShouldContain("not an IPv4 address");
    return Task.CompletedTask;
  }

  public static Task Refusals_Should_NameTheFix()
  {
    DeployAca.NoFlexibleServerMessage("rg", 0).ShouldContain("dev deploy --target aca");
    DeployAca.NoFlexibleServerMessage("rg", 2).ShouldContain("2 PostgreSQL Flexible Servers");
    string role = DeployAca.ConnectionStringUnreadableMessage("rg", "kv1");
    role.ShouldContain("Cannot read the Key Vault secret connectionstrings--postgres-db in kv1");
    role.ShouldContain("az role assignment create --assignee (az ad signed-in-user show --query id --output tsv) --role 'Key Vault Secrets User'");
    DeployAca.ConnectionStringUnreadableMessage("rg", null).ShouldContain("No Key Vault tagged aspire-resource-name=postgres-kv");
    // pwsh: `" escapes a double quote inside a double-quoted string; \" would end it.
    role.ShouldContain("--query \"[?tags.`\"aspire-resource-name`\"=='postgres-kv'].name\"");
    role.ShouldNotContain("\\\"");
    return Task.CompletedTask;
  }

  public static Task PrintedMigrateCommand_Should_BePwsh()
  {
    string display = DeployOperate.BuildPipedCommandDisplay(
      "/repo/artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql",
      "kubectl",
      DeployKubernetes.BuildKubectlMigrateArguments("kind-x", "ns", DeployMigrationHistory.PsqlQuietCommand));
    display.ShouldStartWith("Get-Content -Raw /repo/artifacts/aspire-output/kubernetes/efmigrations/web-migrations.sql | kubectl --context kind-x exec -i");
    display.ShouldEndWith("-- sh -c 'PGPASSWORD=\"$POSTGRES_PASSWORD\" PGOPTIONS=''-c client_min_messages=warning'' psql -U postgres -d postgres-db -q -v ON_ERROR_STOP=1'");
    display.ShouldNotContain(" < ");
    DeployOperate.PwshQuote("it's here").ShouldBe("'it''s here'");
    DeployOperate.PwshQuote("--file").ShouldBe("--file");
    return Task.CompletedTask;
  }
}
