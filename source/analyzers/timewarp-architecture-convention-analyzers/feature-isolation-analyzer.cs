#region Purpose
// Enforces feature isolation: code under features/<x>/ must not reference a namespace owned by
// features/<y>/ (TWPA0009).
#endregion

#region Design
// Replaces the coupling detection the de-flagged template-verification loop used to provide (071):
// cross-feature references (superhero using weather-forecast's table components; the Style Guide
// using CounterState) previously surfaced only when a flag-off generation failed to compile —
// this makes them build errors on every build.
// Ownership is derived, not configured: a namespace is feature-owned only when EVERY declaration
// of it in the compilation lives under a single features/<x>/ folder. Namespaces shared across
// features or with shell code (Pages, Components) are thereby excluded automatically, as is
// anything from referenced assemblies (contracts are shared by design and resolve to metadata).
// The features/base folder is the shared substrate every feature builds on, not a feature — it is
// treated as shell. Razor markup compiles into generated trees, which analyzers skip
// (GeneratedCodeAnalysisFlags.None), so enforcement covers hand-written .cs only.
// [CrossFeatureReference(reason)] on a containing type is the explicit, reasoned opt-out
// (matched by attribute name so this analyzer has no dependency on foundation-contracts).
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FeatureIsolationAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWPA0009";

  private const string OptOutAttributeName = "CrossFeatureReference";
  private const string SharedSubstrateFolder = "base";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Feature references another feature's namespace",
      messageFormat: "Feature '{0}' references '{1}', owned by feature '{2}'; share via components/ or contracts, or mark the type [CrossFeatureReference(reason)]",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Feature folders are independently removable slices. A reference from one feature into another couples them so neither can be deleted cleanly; genuinely shared code belongs in components/ or a contracts assembly."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      Dictionary<string, string> ownedNamespaces = BuildOwnershipMap(startContext.Compilation, startContext.CancellationToken);
      if (ownedNamespaces.Count == 0) return;

      startContext.RegisterSemanticModelAction(modelContext => AnalyzeFile(modelContext, ownedNamespaces));
    });
  }

  // namespace -> owning feature, only where every declaration of the namespace lives under a
  // single features/<x>/ folder (multi-owner and shell-shared namespaces drop out).
  private static Dictionary<string, string> BuildOwnershipMap(Compilation compilation, System.Threading.CancellationToken cancellationToken)
  {
    var owners = new Dictionary<string, HashSet<string?>>(System.StringComparer.Ordinal);

    foreach (SyntaxTree tree in compilation.SyntaxTrees)
    {
      // Generator-produced trees (TimeWarp.State's ActionSet partials, razor codegen) re-declare
      // feature namespaces from generated locations — including on-disk copies under
      // artifacts/generated when EmitCompilerGeneratedFiles is on — and would poison ownership
      // into "multi-owner". Only hand-written sources define ownership.
      if (IsGeneratedFile(tree.FilePath)) continue;

      string? feature = FeatureOf(tree.FilePath);
      SyntaxNode root = tree.GetRoot(cancellationToken);

      foreach (BaseNamespaceDeclarationSyntax declaration in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
      {
        string name = declaration.Name.ToString();
        if (!owners.TryGetValue(name, out HashSet<string?>? set))
        {
          set = [];
          owners[name] = set;
        }

        set.Add(feature);
      }
    }

    var owned = new Dictionary<string, string>(System.StringComparer.Ordinal);
    foreach (KeyValuePair<string, HashSet<string?>> entry in owners)
    {
      if (entry.Value.Count == 1 && entry.Value.First() is string singleOwner)
      {
        owned[entry.Key] = singleOwner;
      }
    }

    return owned;
  }

  private static void AnalyzeFile(SemanticModelAnalysisContext context, Dictionary<string, string> ownedNamespaces)
  {
    SyntaxTree tree = context.SemanticModel.SyntaxTree;
    string? feature = FeatureOf(tree.FilePath);
    if (feature is null) return;

    SyntaxNode root = tree.GetRoot(context.CancellationToken);

    IAssemblySymbol currentAssembly = context.SemanticModel.Compilation.Assembly;

    foreach (IdentifierNameSyntax identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
    {
      ISymbol? symbol = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol;

      // Namespace symbols are merged across assemblies (a using directive proves nothing about
      // which assembly's members get used), so only member-level references carry signal — and
      // only when the symbol is defined in THIS compilation: the same namespace name arriving
      // from metadata (e.g. a contracts assembly) is sharing-by-contract, the sanctioned channel.
      if (symbol is null or INamespaceSymbol) continue;
      if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, currentAssembly)) continue;

      string? namespaceName = symbol.ContainingNamespace?.ToDisplayString();
      if (namespaceName is null) continue;
      if (!ownedNamespaces.TryGetValue(namespaceName, out string? owner) || owner == feature) continue;
      if (HasOptOut(identifier)) continue;

      context.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation(), feature, namespaceName, owner));
    }
  }

  private static bool HasOptOut(SyntaxNode node) =>
    node.Ancestors().OfType<TypeDeclarationSyntax>().Any(static type =>
      type.AttributeLists.SelectMany(static list => list.Attributes).Any(static attribute =>
      {
        string name = attribute.Name switch
        {
          QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
          SimpleNameSyntax simple => simple.Identifier.ValueText,
          _ => attribute.Name.ToString()
        };
        return name is OptOutAttributeName or OptOutAttributeName + "Attribute";
      }));

  private static bool IsGeneratedFile(string filePath) =>
    !Path.IsPathRooted(filePath) ||
    filePath.EndsWith(".g.cs", System.StringComparison.OrdinalIgnoreCase) ||
    filePath.EndsWith(".g.i.cs", System.StringComparison.OrdinalIgnoreCase) ||
    filePath.EndsWith(".generated.cs", System.StringComparison.OrdinalIgnoreCase) ||
    filePath.EndsWith(".designer.cs", System.StringComparison.OrdinalIgnoreCase);

  // features/<x>/... -> x; the shared-substrate folder and non-feature paths -> null.
  private static string? FeatureOf(string filePath)
  {
    if (string.IsNullOrEmpty(filePath)) return null;

    string normalized = filePath.Replace('\\', '/');
    const string Marker = "/features/";
    int index = normalized.IndexOf(Marker, System.StringComparison.OrdinalIgnoreCase);
    if (index < 0) return null;

    int start = index + Marker.Length;
    int end = normalized.IndexOf('/', start);
    if (end < 0) return null; // features/<file>.cs — not inside a feature folder

    string feature = normalized.Substring(start, end - start);
    return feature.Equals(SharedSubstrateFolder, System.StringComparison.OrdinalIgnoreCase) ? null : feature;
  }
}
