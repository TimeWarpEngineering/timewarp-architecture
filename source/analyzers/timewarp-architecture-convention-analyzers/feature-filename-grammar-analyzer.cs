#region Purpose
// Enforce feature filename grammar: registered function segments pair with their layer (TWA0015)
// and unregistered function segments used as archetypes are rejected (TWA0016).
#endregion

#region Design
// Grammar: <name>[-<function>]-<layer>.cs under the feature-cohesive tree (web/features/).
// Registry SSOT: feature-filename-grammar.json → FeatureFilenameGrammar.g.cs (no hand maps).
// Function match is closed-set longest-first so multi-segment functions (feature-annotations)
// win over shorter tokens, and escape-hatch names (role-store-application.cs) stay valid —
// unregistered trailing tokens are part of <name>, not functions (do not require every
// pre-layer token to be registered).
// TWA0015: registered function + wrong layer (message lists valid pairs).
// TWA0016: a multi-segment trailing candidate that is not registered but shares the final
// segment of a registered multi-segment function (incomplete archetype name), OR an explicit
// single-segment token that matches no registered function when the author used a form that
// cannot be an escape hatch: we only flag single-segment unknowns when they equal a registered
// function under ordinal-ignore-case but not ordinal (casing), else inventing tokens is escape hatch.
// Path pitfall (spike 114-001): FilePath arrives project-relative WITH `..` traversal
// (`web-server/../features/...`). Normalize before scoping; never exclude by bare
// `web-server/` substring. Scope with affirmative cohesive markers only:
// `../features/` (layer glob), absolute `…/web/features/…`, and collapsed
// `features/` ONLY when the original path traversed `../features/` — bare project-local
// `features/` (SPA) must stay silent even for grammar-shaped names.
// Registry edit ⇒ full rebuild (analyzer DLLs can go stale under incremental builds).
#endregion

namespace TimeWarp.Architecture.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FeatureFilenameGrammarAnalyzer : DiagnosticAnalyzer
{
  public const string PairingMismatchId = "TWA0015";
  public const string UnregisteredFunctionId = "TWA0016";

  private const string Category = "Naming";

  // Layer-project feature folders that are NOT the cohesive tree (exclude after path normalize).
  private static readonly string[] LayerProjectFeatureMarkers =
  [
    "/web-contracts/features/",
    "/web-application/features/",
    "/web-server/features/",
    "/web-domain/features/",
    "/web-infrastructure/features/",
    "/web-spa/features/",
  ];

  private static readonly DiagnosticDescriptor PairingMismatchRule =
    new
    (
      PairingMismatchId,
      title: "Feature filename function/layer segments disagree",
      messageFormat: "File '{0}': function '-{1}-' is registered to layer '-{2}' — rename to end '-{1}-{2}' or remove the function segment (registered pairs: {3})",
      category: Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Feature files follow <name>[-<function>]-<layer>.cs; a registered function segment must pair with its registered layer (see feature-filename-grammar.json)."
    );

  private static readonly DiagnosticDescriptor UnregisteredFunctionRule =
    new
    (
      UnregisteredFunctionId,
      title: "Feature filename uses an unregistered function segment",
      messageFormat: "File '{0}': '-{1}-' is not a registered function segment (use a registered function ({2}), fix the spelling, or use the escape-hatch form '<name>-<layer>' with no function segment)",
      category: Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Function segments are a closed set from feature-filename-grammar.json (longest-first match). Escape-hatch files omit the function segment entirely."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(PairingMismatchRule, UnregisteredFunctionRule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxTreeAction(AnalyzeTree);
  }

  private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
  {
    string? relativePath = TryGetCohesiveFeatureRelativePath(context.Tree.FilePath);
    if (relativePath is null)
    {
      return;
    }

    string fileName = Path.GetFileName(relativePath);
    if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    {
      return;
    }

    string stem = fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
      ? fileName.Substring(0, fileName.Length - 3)
      : fileName;

    GrammarParse parse = Parse(stem);
    if (parse.Kind == GrammarParseKind.NotApplicable)
    {
      return;
    }

    // Anchor on the first token (same pattern as PurposeRegionAnalyzer) so span matching in
    // analyzer tests is stable and suppressions can target the diagnostic.
    SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);
    SyntaxToken firstToken = root.GetFirstToken();
    if (firstToken.RawKind == 0)
    {
      return;
    }

    Location location = firstToken.GetLocation();

    if (parse.Kind == GrammarParseKind.PairingMismatch)
    {
      context.ReportDiagnostic
      (
        Diagnostic.Create
        (
          PairingMismatchRule,
          location,
          fileName,
          parse.Function,
          parse.RequiredLayer,
          FormatRegisteredPairs()
        )
      );
      return;
    }

    if (parse.Kind == GrammarParseKind.UnregisteredFunction)
    {
      context.ReportDiagnostic
      (
        Diagnostic.Create
        (
          UnregisteredFunctionRule,
          location,
          fileName,
          parse.Function,
          FormatRegisteredFunctions()
        )
      );
    }
  }

  /// <summary>
  /// Returns a normalized path still containing <c>/features/</c> when the tree is in the
  /// cohesive feature tree; null when the file is out of scope.
  /// </summary>
  internal static string? TryGetCohesiveFeatureRelativePath(string? filePath)
  {
    if (string.IsNullOrEmpty(filePath))
    {
      return null;
    }

    string normalized = NormalizePath(filePath);
    string collapsed = CollapseDotDot(normalized);

    // Layer / SPA project feature folders are never the cohesive tree (including bare
    // project-relative features/... when the project is web-spa or a layer project —
    // Roslyn often supplies that form for default SDK includes).
    if (IsLayerProjectFeaturesPath(collapsed))
    {
      return null;
    }

    // Affirmative cohesive markers only (avoid treating any remaining /features/ as
    // cohesive — that collides with SPA project-relative paths).
    // 1) Layer-project glob form: ../features/... (and web-server/../features/... after collapse).
    if (normalized.StartsWith("../features/", StringComparison.Ordinal)
        || normalized.Contains("/../features/", StringComparison.Ordinal)
        || collapsed.StartsWith("../features/", StringComparison.Ordinal))
    {
      return collapsed.StartsWith("../features/", StringComparison.Ordinal)
        ? collapsed
        : CollapseDotDot(normalized);
    }

    // After collapsing web-server/../features/hello/x.cs → features/hello/x.cs from a layer
    // glob. Accept bare features/ ONLY when the original path had a `..` traversal into
    // features (layer glob), not when it was a project-local features/ tree (SPA).
    if (collapsed.StartsWith("features/", StringComparison.Ordinal)
        && (normalized.Contains("../features/", StringComparison.Ordinal)
            || normalized.Contains("/../features/", StringComparison.Ordinal)
            || normalized.StartsWith("../features/", StringComparison.Ordinal)))
    {
      return collapsed;
    }

    // Absolute / repo-rooted cohesive tree: .../web/features/... (not .../web-*/features/...)
    if (collapsed.Contains("/web/features/", StringComparison.Ordinal)
        || collapsed.Contains("/container-apps/web/features/", StringComparison.Ordinal))
    {
      return collapsed;
    }

    return null;
  }

  private static bool IsLayerProjectFeaturesPath(string collapsedPath)
  {
    for (int i = 0; i < LayerProjectFeatureMarkers.Length; i++)
    {
      if (collapsedPath.Contains(LayerProjectFeatureMarkers[i], StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  internal static string NormalizePath(string path) => path.Replace('\\', '/');

  /// <summary>Collapse <c>..</c> and <c>.</c> segments without requiring a rooted path.</summary>
  internal static string CollapseDotDot(string path)
  {
    string[] parts = path.Split('/');
    List<string> stack = new(parts.Length);
    foreach (string part in parts)
    {
      if (part is "." or "")
      {
        if (part is "" && stack.Count == 0)
        {
          // Preserve leading slash for absolute paths.
          stack.Add("");
        }

        continue;
      }

      if (part == ".." && stack.Count > 0 && stack[stack.Count - 1] is not (".." or ""))
      {
        stack.RemoveAt(stack.Count - 1);
        continue;
      }

      stack.Add(part);
    }

    if (stack.Count == 0)
    {
      return "";
    }

    // Join; if first is empty we had a leading slash.
    if (stack[0] is "")
    {
      return "/" + string.Join("/", stack.Skip(1));
    }

    return string.Join("/", stack);
  }

  internal static GrammarParse Parse(string stemWithoutExtension)
  {
    if (string.IsNullOrEmpty(stemWithoutExtension))
    {
      return GrammarParse.NotApplicable;
    }

    // Find registered layer suffix (longest layer name first so infrastructure wins over none).
    string? layer = null;
    string preLayer = stemWithoutExtension;
    foreach (string candidate in FeatureFilenameGrammar.Layers.OrderByDescending(static l => l.Length))
    {
      string suffix = "-" + candidate;
      if (stemWithoutExtension.EndsWith(suffix, StringComparison.Ordinal))
      {
        layer = candidate;
        preLayer = stemWithoutExtension.Substring(0, stemWithoutExtension.Length - suffix.Length);
        break;
      }
    }

    if (layer is null)
    {
      // Membership guard's jurisdiction.
      return GrammarParse.NotApplicable;
    }

    if (preLayer.Length == 0)
    {
      return GrammarParse.NotApplicable;
    }

    // Longest-first closed-set function match at the end of pre-layer.
    foreach (string function in FeatureFilenameGrammar.FunctionsLongestFirst)
    {
      if (preLayer.Equals(function, StringComparison.Ordinal)
          || preLayer.EndsWith("-" + function, StringComparison.Ordinal))
      {
        string requiredLayer = FeatureFilenameGrammar.FunctionToLayer[function];
        if (!string.Equals(layer, requiredLayer, StringComparison.Ordinal))
        {
          return GrammarParse.Mismatch(function, requiredLayer, layer);
        }

        return GrammarParse.Ok;
      }
    }

    // No registered function matched → escape hatch (<name>-<layer>.cs), including multi-hyphen names.
    // TWA0016: incomplete multi-segment archetype (shares final segment of a multi-segment function
    // but is not itself registered), e.g. hello-annotations-server when feature-annotations exists.
    string? incomplete = TryFindIncompleteMultiSegmentFunction(preLayer);
    if (incomplete is not null)
    {
      return GrammarParse.Unregistered(incomplete);
    }

    // Case-only mismatch against a registered function (e.g. -Handler- vs -handler-).
    foreach (string function in FeatureFilenameGrammar.FunctionsLongestFirst)
    {
      if (preLayer.Equals(function, StringComparison.OrdinalIgnoreCase)
          || preLayer.EndsWith("-" + function, StringComparison.OrdinalIgnoreCase))
      {
        // Differ only by case → treat as unregistered spelling of a known function.
        string actual = ExtractTrailingIgnoreCase(preLayer, function);
        return GrammarParse.Unregistered(actual);
      }
    }

    return GrammarParse.Ok;
  }

  private static string? TryFindIncompleteMultiSegmentFunction(string preLayer)
  {
    foreach (string function in FeatureFilenameGrammar.FunctionsLongestFirst)
    {
      if (!function.Contains('-', StringComparison.Ordinal))
      {
        continue;
      }

      int lastHyphen = function.LastIndexOf('-');
      string lastSegment = function.Substring(lastHyphen + 1);
      if (lastSegment.Length == 0)
      {
        continue;
      }

      // preLayer ends with -{lastSegment} or equals lastSegment, but is not the full function.
      bool endsWithLast = preLayer.Equals(lastSegment, StringComparison.Ordinal)
        || preLayer.EndsWith("-" + lastSegment, StringComparison.Ordinal);
      if (!endsWithLast)
      {
        continue;
      }

      if (preLayer.Equals(function, StringComparison.Ordinal)
          || preLayer.EndsWith("-" + function, StringComparison.Ordinal))
      {
        continue; // full match already handled above
      }

      // Avoid flagging escape-hatch names that merely end with a common word unless the
      // preceding token looks like the multi-segment function's first segment(s).
      string functionPrefix = function.Substring(0, function.Length - lastSegment.Length - 1);
      if (preLayer.Equals(lastSegment, StringComparison.Ordinal))
      {
        // Bare last segment as entire pre-layer (e.g. annotations-server) — incomplete.
        return lastSegment;
      }

      int prefixHyphen = functionPrefix.IndexOf('-', StringComparison.Ordinal);
      string firstSegment = prefixHyphen >= 0
        ? functionPrefix.Substring(0, prefixHyphen)
        : functionPrefix;

      if (preLayer.Contains("-" + firstSegment + "-", StringComparison.Ordinal)
          || preLayer.StartsWith(firstSegment + "-", StringComparison.Ordinal))
      {
        // Extract the trailing candidate from firstSegment through lastSegment in preLayer.
        int start = preLayer.StartsWith(firstSegment + "-", StringComparison.Ordinal)
          ? 0
          : preLayer.IndexOf("-" + firstSegment + "-", StringComparison.Ordinal) + 1;
        if (start >= 0 && start < preLayer.Length)
        {
          string candidate = preLayer.Substring(start);
          if (!FeatureFilenameGrammar.FunctionToLayer.ContainsKey(candidate))
          {
            return candidate;
          }
        }
      }
    }

    return null;
  }

  private static string ExtractTrailingIgnoreCase(string preLayer, string function)
  {
    if (preLayer.Length >= function.Length)
    {
      return preLayer.Substring(preLayer.Length - function.Length);
    }

    return preLayer;
  }

  private static string FormatRegisteredPairs()
  {
    IEnumerable<string> pairs = FeatureFilenameGrammar.FunctionToLayer
      .OrderBy(static p => p.Key, StringComparer.Ordinal)
      .Select(static p => $"-{p.Key}- ⇒ -{p.Value}");
    return string.Join(", ", pairs);
  }

  private static string FormatRegisteredFunctions()
  {
    IEnumerable<string> names = FeatureFilenameGrammar.FunctionsLongestFirst
      .OrderBy(static f => f, StringComparer.Ordinal)
      .Select(static f => $"-{f}-");
    return string.Join(", ", names);
  }

  internal enum GrammarParseKind
  {
    NotApplicable,
    Ok,
    PairingMismatch,
    UnregisteredFunction,
  }

  internal readonly struct GrammarParse
  {
    public GrammarParseKind Kind { get; }
    public string? Function { get; }
    public string? RequiredLayer { get; }
    public string? ActualLayer { get; }

    private GrammarParse(GrammarParseKind kind, string? function, string? requiredLayer, string? actualLayer)
    {
      Kind = kind;
      Function = function;
      RequiredLayer = requiredLayer;
      ActualLayer = actualLayer;
    }

    public static GrammarParse NotApplicable { get; } = new(GrammarParseKind.NotApplicable, null, null, null);
    public static GrammarParse Ok { get; } = new(GrammarParseKind.Ok, null, null, null);

    public static GrammarParse Mismatch(string function, string requiredLayer, string actualLayer) =>
      new(GrammarParseKind.PairingMismatch, function, requiredLayer, actualLayer);

    public static GrammarParse Unregistered(string function) =>
      new(GrammarParseKind.UnregisteredFunction, function, null, null);
  }
}
