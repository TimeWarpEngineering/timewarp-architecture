#region Purpose
// Generates the feature filename grammar outputs (analyzer .g.cs and one .g.props per family) from
// feature-filename-grammar.json.
#endregion

#region Design
// feature-filename-grammar.json is the registry (routed layers, unrouted layers, function→layer
// map). It is family-agnostic. This generator writes four checked-in files from it:
//   - the convention analyzers' feature-filename-grammar.g.cs (TWA0015/TWA0016 constants, plus the
//     Families constant the analyzer derives its per-family path markers from), once;
//   - source/container-apps/{family}/msbuild/feature-filename-grammar.g.props, once per family in
//     Families. Each carries the layer-suffix regex, layer and function items, and the hybrid
//     Compile globs for {Prefix}FeatureTreeRoot and {Prefix}PlatformTreeRoot.
// `dev regenerate-generated` runs it, and `dev workflow` fails on any diff afterwards. The build
// does not run it: a build that rewrites tracked files from checkout timestamps broke a runner
// that had no generator toolchain (task 519).
//
// Families is the one authored family list. Should_Keep_Grammar_Registry_In_Sync
// (tests/analyzers/timewarp-architecture-analyzers-tests) keeps its own copy and checks it against
// the .g.cs Families constant. yarp is a single-project family with no concern trees and is left out.
//
// Unrouted layers (for example "tests") are accepted by the regex and the analyzer's Layers set like
// a routed layer, get a layer item with no Project metadata, and get no Compile ItemGroup, so a
// co-located `-tests.cs` runfile is validated by the grammar and compiles into no layer project.
// Functions may map only to routed layers. A layer suffix that ends with another layer suffix is
// refused, because one file would match two layer globs.
//
// Output is LF with no BOM so the files do not change with the OS. Ordering: Layers keeps JSON
// order (routed, then unrouted not already routed); function items sort by name (ordinal);
// FunctionsLongestFirst and the regex alternation sort by length descending, then ordinal.
#endregion

namespace DevCli.Services;

using System.Text;
using System.Text.Json;

internal sealed record FeatureFilenameGrammarRegistry(
  IReadOnlyList<string> Layers,
  IReadOnlyList<string> UnroutedLayers,
  IReadOnlyList<KeyValuePair<string, string>> Functions)
{
  /// <summary>Routed layers, then unrouted layers not already routed (JSON order).</summary>
  internal IReadOnlyList<string> AllLayers =>
    [.. Layers, .. UnroutedLayers.Where(layer => !Layers.Contains(layer, StringComparer.Ordinal))];
}

internal sealed record FeatureFilenameGrammarOutput(string RelativePath, string Text);

internal static class FeatureFilenameGrammarGenerator
{
  internal const string JsonRelativePath =
    "source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json";

  internal const string CsRelativePath =
    "source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.g.cs";

  internal static readonly (string Prefix, string Family)[] Families =
  [
    ("Web", "web"),
    ("Api", "api"),
    ("Grpc", "grpc"),
  ];

  internal static string PropsRelativePath(string family) =>
    $"source/container-apps/{family}/msbuild/feature-filename-grammar.g.props";

  internal static IReadOnlyList<string> OutputRelativePaths =>
    [CsRelativePath, .. Families.Select(family => PropsRelativePath(family.Family))];

  internal static bool TryParse(string json, out FeatureFilenameGrammarRegistry registry, out string error)
  {
    registry = new FeatureFilenameGrammarRegistry([], [], []);
    error = string.Empty;

    try
    {
      using var document = JsonDocument.Parse(json);
      JsonElement root = document.RootElement;

      if (root.ValueKind != JsonValueKind.Object)
      {
        error = "feature-filename-grammar.json must be a JSON object.";
        return false;
      }

      if (!TryReadStrings(root, "layers", required: true, out string[] layers, ref error)) return false;
      if (!TryReadStrings(root, "unroutedLayers", required: false, out string[] unrouted, ref error)) return false;

      if (!root.TryGetProperty("functions", out JsonElement functionsElement)
        || functionsElement.ValueKind != JsonValueKind.Object)
      {
        error = "feature-filename-grammar.json needs a \"functions\" object.";
        return false;
      }

      List<KeyValuePair<string, string>> functions = [];
      foreach (JsonProperty property in functionsElement.EnumerateObject())
      {
        if (property.Value.ValueKind != JsonValueKind.String)
        {
          error = $"Function '{property.Name}' must map to a layer name string.";
          return false;
        }

        functions.Add(new KeyValuePair<string, string>(property.Name, property.Value.GetString()!));
      }

      registry = new FeatureFilenameGrammarRegistry(layers, unrouted, functions);
      return true;
    }
    catch (JsonException exception)
    {
      error = $"feature-filename-grammar.json is not valid JSON: {exception.Message}";
      return false;
    }
  }

  /// <summary>Returns the first rule violation, or null when the registry is valid.</summary>
  internal static string? Validate(FeatureFilenameGrammarRegistry registry)
  {
    foreach (KeyValuePair<string, string> function in registry.Functions)
    {
      if (!registry.Layers.Contains(function.Value, StringComparer.Ordinal))
      {
        return $"Function '{function.Key}' maps to unregistered layer '{function.Value}'.";
      }
    }

    string[] suffixes = registry.AllLayers.Select(layer => $"-{layer}").ToArray();
    foreach (string a in suffixes)
    {
      foreach (string b in suffixes)
      {
        if (!string.Equals(a, b, StringComparison.Ordinal) && a.EndsWith(b, StringComparison.Ordinal))
        {
          return $"Layer suffix '{a}' nests inside '{b}' (dual glob match risk).";
        }
      }
    }

    return null;
  }

  internal static bool TryGenerate(string json, out IReadOnlyList<FeatureFilenameGrammarOutput> outputs, out string error)
  {
    outputs = [];
    if (!TryParse(json, out FeatureFilenameGrammarRegistry registry, out error)) return false;

    string? violation = Validate(registry);
    if (violation is not null)
    {
      error = violation;
      return false;
    }

    List<FeatureFilenameGrammarOutput> generated =
    [
      new(CsRelativePath, EmitCs(registry, Families.Select(family => family.Family).ToArray())),
    ];
    generated.AddRange(Families.Select(family => new FeatureFilenameGrammarOutput(
      PropsRelativePath(family.Family),
      EmitProps(registry, family.Prefix))));

    outputs = generated;
    return true;
  }

  internal static void Write(string repoRoot, IEnumerable<FeatureFilenameGrammarOutput> outputs)
  {
    UTF8Encoding encoding = new(encoderShouldEmitUTF8Identifier: false);
    foreach (FeatureFilenameGrammarOutput output in outputs)
    {
      string path = Path.Combine(repoRoot, output.RelativePath);
      string? directory = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(directory))
      {
        Directory.CreateDirectory(directory);
      }

      File.WriteAllText(path, output.Text, encoding);
    }
  }

  internal static string EmitCs(FeatureFilenameGrammarRegistry registry, IReadOnlyList<string> families)
  {
    string layerArgs = string.Join(", ", registry.AllLayers.Select(layer => $"\"{layer}\""));
    string functionEntries = string.Join("\n", SortedFunctions(registry).Select(function =>
      $"      new KeyValuePair<string, string>(\"{function.Key}\", \"{function.Value}\"),"));
    string longestFirst = string.Join("\n", LongestFirst(registry.Functions.Select(function => function.Key))
      .Select(function => $"      \"{function}\","));
    string familyArgs = string.Join(", ", families.Select(family => $"\"{family}\""));

    string text = $$"""
      // <auto-generated />
      // Generated from feature-filename-grammar.json — do not edit. Edit feature-filename-grammar.json and run dev regenerate-generated.
      #nullable enable
      namespace TimeWarp.Architecture.Analyzers;

      using System.Collections.Generic;
      using System.Collections.Immutable;

      /// <summary>SSOT-backed feature filename grammar constants (TWA0015/TWA0016).</summary>
      internal static class FeatureFilenameGrammar
      {
        /// <summary>Routed layers (own a layer-project Compile glob) PLUS registered-unrouted layers
        /// (e.g. "tests" — matched and validated by TWA0015/TWA0016/the membership guard, but compiled
        /// into no layer project). See feature-filename-grammar.json's "unroutedLayers".</summary>
        public static readonly ImmutableHashSet<string> Layers =
          ImmutableHashSet.Create(System.StringComparer.Ordinal, {{layerArgs}});

        public static readonly ImmutableDictionary<string, string> FunctionToLayer =
          ImmutableDictionary.CreateRange(System.StringComparer.Ordinal, new[]
          {
      {{functionEntries}}
          });

        public static readonly ImmutableArray<string> FunctionsLongestFirst =
          ImmutableArray.CreateRange(new[]
          {
      {{longestFirst}}
          });

        /// <summary>Lowercase family directory names (e.g. "web", "api", "grpc") whose cohesive trees
        /// TWA0015/TWA0016 scope into; sourced from FeatureFilenameGrammarGenerator.Families
        /// (dev regenerate-generated) and cross-checked by the SSOT drift test.</summary>
        public static readonly ImmutableArray<string> Families =
          ImmutableArray.Create({{familyArgs}});
      }
      """;

    return Normalize(text);
  }

  internal static string EmitProps(FeatureFilenameGrammarRegistry registry, string prefix)
  {
    string prefixLower = prefix.ToLowerInvariant();
    string layerAlternation = string.Join("|", LongestFirst(registry.AllLayers));

    var layerItems = registry.Layers
      .Select(layer => $"    <FeatureFilenameGrammarLayer Include=\"{layer}\" Project=\"{prefixLower}-{layer}\" />")
      .ToList();
    layerItems.AddRange(registry.UnroutedLayers
      .Select(layer => $"    <FeatureFilenameGrammarLayer Include=\"{layer}\" />"));
    string functionItems = string.Join("\n", SortedFunctions(registry).Select(function =>
      $"    <FeatureFilenameGrammarFunction Include=\"{function.Key}\" Layer=\"{function.Value}\" />"));

    // Compile groups come from routed layers only; an unrouted layer claims no layer project.
    string compileGroups = string.Join("\n", registry.Layers.Select(layer => $$"""
        <ItemGroup Condition="'$(MSBuildProjectName)' == '{{prefixLower}}-{{layer}}'">
          <Compile Include="$({{prefix}}FeatureTreeRoot)/**/*-{{layer}}.cs"
                   Link="features\%(RecursiveDir)%(Filename)%(Extension)" />
          <Compile Include="$({{prefix}}PlatformTreeRoot)/**/*-{{layer}}.cs"
                   Condition="'$({{prefix}}PlatformTreeRoot)' != ''"
                   Link="platform\%(RecursiveDir)%(Filename)%(Extension)" />
        </ItemGroup>
      """));

    string text = $$"""
      <!--
        AUTO-GENERATED from source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json
        Do not edit by hand. Edit feature-filename-grammar.json and run dev regenerate-generated.
        Registry edit ⇒ full rebuild (analyzer incremental staleness).
        Nesting of layer suffixes is rejected at generation time (dual-match prevention).
        Hybrid includes: {{prefix}}FeatureTreeRoot (features/) and {{prefix}}PlatformTreeRoot (platform/) from
        feature-membership.targets — same -{layer} suffix → layer project mapping. Family: {{prefix}}.
      -->
      <Project>
        <PropertyGroup>
          <FeatureFilenameLayerSuffixRegex>-({{layerAlternation}})$</FeatureFilenameLayerSuffixRegex>
        </PropertyGroup>
        <ItemGroup>
      {{string.Join("\n", layerItems)}}
      {{functionItems}}
        </ItemGroup>
        <!-- Hybrid includes: require tree roots set by feature-membership.targets. -->
      {{compileGroups}}
      </Project>
      """;

    return Normalize(text);
  }

  private static IEnumerable<KeyValuePair<string, string>> SortedFunctions(FeatureFilenameGrammarRegistry registry) =>
    registry.Functions.OrderBy(function => function.Key, StringComparer.Ordinal);

  private static IEnumerable<string> LongestFirst(IEnumerable<string> names) =>
    names.OrderByDescending(name => name.Length).ThenBy(name => name, StringComparer.Ordinal);

  private static string Normalize(string text) => text.ReplaceLineEndings("\n") + "\n";

  private static bool TryReadStrings(JsonElement root, string name, bool required, out string[] values, ref string error)
  {
    values = [];
    if (!root.TryGetProperty(name, out JsonElement element))
    {
      if (!required) return true;
      error = $"feature-filename-grammar.json needs a \"{name}\" array.";
      return false;
    }

    if (element.ValueKind != JsonValueKind.Array
      || element.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String))
    {
      error = $"feature-filename-grammar.json \"{name}\" must be an array of strings.";
      return false;
    }

    values = element.EnumerateArray().Select(item => item.GetString()!).ToArray();
    return true;
  }
}
