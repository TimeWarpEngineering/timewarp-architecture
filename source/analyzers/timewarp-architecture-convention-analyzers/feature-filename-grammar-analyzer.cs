#region Purpose
// SPIKE (114-001): prototype archetype-pairing check for the feature filename grammar.
#endregion

#region Design
// Grammar: <name>[-<function>]-<layer>.cs for files under a feature-cohesive tree (path segment
// "/features/" outside a layer project). A recognized FUNCTION token must pair with its
// registered LAYER (e.g. -handler- pairs with -application); an unrecognized function token is
// an error so the vocabulary stays curated. Prototype scope: handler/endpoint only, and only
// teaching-quality messages — the shipped version derives both maps from a single registry
// shared with the membership .targets (two-things-must-agree: generate one from the other).
// TWA0001 already proves filename-aware analysis in this repo; this adds the pairing shape.
#endregion

namespace TimeWarp.Architecture.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FeatureFilenameGrammarAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWA9999";

  private static readonly ImmutableDictionary<string, string> FunctionToLayer =
    ImmutableDictionary.CreateRange(StringComparer.Ordinal, new[]
    {
      new KeyValuePair<string, string>("handler", "application"),
      new KeyValuePair<string, string>("endpoint", "server"),
    });

  private static readonly ImmutableHashSet<string> Layers =
    ImmutableHashSet.Create(StringComparer.Ordinal, "contracts", "application", "server");

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Feature filename function/layer segments disagree",
      messageFormat: "{0}",
      category: "Naming",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Feature files follow <name>[-<function>]-<layer>.cs; a recognized function segment must pair with its registered layer."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxTreeAction(AnalyzeTree);
  }

  private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
  {
    string path = context.Tree.FilePath.Replace('\\', '/');
    if (!path.Contains("/features/", StringComparison.Ordinal))
    {
      return;
    }
    // Only the cohesive tree (features/ directly under the container root, not layer projects'
    // internal features folders): heuristic for the spike — parent of /features/ has no csproj.
    if (path.Contains("web-application/features/", StringComparison.Ordinal) || path.Contains("web-contracts/features/", StringComparison.Ordinal) ||
        path.Contains("web-server/features/", StringComparison.Ordinal) || path.Contains("web-spa/features/", StringComparison.Ordinal))
    {
      return;
    }

    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
    string[] segments = fileName.Split('-');
    if (segments.Length < 2)
    {
      return; // membership guard reports missing layer suffix
    }

    string layer = segments[^1];
    if (!Layers.Contains(layer))
    {
      return; // membership guard's jurisdiction
    }

    if (segments.Length < 3)
    {
      return; // no function segment: escape hatch, valid
    }

    string function = segments[^2];
    if (FunctionToLayer.TryGetValue(function, out string? requiredLayer))
    {
      if (!string.Equals(layer, requiredLayer, StringComparison.Ordinal))
      {
        context.ReportDiagnostic(Diagnostic.Create(Rule, Location.Create(context.Tree,
          Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(0, 0)),
          $"File '{fileName}.cs': function '-{function}-' is registered to layer '-{requiredLayer}'; rename to end '-{function}-{requiredLayer}.cs' or remove the function segment"));
      }
    }
    // Unrecognized function tokens are tolerated in the prototype (escape hatch names like
    // hello-feature-annotations-server.cs); the shipped registry decides strictness.
  }
}
