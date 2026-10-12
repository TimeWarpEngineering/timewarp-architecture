#region Purpose
// TWA0032: Multi-line text in the SPA renders through the shared components/forms/TextArea.razor.
// A raw FluentTextArea anywhere else is reported.
#endregion

#region Design
// FluentTextArea's visible box is its shadow part="root", sized by --inline-size (default 18rem)
// and --min-block-size. Width="100%" and `block` on the host do not reach it, so every page that
// used the raw control had to remember page CSS. Task 302 fixed /Feedback that way and task 326
// found the same narrow box on the admin and permalink pages. TextArea.razor owns that sizing
// once; this rule turns "remember to use it" into a build failure (standing directive: prefer
// analyzers over convention-by-memory).
//
// Detection: an OpenComponent<FluentTextArea> invocation (Microsoft.FluentUI.AspNetCore.Components)
// in a Blazor WASM compilation. Razor markup and hand-written RenderTreeBuilder code both compile
// to that call. Scope and generated-code handling follow TWA0025: razor/cshtml-generated trees are
// analyzed, other generated trees are exempt.
//
// Host exemption is by file name: the tree (components_forms_TextArea_razor.g.cs) or its #line
// mapped path (TextArea.razor). A file name keeps the rule stable when the template renames the
// root namespace. Opt-out: [DirectFluentTextArea("reason")] on the component or a containing type,
// matched by simple name. An empty or whitespace reason does not opt out.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using Microsoft.CodeAnalysis.Operations;

/// <summary>Roslyn analyzer for TWA0032: FluentTextArea renders only inside the shared TextArea component.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DirectFluentTextAreaAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0032.</summary>
  public const string DiagnosticId = "TWA0032";

  private const string BlazorWasmSdkProperty = "build_property.UsingMicrosoftNETSdkBlazorWebAssembly";
  private const string FluentTextAreaTypeName = "FluentTextArea";
  private const string FluentUiNamespace = "Microsoft.FluentUI.AspNetCore.Components";
  private const string OpenComponentMethodName = "OpenComponent";
  private const string CloseComponentMethodName = "CloseComponent";
  private const string HostFileName = "TextArea.razor";
  private const string HostGeneratedSuffix = "_TextArea_razor.g.cs";
  private const string HostGeneratedFileName = "TextArea_razor.g.cs";
  private const string DirectFluentTextAreaAttributeName = "DirectFluentTextAreaAttribute";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Multi-line text uses the shared TextArea component",
      messageFormat: "Raw FluentTextArea outside components/forms/TextArea.razor. Use <TextArea> so the box fills its field and resizes. [DirectFluentTextArea(reason)] on the component opts out.",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "FluentTextArea's visible box is its shadow part=\"root\", whose --inline-size defaults to 18rem; Width=\"100%\" on the host does not size it. The shared TextArea component sets the shadow sizing custom properties once, so pages and feature components render <TextArea>, never a raw FluentTextArea."
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  /// <summary>Registers the operation action that reports TWA0032.</summary>
  public override void Initialize(AnalysisContext context)
  {
    // Deliberately NOT GeneratedCodeAnalysisFlags.None: razor markup lives in generated trees.
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      if (!IsBlazorWebAssemblyCompilation(startContext.Options)) return;

      startContext.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    });
  }

  private static bool IsBlazorWebAssemblyCompilation(AnalyzerOptions options) =>
    options.AnalyzerConfigOptionsProvider.GlobalOptions
      .TryGetValue(BlazorWasmSdkProperty, out string? value)
    && string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

  private static void AnalyzeInvocation(OperationAnalysisContext context)
  {
    var invocation = (IInvocationOperation)context.Operation;
    if (!string.Equals(invocation.TargetMethod.Name, OpenComponentMethodName, StringComparison.Ordinal))
      return;

    if (invocation.TargetMethod.TypeArguments.Length != 1)
      return;

    if (!IsFluentTextAreaType(invocation.TargetMethod.TypeArguments[0]))
      return;

    SyntaxTree tree = invocation.Syntax.SyntaxTree;
    string path = tree.FilePath ?? string.Empty;
    if (!IsRazorGeneratedPath(path) && IsGeneratedPath(path))
      return;

    if (IsHostPath(path))
      return;

    FileLinePositionSpan mapped = invocation.Syntax.GetLocation().GetMappedLineSpan();
    if (mapped.HasMappedPath && IsHostPath(mapped.Path))
      return;

    if (HasOptOut(context.ContainingSymbol))
      return;

    context.ReportDiagnostic(Diagnostic.Create(Rule, ReportLocation(invocation)));
  }

  // Razor emits OpenComponent under #line hidden, so its own location points into the .g.cs.
  // The component's parameters carry #line mappings to the .razor tag; report on the first one
  // before CloseComponent so the error lands on the markup. Fall back to the invocation.
  private static Location ReportLocation(IInvocationOperation invocation)
  {
    Location fallback = invocation.Syntax.GetLocation();
    IOperation? statement = invocation.Parent is IExpressionStatementOperation ? invocation.Parent : invocation;
    if (statement?.Parent is not IBlockOperation block)
      return fallback;

    int index = block.Operations.IndexOf(statement);
    for (int i = index + 1; i < block.Operations.Length; i++)
    {
      IOperation operation = block.Operations[i];
      if (operation is IExpressionStatementOperation { Operation: IInvocationOperation call }
        && string.Equals(call.TargetMethod.Name, CloseComponentMethodName, StringComparison.Ordinal))
      {
        break;
      }

      foreach (SyntaxNode node in operation.Syntax.DescendantNodesAndSelf())
      {
        Location location = node.GetLocation();
        if (location.GetMappedLineSpan().HasMappedPath && IsVisibleLine(node))
          return location;
      }
    }

    return fallback;
  }

  private static bool IsVisibleLine(SyntaxNode node) =>
    node.SyntaxTree.GetLineVisibility(node.SpanStart) == LineVisibility.Visible;

  private static bool IsFluentTextAreaType(ITypeSymbol type) =>
    type.Name == FluentTextAreaTypeName
    && type.ContainingNamespace.ToDisplayString() == FluentUiNamespace;

  private static bool IsHostPath(string path)
  {
    string fileName = Path.GetFileName(path.Replace("\\", "/", StringComparison.Ordinal));
    return string.Equals(fileName, HostFileName, StringComparison.OrdinalIgnoreCase)
      || string.Equals(fileName, HostGeneratedFileName, StringComparison.OrdinalIgnoreCase)
      || fileName.EndsWith(HostGeneratedSuffix, StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsRazorGeneratedPath(string path) =>
    path.EndsWith("_razor.g.cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".razor.g.cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith("_cshtml.g.cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".cshtml.g.cs", StringComparison.OrdinalIgnoreCase);

  private static bool IsGeneratedPath(string path) =>
    path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase);

  private static bool HasOptOut(ISymbol? symbol)
  {
    for (ISymbol? current = symbol; current is not null and not INamespaceSymbol; current = current.ContainingSymbol)
    {
      if (current is INamedTypeSymbol namedType && HasOptOutAttribute(namedType))
        return true;
    }

    return false;
  }

  private static bool HasOptOutAttribute(INamedTypeSymbol type)
  {
    foreach (AttributeData attribute in type.GetAttributes())
    {
      // Match by simple name to avoid a ProjectReference on the Attributes assembly.
      if (attribute.AttributeClass?.Name != DirectFluentTextAreaAttributeName)
        continue;

      if (attribute.ConstructorArguments.Length > 0
        && attribute.ConstructorArguments[0].Value is string reason
        && !string.IsNullOrWhiteSpace(reason))
      {
        return true;
      }
    }

    return false;
  }
}
