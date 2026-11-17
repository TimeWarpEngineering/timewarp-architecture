#region Purpose
// AcaPublish_Given_ facts over secrets and data: secrets stay in container-app secrets, Microsoft365
// comes from a parameter, ACA scale and Postgres shape, the pinned postgres secrets and firewall,
// the passkey host, and that run mode carries no Azure wiring. Setup and the rule set: aca-publish-tests.cs.
#endregion

namespace Aspire.Tests;

public partial class AcaPublish_Given_
{
  public static async Task Publish_Should_KeepSecretsInSecrets()
  {
    foreach (ContainerApp app in ContainerApps)
    {
      // A secret-shaped env var reads a container-app secret; it never carries a value inline.
      foreach (EnvironmentEntry entry in app.Environment.Where(entry => IsSecretShaped(entry.Name)))
      {
        entry.IsSecretRef.ShouldBeTrue($"{app.Name}:{entry.Name} is an inline value, not a secretRef");
      }

      // Every container-app secret is a Key Vault reference or built from @secure() parameters.
      foreach (string secret in ArrayItems(app.Text, "secrets"))
      {
        string name = Unquote(Field(secret, "name"));
        if (secret.Contains("keyVaultUrl:", StringComparison.Ordinal))
        {
          continue;
        }

        string value = Field(secret, "value").ShouldNotBeNull($"{app.Name} secret '{name}' has neither keyVaultUrl nor value");
        IsStringLiteral(value).ShouldBeFalse($"{app.Name} secret '{name}' is a literal");
        ParameterNames(value).ShouldNotBeEmpty($"{app.Name} secret '{name}' does not come from a parameter");
        foreach (string parameter in ParameterNames(value).Where(IsSecretShaped))
        {
          SecureParameters(Modules[app.Module]).ShouldContain(parameter, $"{app.Name} secret '{name}' reads non-@secure() parameter '{parameter}'");
        }
      }
    }

    // Every secret-shaped parameter in every module is @secure() and ships no default.
    foreach ((string module, string text) in Modules)
    {
      foreach (Match parameter in ParameterDeclaration().Matches(text))
      {
        string name = parameter.Groups["name"].Value;
        if (!IsSecretShaped(name))
        {
          continue;
        }

        parameter.Groups["secure"].Success.ShouldBeTrue($"{module} parameter '{name}' is not @secure()");
        parameter.Groups["default"].Success.ShouldBeFalse($"{module} parameter '{name}' ships a default value");
      }

      LiteralCredential().IsMatch(text).ShouldBeFalse($"{module} carries a literal credential");
    }

    if (ContainerApps.SingleOrDefault(app => app.Name == WebServerApp) is { } webServer)
    {
      Entry(webServer, "Authentication__Entra__ClientSecret").IsSecretRef.ShouldBeTrue();
    }

    await Task.CompletedTask;
  }

  public static Task Publish_Should_AppendMicrosoft365FromAParameter()
  {
    if (ContainerApps.SingleOrDefault(app => app.Name == WebServerApp) is not { } webServer)
    {
      return Task.CompletedTask;
    }

    EnvironmentEntry entry = Entry(webServer, "SignInOptions__IncludeMicrosoft365");
    entry.IsSecretRef.ShouldBeFalse();
    IsStringLiteral(entry.Expression).ShouldBeFalse(entry.Expression);
    entry.Expression.ShouldNotContain("Passkey");
    ParameterNames(entry.Expression).ShouldNotBeEmpty(entry.Expression);
    webServer.Environment.ShouldNotContain(item => item.Name == "SignInOptions__Methods__0");
    webServer.Environment.ShouldNotContain(item => item.Name == "SignInOptions__Methods__1");
    return Task.CompletedTask;
  }

  public static Task Publish_Should_KeepAcaScaleAndPostgres()
  {
    string all = string.Join('\n', Modules.Values);
    all.ShouldNotContain("minReplicas: 0");
    all.ShouldNotContain("neon-pooled-connection-string");
    all.ShouldNotContain("neon-direct-connection-string");
    all.ShouldNotContain("Postgres__RetryCount");
    all.ShouldNotContain("Postgres__RetryDelaySeconds");
#if web
#if postgres
    all.ShouldContain("Microsoft.DBforPostgreSQL/flexibleServers");
    all.ShouldContain("Microsoft.KeyVault/vaults");
#endif
#endif
    return Task.CompletedTask;
  }

  public static async Task Publish_Should_UseAFlexibleServerForPostgres()
  {
    // No container app runs postgres, and nothing mounts an Azure Files share, whatever the flags.
    ContainerApps.ShouldNotContain(app => app.Name == PostgresModule);
    foreach ((string module, string text) in Modules)
    {
      text.ShouldNotContain("Microsoft.App/managedEnvironments/storages", Case.Sensitive, $"{module} declares an Azure Files share");
    }

    string postgresModule = $"{PostgresModule}/{PostgresModule}.bicep";
    if (!Modules.TryGetValue(postgresModule, out string? postgres))
    {
      return;
    }

    postgres.ShouldContain("'Microsoft.DBforPostgreSQL/flexibleServers@");
    postgres.ShouldContain("passwordAuth: 'Enabled'");
    SecureParameters(postgres).ShouldContain("administratorLoginPassword");
    postgres.ShouldContain("name: 'postgres-db'");
    postgres.ShouldContain("name: 'connectionstrings--postgres-db'");
    postgres.ShouldContain("'Microsoft.KeyVault/vaults/secrets@");

    // web-server reads the connection string from that Key Vault secret, not from an inline value.
    ContainerApp webServer = ContainerApps.Single(app => app.Name == WebServerApp);
    EnvironmentEntry connectionString = Entry(webServer, "ConnectionStrings__postgres-db");
    connectionString.IsSecretRef.ShouldBeTrue();
    string secret = ArrayItems(webServer.Text, "secrets").Single(item => Field(item, "name") == connectionString.Expression);
    secret.ShouldContain("keyVaultUrl:");

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_PinThePostgresSecrets()
  {
    // Key Vault holds the connection string, but Aspire's WithReference also hands web-server the
    // password as two container-app secrets built from the @secure() parameter. Pin that set: a new
    // copy (or a copy on another app) must be a deliberate change, not a side effect of a refactor.
    if (!Modules.ContainsKey($"{PostgresModule}/{PostgresModule}.bicep"))
    {
      return;
    }

    foreach (ContainerApp app in ContainerApps.Where(app => app.Name != WebServerApp))
    {
      PostgresSecrets(app).ShouldBeEmpty($"container app '{app.Name}' carries postgres-derived secrets");
    }

    ContainerApp webServer = ContainerApps.Single(app => app.Name == WebServerApp);
    Dictionary<string, string> secrets = PostgresSecrets(webServer);
    string.Join(", ", secrets.Keys.Order(StringComparer.Ordinal)).ShouldBe(
      "connectionstrings--postgres-db, postgres-db-password, postgres-db-uri",
      "web-server's postgres-derived secrets changed");
    secrets["connectionstrings--postgres-db"].ShouldContain("keyVaultUrl:");
    foreach (string name in new[] { "postgres-db-password", "postgres-db-uri" })
    {
      string value = Field(secrets[name], "value").ShouldNotBeNull($"web-server secret '{name}' has no value");
      ParameterNames(value).ShouldContain("postgres_password_value", $"web-server secret '{name}' is not built from the password parameter");
      SecureParameters(Modules[webServer.Module]).ShouldContain("postgres_password_value");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_PinTheFlexibleServerFirewall()
  {
    string postgresModule = $"{PostgresModule}/{PostgresModule}.bicep";
    if (!Modules.ContainsKey(postgresModule))
    {
      return;
    }

    // Every firewall rule in every module: exactly Aspire's AllowAllAzureIps (0.0.0.0–0.0.0.0, which
    // Azure reads as "any Azure-hosted IP"). Anything else — an operator range left in code, or
    // 0.0.0.0–255.255.255.255 — opens the server wider than the password-only posture the skill documents.
    List<(string Module, string Name, string? Start, string? End)> rules = [.. Modules.SelectMany(module =>
      FirewallRuleDeclaration().Matches(module.Value).Select(match =>
      {
        string block = Block(module.Value, module.Value.IndexOf('{', match.Index, StringComparison.Ordinal));
        return (module.Key, Unquote(Field(block, "name")), Field(block, "startIpAddress"), Field(block, "endIpAddress"));
      }))];

    // Shape-independent backstop: a rule declared any other way (a `[for …]` loop, nested under its
    // server, a module output) still names the firewallRules type or carries an IP range, so exactly
    // one occurrence of each may exist across all modules — the declaration parsed above.
    Modules.Values.Sum(text => text.Split("firewallRules").Length - 1).ShouldBe(1, "firewall rules declared outside the parsed top-level shape");
    Modules.Values.Sum(text => text.Split("startIpAddress").Length - 1).ShouldBe(1, "IP ranges declared outside the parsed top-level shape");

    rules.ShouldHaveSingleItem($"firewall rules: {string.Join("; ", rules)}");
    (string module, string name, string? start, string? end) = rules[0];
    module.ShouldBe(postgresModule);
    name.ShouldBe("AllowAllAzureIps");
    start.ShouldBe("'0.0.0.0'", $"firewall rule '{name}' starts at {start}");
    end.ShouldBe("'0.0.0.0'", $"firewall rule '{name}' ends at {end}");

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_AllowTheIngressHostForPasskeys()
  {
    // The public host is ingress.<environment domain>. The domain is an output of the environment,
    // so web-server does not reference the ingress container app (that pairing cycles).
#if web
#if yarp
    ContainerApp webServer = ContainerApps.Single(app => app.Name == WebServerApp);
    webServer.Text.ShouldContain("WebAuthnOptions__AllowedRpIds__0");
    webServer.Text.ShouldContain("ingress.${aca_env_outputs_azure_container_apps_environment_default_domain}");
#endif
#endif
    await Task.CompletedTask;
  }

  public static async Task RunMode_Should_CarryNoAzureWiring()
  {
    // Publish-only: the dev loop's model has no Azure resource at all — postgres stays the container
    // (`dev run`, aspire-tests) even when the configuration asks for the aca target.
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(["--Publish:Target=aca"]);

    appHost.Resources.ShouldNotContain(
      resource => resource.GetType().Namespace!.StartsWith("Aspire.Hosting.Azure", StringComparison.Ordinal),
      "run mode declared an Azure resource");
    if (appHost.Resources.SingleOrDefault(resource => resource.Name == PostgresModule) is { } postgres)
    {
      // By name: a template without the postgres flag has no Aspire.Hosting.PostgreSQL reference.
      postgres.GetType().Name.ShouldBe("PostgresServerResource");
    }
  }
}
