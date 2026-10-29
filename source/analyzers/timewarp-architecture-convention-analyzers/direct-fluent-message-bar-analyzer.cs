#region Purpose
// TWA0037: Message bars in the SPA render through the shared components/MessageBar.razor.
// A raw FluentMessageBar anywhere but that control and the shell MessageBars host is reported.
#endregion

#region Design
// Every message bar carries the icon-only Copy button (task 313 on shell rows, task 445 on
// page-local bars). MessageBar.razor paints it once; this rule turns "remember to use the
// control" into a build failure (standing directive: prefer analyzers over convention-by-memory).
//
// Detection: an OpenComponent<FluentMessageBar> invocation (Microsoft.FluentUI.AspNetCore.Components)
// in a Blazor WASM compilation. Razor markup and hand-written RenderTreeBuilder code both compile
// to that call. Scope and generated-code handling follow TWA0032: razor/cshtml-generated trees are
// analyzed, other generated trees are exempt. Non-WASM compilations (analyzer test fixtures that
// stub FluentMessageBar, server projects) are never analyzed.
//
// Host exemption is by type identity: the invocation's containing type is MessageBar (the
// control) or MessageBars (the shell host) in namespace {RootNamespace}.Components, read from
// build_property.RootNamespace so it survives a template root namespace rename. Both razor files
// declare @namespace {RootNamespace}.Components, and a type name is unique per namespace, so no
// other file can claim the exemption. A same-named component in a feature folder (for example
// features/identity/components/MessageBar.razor, namespace ...Features.Identity.Components or the
// feature's own @namespace) is still reported. The file path is not consulted: TWA0032's
// path-suffix match would exempt any .../components/MessageBar.razor. With no RootNamespace
// there is no exemption.
// No opt-out attribute: a bar that should not offer Copy is a MessageBar parameter, not a raw
// FluentMessageBar.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using Microsoft.CodeAnalysis.Operations;

/// <summary>Roslyn analyzer for TWA0037: FluentMessageBar renders only inside the shared MessageBar control and the shell host.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DirectFluentMessageBarAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0037.</summary>
  public const string DiagnosticId = "TWA0037";

  private const string BlazorWasmSdkProperty = "build_property.UsingMicrosoftNETSdkBlazorWebAssembly";
  private const string RootNamespaceProperty = "build_property.RootNamespace";
  private const string ComponentsNamespaceSuffix = ".Components";
  private const string FluentMessageBarTypeName = "FluentMessageBar";
  private const string FluentUiNamespace = "Microsoft.FluentUI.AspNetCore.Components";
  private const string OpenComponentMethodName = "OpenComponent";
  private const string CloseComponentMethodName = "CloseComponent";
  private static readonly string[] HostNames = ["MessageBar", "MessageBars"];

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Message bars use the shared MessageBar component",
      messageFormat: "Raw FluentMessageBar outside components/MessageBar.razor. Use <MessageBar> so the bar has its Copy button.",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Every message bar offers an icon-only Copy button that copies its title and body. The shared MessageBar component paints that button once, so pages, feature components, and the shell MessageBars host render <MessageBar>, never a raw FluentMessageBar."
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  /// <summary>Registers the operation action that reports TWA0037.</summary>
  public override void Initialize(AnalysisContext context)
  {
    // Deliberately NOT GeneratedCodeAnalysisFlags.None: razor markup lives in generated trees.
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      if (!IsBlazorWebAssemblyCompilation(startContext.Options)) return;

      string? hostNamespace = ResolveHostNamespace(startContext.Options);
      startContext.RegisterOperationAction
      (
        operationContext => AnalyzeInvocation(operationContext, hostNamespace),
        OperationKind.Invocation
      );
    });
  }

  private static bool IsBlazorWebAssemblyCompilation(AnalyzerOptions options) =>
    options.AnalyzerConfigOptionsProvider.GlobalOptions
      .TryGetValue(BlazorWasmSdkProperty, out string? value)
    && string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

  private static string? ResolveHostNamespace(AnalyzerOptions options) =>
    options.AnalyzerConfigOptionsProvider.GlobalOptions
      .TryGetValue(RootNamespaceProperty, out string? rootNamespace)
    && !string.IsNullOrWhiteSpace(rootNamespace)
      ? rootNamespace.Trim() + ComponentsNamespaceSuffix
      : null;

  private static void AnalyzeInvocation(OperationAnalysisContext context, string? hostNamespace)
  {
    var invocation = (IInvocationOperation)context.Operation;
    if (!string.Equals(invocation.TargetMethod.Name, OpenComponentMethodName, StringComparison.Ordinal))
      return;

    if (invocation.TargetMethod.TypeArguments.Length != 1)
      return;

    if (!IsFluentMessageBarType(invocation.TargetMethod.TypeArguments[0]))
      return;

    SyntaxTree tree = invocation.Syntax.SyntaxTree;
    string path = tree.FilePath ?? string.Empty;
    if (!IsRazorGeneratedPath(path) && IsGeneratedPath(path))
      return;

    if (hostNamespace is not null && IsHostType(context.ContainingSymbol, hostNamespace))
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

  private static bool IsFluentMessageBarType(ITypeSymbol type) =>
    type.Name == FluentMessageBarTypeName
    && type.ContainingNamespace.ToDisplayString() == FluentUiNamespace;

  // The outermost type containing the render call. Razor emits BuildRenderTree on the component
  // class itself; walking out of nested types keeps a helper type inside the control exempt.
  private static bool IsHostType(ISymbol containingSymbol, string hostNamespace)
  {
    INamedTypeSymbol? type = containingSymbol as INamedTypeSymbol ?? containingSymbol.ContainingType;
    while (type?.ContainingType is not null)
      type = type.ContainingType;

    return type is not null
      && Array.IndexOf(HostNames, type.Name) >= 0
      && string.Equals(type.ContainingNamespace.ToDisplayString(), hostNamespace, StringComparison.Ordinal);
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
}
