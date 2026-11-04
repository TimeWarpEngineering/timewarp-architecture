#region Purpose
// AcaPublish_Given_ helpers: the in-proc publish and the text-level Bicep parsing (container apps,
// env entries, secrets, @secure() parameters, bracket-matched blocks) the facts read. Design: aca-publish-tests.cs.
#endregion

namespace Aspire.Tests;

public partial class AcaPublish_Given_
{
  private static async Task PublishInProcAsync(string outputDirectory)
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--operation", "publish", "--step", "publish-azure-environment", "--output-path", outputDirectory, "--Publish:Target=aca", SigNozUrlArgument],
        // The testing builder defaults to Development; `aspire publish` defaults to Production.
        (_, settings) => settings.EnvironmentName = "Production");

    await using DistributedApplication app = await appHost.BuildAsync();
    await app.RunAsync();
  }

  private static IEnumerable<ContainerApp> ParseContainerApps(string module, string text)
  {
    foreach (Match match in ContainerAppDeclaration().Matches(text))
    {
      string body = Block(text, text.IndexOf('{', match.Index, StringComparison.Ordinal));
      List<EnvironmentEntry> environment = [.. ArrayItems(body, "env").Select(item =>
        Field(item, "secretRef") is { } secretRef
          ? new EnvironmentEntry(Unquote(Field(item, "name")), secretRef, IsSecretRef: true)
          : new EnvironmentEntry(Unquote(Field(item, "name")), Field(item, "value") ?? "", IsSecretRef: false))];
      Match ingress = IngressDeclaration().Match(body);
      string? ingressBlock = ingress.Success ? Block(body, ingress.Index + ingress.Length - 1) : null;
      yield return new ContainerApp(match.Groups["name"].Value, module, body, ingressBlock, environment);
    }
  }

  // Secrets whose name or value mentions postgres (the Key Vault reference, the password parameter,
  // the server host output), keyed by secret name.
  private static Dictionary<string, string> PostgresSecrets(ContainerApp app) =>
    ArrayItems(app.Text, "secrets")
      .Where(secret => secret.Contains("postgres", StringComparison.OrdinalIgnoreCase))
      .ToDictionary(secret => Unquote(Field(secret, "name")));

  private static EnvironmentEntry Entry(ContainerApp app, string name) =>
    app.Environment.Single(entry => entry.Name == name);

  private static bool IsSecretShaped(string name) =>
    name.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase)
    || name.Contains("SECRET", StringComparison.OrdinalIgnoreCase)
    || name.StartsWith("ConnectionStrings", StringComparison.OrdinalIgnoreCase)
    || name.EndsWith("_URI", StringComparison.OrdinalIgnoreCase);

  private static bool IsStringLiteral(string expression) =>
    expression.StartsWith('\'', StringComparison.Ordinal) && !expression.Contains("${", StringComparison.Ordinal);

  // Identifiers an expression reads: inside each `${…}` of a string, or the bare expression. Function
  // names (followed by '(') are not identifiers.
  private static List<string> ParameterNames(string expression)
  {
    IEnumerable<string> code = expression.StartsWith('\'', StringComparison.Ordinal)
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

  // Ingress: the app's `ingress: { … }` block, or null when it declares none.
  private sealed record ContainerApp(string Name, string Module, string Text, string? Ingress, List<EnvironmentEntry> Environment);

  private sealed record EnvironmentEntry(string Name, string Expression, bool IsSecretRef);

  [GeneratedRegex(@"^resource\s+\w+\s+'Microsoft\.App/containerApps@[^']+'\s*=\s*\{\s*name:\s*'(?<name>[^']+)'", RegexOptions.Multiline)]
  private static partial Regex ContainerAppDeclaration();

  [GeneratedRegex(@"\bingress:\s*\{")]
  private static partial Regex IngressDeclaration();

  [GeneratedRegex(@"^resource\s+\w+\s+'Microsoft\.DBforPostgreSQL/flexibleServers/firewallRules@[^']+'\s*=\s*\{", RegexOptions.Multiline)]
  private static partial Regex FirewallRuleDeclaration();

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
