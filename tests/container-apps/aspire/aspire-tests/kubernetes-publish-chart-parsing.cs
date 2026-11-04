#region Purpose
// KubernetesPublish_Given_ helpers: the in-proc publish and the Helm chart reading (YAML documents,
// values.yaml resolution, env entries, ConfigMap/Secret data, workloads) the facts read. Design: kubernetes-publish-tests.cs.
#endregion

namespace Aspire.Tests;

using YamlDotNet.RepresentationModel;

public partial class KubernetesPublish_Given_
{
  private static async Task PublishInProcAsync(string outputDirectory)
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--operation", "publish", "--step", "publish-k8s", "--output-path", outputDirectory, "--Publish:Target=kubernetes", SigNozUrlArgument],
        // The testing builder defaults to Development; `aspire publish` defaults to Production.
        (_, settings) => settings.EnvironmentName = "Production");

    await using DistributedApplication app = await appHost.BuildAsync();
    await app.RunAsync();
  }

  private static List<YamlMappingNode> LoadDocuments(string path)
  {
    var yaml = new YamlStream();
    yaml.Load(new StringReader(UnquotedTemplateExpression().Replace(File.ReadAllText(path), "0")));
    return [.. yaml.Documents.Select(document => document.RootNode).OfType<YamlMappingNode>()];
  }

  // Every container env source the chart defines: ConfigMap data, Secret stringData/data, and inline
  // container env values. Owner is "<Kind>/<name>"; values are resolved against values.yaml.
  private static IEnumerable<(string Owner, string Key, string Value)> EnvironmentEntries()
  {
    foreach (YamlMappingNode manifest in Manifests)
    {
      string kind = Scalar(manifest, "kind") ?? "";
      string owner = $"{kind}/{Name(manifest)}";
      if (kind is "ConfigMap" or "Secret")
      {
        foreach (string section in new[] { "data", "stringData" })
        {
          if (manifest.Children.TryGetValue(new YamlScalarNode(section), out YamlNode? data))
          {
            foreach ((YamlNode key, YamlNode value) in ((YamlMappingNode)data).Children)
            {
              yield return (owner, ((YamlScalarNode)key).Value!, Resolve(((YamlScalarNode)value).Value ?? ""));
            }
          }
        }
      }
    }

    foreach (YamlMappingNode workload in Workloads())
    {
      foreach (YamlMappingNode container in Containers(workload))
      {
        foreach (YamlMappingNode variable in Items(container, "env"))
        {
          yield return ($"{Scalar(workload, "kind")}/{Name(workload)}", Scalar(variable, "name") ?? "", Resolve(Scalar(variable, "value") ?? ""));
        }
      }
    }
  }

  private static bool IsSecretShaped(string key, string value) =>
    key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase)
    || key.Contains("SECRET", StringComparison.OrdinalIgnoreCase)
    || key.StartsWith("ConnectionStrings", StringComparison.OrdinalIgnoreCase)
    || key.EndsWith("_URI", StringComparison.OrdinalIgnoreCase)
    || value.Contains("Password=", StringComparison.OrdinalIgnoreCase)
    || UriCredential().IsMatch(value);

  private static Dictionary<string, string> ConfigData(string component) => Data("ConfigMap", component, "data");

  private static Dictionary<string, string> SecretData(string component) => Data("Secret", component, "stringData");

  private static Dictionary<string, string> Data(string kind, string component, string section) =>
    Mapping(OfKind(kind).Single(manifest => Component(manifest) == component), section).Children
      .ToDictionary(pair => ((YamlScalarNode)pair.Key).Value!, pair => ((YamlScalarNode)pair.Value).Value ?? "");

  // "{{ .Values.a.b }}" → values.yaml a.b (missing path → the expression itself); anything else as-is.
  private static string Resolve(string value)
  {
    Match match = ValuesReference().Match(value);
    if (!match.Success)
    {
      return value;
    }

    YamlNode node = Values;
    foreach (string segment in match.Groups[1].Value.Split('.'))
    {
      if (node is not YamlMappingNode mapping || !mapping.Children.TryGetValue(new YamlScalarNode(segment), out YamlNode? child))
      {
        return value;
      }

      node = child;
    }

    return node is YamlScalarNode scalar ? scalar.Value ?? "" : value;
  }

  private static IEnumerable<(string Path, string Value)> Leaves(YamlMappingNode? node, string path)
  {
    if (node is null)
    {
      yield break;
    }

    foreach ((YamlNode key, YamlNode value) in node.Children)
    {
      string childPath = $"{path}.{((YamlScalarNode)key).Value}";
      if (value is YamlMappingNode mapping)
      {
        foreach ((string Path, string Value) leaf in Leaves(mapping, childPath))
        {
          yield return leaf;
        }
      }
      else if (value is YamlScalarNode scalar)
      {
        yield return (childPath, scalar.Value ?? "");
      }
    }
  }

  private static IEnumerable<string> BackendServices(YamlMappingNode ingress)
  {
    YamlMappingNode spec = Mapping(ingress, "spec");
    if (spec.Children.TryGetValue(new YamlScalarNode("defaultBackend"), out YamlNode? defaultBackend))
    {
      yield return Scalar(Mapping((YamlMappingNode)defaultBackend, "service"), "name")!;
    }

    foreach (YamlMappingNode rule in Items(spec, "rules"))
    {
      if (rule.Children.TryGetValue(new YamlScalarNode("http"), out YamlNode? http))
      {
        foreach (YamlMappingNode path in Items((YamlMappingNode)http, "paths"))
        {
          yield return Scalar(Mapping(Mapping(path, "backend"), "service"), "name")!;
        }
      }
    }
  }

  private static List<YamlMappingNode> OfKind(string kind) =>
    [.. Manifests.Where(manifest => Scalar(manifest, "kind") == kind)];

  private static List<YamlMappingNode> Workloads() =>
    [.. Manifests.Where(manifest => Scalar(manifest, "kind") is "Deployment" or "StatefulSet" or "DaemonSet" or "Job" or "CronJob" or "Pod")];

  private static IEnumerable<YamlMappingNode> Containers(YamlMappingNode workload)
  {
    YamlMappingNode spec = Mapping(workload, "spec");
    YamlMappingNode podSpec = Scalar(workload, "kind") == "Pod" ? spec : Mapping(Mapping(spec, "template"), "spec");
    return Items(podSpec, "containers").Concat(Items(podSpec, "initContainers"));
  }

  private static string Name(YamlMappingNode manifest) => Scalar(Mapping(manifest, "metadata"), "name") ?? "";

  private static string? Component(YamlMappingNode manifest) =>
    Mapping(manifest, "metadata").Children.TryGetValue(new YamlScalarNode("labels"), out YamlNode? labels)
      ? Scalar((YamlMappingNode)labels, "app.kubernetes.io/component")
      : null;

  private static YamlMappingNode Mapping(YamlMappingNode node, string key) =>
    (YamlMappingNode)node.Children[new YamlScalarNode(key)];

  // A values.yaml section a flag combination may not emit (no secret parameters → no `secrets:`).
  private static YamlMappingNode? OptionalMapping(YamlMappingNode node, string key) =>
    node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) ? value as YamlMappingNode : null;

  private static IEnumerable<YamlMappingNode> Items(YamlMappingNode node, string key) =>
    node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value)
      ? ((YamlSequenceNode)value).Children.Cast<YamlMappingNode>()
      : [];

  private static string? Scalar(YamlMappingNode node, string key) =>
    node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) ? ((YamlScalarNode)value).Value : null;

  // A whole unquoted scalar that is a Helm expression (e.g. `containerPort: {{ .Values.x | int }}`) —
  // invalid YAML until rendered. Expressions inside quoted strings are left alone.
  [GeneratedRegex(@"(?<=(:|-)[ \t]+)\{\{[^}]*\}\}(?=[ \t]*$)", RegexOptions.Multiline)]
  private static partial Regex UnquotedTemplateExpression();

  [GeneratedRegex(@"^\{\{\s*\.Values\.([A-Za-z0-9_.\-]+)\s*\}\}$")]
  private static partial Regex ValuesReference();

  // scheme://user:password@host — a literal (non-template) password.
  [GeneratedRegex(@"://[^/:@\s]+:(?!\{\{)[^@\s]+@")]
  private static partial Regex UriCredential();
}
