#region Purpose
// Enforce slice isolation: a type under the configured slice root may not reference types
// owned by a different product slice in the same assembly (TWA0009).
#endregion

#region Design
// Namespace-as-slice (Option A): SliceRoot defaults to {RootNamespace}.Features (optional
// TimeWarpSliceRoot override). Slice id is the full path under the root after stripping
// structural suffixes Pages/Components/Application. Folders are non-normative.
// Tiers: Outside (composition free) · Substrate (bare …Features) · Platform (Applications,
// one-way: product may use it; platform/substrate must not use product) · Product (symmetric).
// Same-assembly only; contracts/metadata free. SimpleNameSyntax walk covers generics.
// [CrossSliceReference(typeof(T), reason)] is edge-scoped and partial-safe via semantic
// GetAttributes(). Razor/generated trees out (GeneratedCodeAnalysisFlags.None).
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SliceIsolationAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWA0009";

  private const string OptOutAttributeFullName = "CrossSliceReferenceAttribute";
  private const string PlatformSliceId = "Applications";
  private const string RootNamespaceProperty = "build_property.RootNamespace";
  private const string SliceRootProperty = "build_property.TimeWarpSliceRoot";

  private static readonly HashSet<string> StructuralSuffixes =
    new(System.StringComparer.Ordinal) { "Pages", "Components", "Application" };

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Slice references another product slice",
      messageFormat: "Slice '{0}' references '{1}', owned by slice '{2}'; share via Components or contracts, or mark the type [CrossSliceReference(typeof(...), reason)]",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Slices are independently removable vertical units. A reference from one product slice into another couples them so neither can be deleted cleanly; genuinely shared code belongs in Components or a contracts assembly. Product may depend on the Applications platform slice one-way."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      string? sliceRoot = ResolveSliceRoot(startContext.Options);
      if (sliceRoot is null) return;

      startContext.RegisterSemanticModelAction(modelContext => AnalyzeFile(modelContext, sliceRoot));
    });
  }

  private static string? ResolveSliceRoot(AnalyzerOptions options)
  {
    AnalyzerConfigOptions global = options.AnalyzerConfigOptionsProvider.GlobalOptions;

    if (global.TryGetValue(SliceRootProperty, out string? overrideRoot) &&
        !string.IsNullOrWhiteSpace(overrideRoot))
    {
      return overrideRoot.Trim();
    }

    if (global.TryGetValue(RootNamespaceProperty, out string? rootNamespace) &&
        !string.IsNullOrWhiteSpace(rootNamespace))
    {
      return rootNamespace.Trim() + ".Features";
    }

    return null;
  }

  private static void AnalyzeFile(SemanticModelAnalysisContext context, string sliceRoot)
  {
    SemanticModel model = context.SemanticModel;
    SyntaxNode root = model.SyntaxTree.GetRoot(context.CancellationToken);
    IAssemblySymbol currentAssembly = model.Compilation.Assembly;

    foreach (SimpleNameSyntax name in root.DescendantNodes().OfType<SimpleNameSyntax>())
    {
      ISymbol? symbol = model.GetSymbolInfo(name, context.CancellationToken).Symbol;
      if (symbol is null or INamespaceSymbol) continue;
      if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, currentAssembly)) continue;

      INamedTypeSymbol? containingType = GetContainingType(name, model, context.CancellationToken);
      if (containingType is null) continue;

      Slice source = SliceOf(containingType.ContainingNamespace?.ToDisplayString(), sliceRoot);
      if (source.Kind == SliceKind.Outside) continue;

      string? targetNamespace = symbol.ContainingNamespace?.ToDisplayString();
      Slice target = SliceOf(targetNamespace, sliceRoot);

      if (!IsForbidden(source, target)) continue;

      // Opt-out only applies when the target is a product slice (edge into that id).
      if (target is { Kind: SliceKind.Product, Id: string targetId } &&
          HasOptOutForTarget(containingType, targetId, sliceRoot))
      {
        continue;
      }

      string referenced = ReferencedDisplay(symbol);
      context.ReportDiagnostic(
        Diagnostic.Create(Rule, name.GetLocation(), source.Id ?? source.Kind.ToString(), referenced, target.Id ?? target.Kind.ToString()));
    }
  }

  private static bool IsForbidden(Slice source, Slice target) =>
    (source.Kind, target.Kind) switch
    {
      // product A → product B (A ≠ B)
      (SliceKind.Product, SliceKind.Product) =>
        !string.Equals(source.Id, target.Id, System.StringComparison.Ordinal),
      // platform / substrate must not reach product without opt-out
      (SliceKind.Platform, SliceKind.Product) => true,
      (SliceKind.Substrate, SliceKind.Product) => true,
      _ => false
    };

  private static bool HasOptOutForTarget(INamedTypeSymbol containingType, string targetProductSliceId, string sliceRoot)
  {
    foreach (AttributeData attribute in containingType.GetAttributes())
    {
      if (attribute.AttributeClass?.Name is not OptOutAttributeFullName) continue;
      if (attribute.ConstructorArguments.Length < 1) continue;
      if (attribute.ConstructorArguments[0].Value is not ITypeSymbol targetType) continue;

      Slice targetSlice = SliceOf(targetType.ContainingNamespace?.ToDisplayString(), sliceRoot);
      if (targetSlice is { Kind: SliceKind.Product, Id: string id } &&
          string.Equals(id, targetProductSliceId, System.StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  private static INamedTypeSymbol? GetContainingType(SyntaxNode node, SemanticModel model, System.Threading.CancellationToken cancellationToken)
  {
    TypeDeclarationSyntax? declaration = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
    return declaration is null
      ? null
      : model.GetDeclaredSymbol(declaration, cancellationToken) as INamedTypeSymbol;
  }

  private static string ReferencedDisplay(ISymbol symbol)
  {
    INamedTypeSymbol? type = symbol as INamedTypeSymbol ?? symbol.ContainingType;
    if (type is not null) return type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    return symbol.ContainingNamespace?.ToDisplayString() ?? symbol.Name;
  }

  // TryGetSliceId per task 091 §2.1
  internal static Slice SliceOf(string? namespaceDisplayString, string sliceRoot)
  {
    if (string.IsNullOrEmpty(namespaceDisplayString)) return Slice.Outside;

    if (string.Equals(namespaceDisplayString, sliceRoot, System.StringComparison.Ordinal))
    {
      return Slice.Substrate;
    }

    string prefix = sliceRoot + ".";
    if (!namespaceDisplayString.StartsWith(prefix, System.StringComparison.Ordinal))
    {
      return Slice.Outside;
    }

    string rest = namespaceDisplayString.Substring(prefix.Length);
    if (rest.Length == 0) return Slice.Substrate;

    var parts = new List<string>(rest.Split('.'));
    while (parts.Count > 0 && StructuralSuffixes.Contains(parts[parts.Count - 1]))
    {
      parts.RemoveAt(parts.Count - 1);
    }

    if (parts.Count == 0) return Slice.Substrate;

    string id = string.Join(".", parts);
    if (string.Equals(id, PlatformSliceId, System.StringComparison.Ordinal))
    {
      return Slice.Platform(id);
    }

    return Slice.Product(id);
  }

  internal enum SliceKind
  {
    Outside,
    Substrate,
    Platform,
    Product
  }

  internal readonly struct Slice
  {
    public Slice(SliceKind kind, string? id)
    {
      Kind = kind;
      Id = id;
    }

    public SliceKind Kind { get; }
    public string? Id { get; }

    public static Slice Outside => new(SliceKind.Outside, null);
    public static Slice Substrate => new(SliceKind.Substrate, null);
    public static Slice Platform(string id) => new(SliceKind.Platform, id);
    public static Slice Product(string id) => new(SliceKind.Product, id);
  }
}
