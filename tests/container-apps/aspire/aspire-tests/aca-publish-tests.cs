#region Purpose
// Production-safety guard over the Azure Container Apps publish output (task 070-007): runs the
// AppHost's publish pipeline with Publish:Target=aca and inspects the generated Bicep.
#endregion

#region Design
// Same two-source shape as compose-publish-tests and kubernetes-publish-tests. By default SetupOnce
// runs the publish pipeline in-proc (step publish-azure-environment, Production environment) into a
// temp directory. Publishing Azure only WRITES Bicep — no images, no Azure credentials, no
// subscription, no AppHost run — so the suite runs on any CI runner. When TIMEWARP_ACA_OUTPUT names a
// directory, the facts inspect THAT output instead: `dev publish aca` runs the real `aspire publish`
// CLI and points this suite at its output, so CI checks the exact files it uploads.
// Bicep is read as text, not compiled: the bicep CLI is not guaranteed on a runner, and the shapes
// Aspire emits are regular (one `resource ... 'Microsoft.App/containerApps@…'` per compute module,
// `env: [ { name: '…' value|secretRef: … } ]`, `secrets: [ … ]`, `@secure() param …`). Arrays are
// sliced by bracket matching outside quoted strings.
// Rule set = the Compose/Kubernetes rules plus the two ACA-specific ones:
//   - only the YARP ingress container app has external ingress (none at all without the yarp flag);
//   - no Aspire dashboard: Aspire emits it as a managedEnvironments/dotNetComponents resource of
//     componentType AspireDashboard on a public URL unless WithDashboard(false);
//   - no mock auth; no app runs as Development/Testing (browser-log forwarding and the REPL are
//     Development-only);
//   - secrets: every secret-shaped env var reads a container-app secret (secretRef), every secret is a
//     Key Vault reference or a @secure() parameter (never a literal), and every secret-shaped
//     parameter is @secure() with no default;
//   - Postgres is an Azure Database for PostgreSQL Flexible Server with password auth whose
//     connection string web-server reads from Key Vault — never a postgres container app, and no
//     Azure Files share (managedEnvironments/storages) anywhere.
// Every rule is conditional on what the template flags emitted (no web/postgres → their rules skip).
#endregion

namespace Aspire.Tests;

[TestTag("Integration")]
public partial class AcaPublish_Given_
{
  internal const string OutputEnvironmentVariable = "TIMEWARP_ACA_OUTPUT";
  private const string IngressApp = "ingress";
  private const string WebServerApp = "web-server";
  private const string PostgresModule = "postgres";

  private static string OutputDirectory = null!;
  private static bool OwnsOutputDirectory;
  private static Dictionary<string, string> Modules = null!;
  private static List<ContainerApp> ContainerApps = null!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AcaPublish_Given_>();

  public static async Task SetupOnce()
  {
    string? provided = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
    if (string.IsNullOrWhiteSpace(provided))
    {
      OutputDirectory = Directory.CreateTempSubdirectory("aca-publish-").FullName;
      OwnsOutputDirectory = true;
      await PublishInProcAsync(OutputDirectory);
    }
    else
    {
      OutputDirectory = Path.GetFullPath(provided);
    }

    File.Exists(Path.Combine(OutputDirectory, "main.bicep")).ShouldBeTrue($"publish produced no main.bicep in {OutputDirectory}");
    Modules = Directory.EnumerateFiles(OutputDirectory, "*.bicep", SearchOption.AllDirectories)
      .ToDictionary(path => Path.GetRelativePath(OutputDirectory, path).Replace('\\', '/'), File.ReadAllText);
    ContainerApps = [.. Modules.SelectMany(module => ParseContainerApps(module.Key, module.Value))];
    ContainerApps.ShouldNotBeEmpty();
  }

  public static Task CleanUpOnce()
  {
    if (OwnsOutputDirectory && Directory.Exists(OutputDirectory))
    {
      Directory.Delete(OutputDirectory, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static async Task Publish_Should_OnlyExposeTheIngressOutsideTheEnvironment()
  {
    bool hasIngress = ContainerApps.Any(app => app.Name == IngressApp);
    foreach (ContainerApp app in ContainerApps)
    {
      bool expected = hasIngress && app.Name == IngressApp;
      app.External.ShouldBe(expected, $"container app '{app.Name}' external ingress is {app.External}; only the YARP ingress may be external");
      app.Text.ShouldNotContain("additionalPortMappings", Case.Sensitive, $"container app '{app.Name}' opens extra ports");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_HaveNoDashboard()
  {
    foreach ((string module, string text) in Modules)
    {
      text.ShouldNotContain("dotNetComponents", Case.Insensitive, $"{module} declares a dotNetComponents resource (the Aspire dashboard)");
      text.ShouldNotContain("AspireDashboard", Case.Insensitive, $"{module} declares the Aspire dashboard");
    }

    ContainerApps.ShouldNotContain(app => app.Name.Contains("dashboard", StringComparison.OrdinalIgnoreCase));

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_NeverEnableMockAuth()
  {
    foreach ((string module, string text) in Modules)
    {
      text.ShouldNotContain("UseMock", Case.Insensitive, $"{module} carries a mock-auth setting");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_NotRunAnyAppAsDevelopmentOrTesting()
  {
    // Browser-log forwarding and the Postgres REPL are Development(/Testing)-only; a published app
    // running as either environment would switch them back on.
    foreach (ContainerApp app in ContainerApps)
    {
      foreach (EnvironmentEntry entry in app.Environment.Where(entry => entry.Name is "ASPNETCORE_ENVIRONMENT" or "DOTNET_ENVIRONMENT"))
      {
        new[] { "'Development'", "'Testing'" }.ShouldNotContain(
          environment => string.Equals(environment, entry.Expression, StringComparison.OrdinalIgnoreCase), $"{app.Name}:{entry.Name}={entry.Expression}");
      }
    }

    await Task.CompletedTask;
  }

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
        foreach (string parameter in ParameterNames(value).Where(parameter => IsSecretShaped(parameter)))
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

  private static async Task PublishInProcAsync(string outputDirectory)
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--operation", "publish", "--step", "publish-azure-environment", "--output-path", outputDirectory, "--Publish:Target=aca"],
        // The testing builder defaults to Development; `aspire publish` defaults to Production.
        (_, settings) => settings.EnvironmentName = "Production");

    await using DistributedApplication app = await appHost.BuildAsync();
    await app.RunAsync();
  }

  private static IEnumerable<ContainerApp> ParseContainerApps(string module, string text)
  {
    foreach (Match match in ContainerAppDeclaration().Matches(text))
    {
      string body = Block(text, text.IndexOf('{', match.Index));
      List<EnvironmentEntry> environment = [.. ArrayItems(body, "env").Select(item =>
        Field(item, "secretRef") is { } secretRef
          ? new EnvironmentEntry(Unquote(Field(item, "name")), secretRef, IsSecretRef: true)
          : new EnvironmentEntry(Unquote(Field(item, "name")), Field(item, "value") ?? "", IsSecretRef: false))];
      bool external = ExternalIngress().Match(body) is { Success: true } ingress && ingress.Groups[1].Value == "true";
      yield return new ContainerApp(match.Groups["name"].Value, module, body, external, environment);
    }
  }

  private static EnvironmentEntry Entry(ContainerApp app, string name) =>
    app.Environment.Single(entry => entry.Name == name);

  private static bool IsSecretShaped(string name) =>
    name.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase)
    || name.Contains("SECRET", StringComparison.OrdinalIgnoreCase)
    || name.StartsWith("ConnectionStrings", StringComparison.OrdinalIgnoreCase)
    || name.EndsWith("_URI", StringComparison.OrdinalIgnoreCase);

  private static bool IsStringLiteral(string expression) =>
    expression.StartsWith('\'') && !expression.Contains("${", StringComparison.Ordinal);

  // Identifiers an expression reads: inside each `${…}` of a string, or the bare expression. Function
  // names (followed by '(') are not identifiers.
  private static List<string> ParameterNames(string expression)
  {
    IEnumerable<string> code = expression.StartsWith('\'')
      ? Interpolation().Matches(expression).Select(match => match.Groups[1].Value)
      : [expression];
    return [.. code.SelectMany(part => Identifier().Matches(part).Select(match => match.Value))];
  }

  private static string Unquote(string? expression) => (expression ?? "").Trim('\'');

  private static HashSet<string> SecureParameters(string text) =>
    [.. ParameterDeclaration().Matches(text).Where(match => match.Groups["secure"].Success).Select(match => match.Groups["name"].Value)];

  // `key: <expression>` on one line of a Bicep object, or null.
  private static string? Field(string item, string key) =>
    Regex.Match(item, $@"^\s*{Regex.Escape(key)}:\s*(.+?)\s*$", RegexOptions.Multiline) is { Success: true } match ? match.Groups[1].Value : null;

  // The `{ … }` items of the first `<key>: [ … ]` array in text.
  private static IEnumerable<string> ArrayItems(string text, string key)
  {
    Match array = Regex.Match(text, $@"\b{Regex.Escape(key)}:\s*\[");
    if (!array.Success)
    {
      yield break;
    }

    string body = Block(text, array.Index + array.Length - 1);
    int index = 1;
    while ((index = IndexOutsideQuotes(body, '{', index)) >= 0)
    {
      string item = Block(body, index);
      yield return item;
      index += item.Length;
    }
  }

  // The bracketed block opening at text[start] ('{' or '['), including its closing bracket.
  private static string Block(string text, int start)
  {
    int depth = 0;
    bool quoted = false;
    for (int index = start; index < text.Length; index++)
    {
      char character = text[index];
      if (character == '\'')
      {
        quoted = !quoted;
      }
      else if (!quoted && character is '{' or '[')
      {
        depth++;
      }
      else if (!quoted && character is '}' or ']' && --depth == 0)
      {
        return text[start..(index + 1)];
      }
    }

    throw new InvalidOperationException($"unbalanced Bicep block at {start}");
  }

  private static int IndexOutsideQuotes(string text, char target, int start)
  {
    bool quoted = false;
    for (int index = start; index < text.Length; index++)
    {
      if (text[index] == '\'')
      {
        quoted = !quoted;
      }
      else if (!quoted && text[index] == target)
      {
        return index;
      }
    }

    return -1;
  }

  private sealed record ContainerApp(string Name, string Module, string Text, bool External, List<EnvironmentEntry> Environment);

  private sealed record EnvironmentEntry(string Name, string Expression, bool IsSecretRef);

  [GeneratedRegex(@"^resource\s+\w+\s+'Microsoft\.App/containerApps@[^']+'\s*=\s*\{\s*name:\s*'(?<name>[^']+)'", RegexOptions.Multiline)]
  private static partial Regex ContainerAppDeclaration();

  [GeneratedRegex(@"\bingress:\s*\{\s*external:\s*(true|false)")]
  private static partial Regex ExternalIngress();

  [GeneratedRegex(@"(?<secure>@secure\(\)\s*)?^param\s+(?<name>\w+)\s+\w+(?<default>\s*=)?", RegexOptions.Multiline)]
  private static partial Regex ParameterDeclaration();

  [GeneratedRegex(@"(?<![\w.])[A-Za-z_]\w*(?![\w(])")]
  private static partial Regex Identifier();

  [GeneratedRegex(@"\$\{([^}]*)\}")]
  private static partial Regex Interpolation();

  // Password=<literal> or scheme://user:<literal>@host — anything not interpolated from a parameter.
  [GeneratedRegex(@"Password=(?!\$\{)[^;'\s]+|://[^/:@\s']+:(?!\$\{)[^@\s']+@", RegexOptions.IgnoreCase)]
  private static partial Regex LiteralCredential();
}
