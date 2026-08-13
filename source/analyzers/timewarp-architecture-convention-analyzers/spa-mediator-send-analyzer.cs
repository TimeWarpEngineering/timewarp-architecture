#region Purpose
// TWA0022: SPA client code must not call the mediator's Send directly — dispatch goes through the
// TimeWarp.State generated ActionSet methods, which wire the CancellationToken automatically.
#endregion

#region Design
// Access modifiers cannot enforce this: Mediator is a protected member inherited from
// TimeWarp.State's base component, a derived class cannot reduce inherited visibility, and
// `private new` shadowing is defeated by a base-type cast. Hence an analyzer, per the standing
// "prefer analyzers over convention-by-memory" directive (task 196).
//
// Scope is ALL SPA client code with no carve-out for handlers or pipeline behaviors: the COPIC
// sweep found 8/8 client-side direct sends replaceable, and every in-repo site — including ones
// inside IRequestHandler implementations — was a one-line swap for an already-generated dispatcher.
//
// SPA gating is `build_property.UsingMicrosoftNETSdkBlazorWebAssembly`, which the Blazor WASM SDK
// sets itself, so the rule needs zero per-project opt-in and stays correct in generated apps.
// Reference detection was rejected: web-server references web-spa for prerendering, so WASM
// assemblies flow transitively into the server compilation and the rule would misfire there.
//
// Generated-code handling is the one divergence from every sibling TWA analyzer, which use
// GeneratedCodeAnalysisFlags.None. User-authored `@code` blocks compile into `*_razor.g.cs` trees
// that Roslyn's path heuristic classifies as generated, so `.None` would blind the rule to exactly
// the component code it exists to police. Instead the rule analyzes generated trees and applies its
// own path-based exemption: razor/cshtml-generated trees ARE analyzed (their #line pragmas map
// diagnostics back to the .razor), while other generated trees are exempt. The path check is
// load-bearing rather than belt-and-braces for the TimeWarp.State dispatchers themselves: their
// emitted `*ActionSet_Method.g.cs` bodies call Sender.Send and carry NO GeneratedCodeAttribute.
// The attribute check is kept as a secondary net for generators that do mark their output.
//
// No escape-hatch attribute in v1 — `#pragma warning disable TWA0022` remains the standard Roslyn
// valve if a legitimate case ever appears.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SpaMediatorSendAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWA0022";

  private const string BlazorWasmSdkProperty = "build_property.UsingMicrosoftNETSdkBlazorWebAssembly";
  private const string SenderInterfaceFullName = "TimeWarp.Mediator.ISender";
  private const string SendMethodName = "Send";
  private const string GeneratedCodeAttributeFullName = "System.CodeDom.Compiler.GeneratedCodeAttribute";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "SPA client code must not call the mediator's Send directly",
      messageFormat: "Direct '{0}.Send' call in SPA client code; dispatch through the state's generated ActionSet method instead (name the nested action container '<Name>ActionSet' so TimeWarp.State generates one, and it wires the CancellationToken automatically)",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Client-side dispatch goes through the TimeWarp.State generated ActionSet methods, which link the component's CancellationToken into the action. A raw Mediator/ISender Send skips that wiring and cannot be prevented by access modifiers, because Mediator is inherited protected."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    // Deliberately NOT GeneratedCodeAnalysisFlags.None — razor `@code` blocks live in generated
    // trees and are the primary target of this rule. See the Design region.
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      if (!IsBlazorWebAssemblyCompilation(startContext.Options)) return;

      INamedTypeSymbol? senderInterface =
        startContext.Compilation.GetTypeByMetadataName(SenderInterfaceFullName);
      if (senderInterface is null) return;

      startContext.RegisterOperationAction
      (
        operationContext => AnalyzeInvocation(operationContext, senderInterface),
        OperationKind.Invocation
      );
    });
  }

  private static bool IsBlazorWebAssemblyCompilation(AnalyzerOptions options) =>
    options.AnalyzerConfigOptionsProvider.GlobalOptions
      .TryGetValue(BlazorWasmSdkProperty, out string? value)
    && string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

  private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol senderInterface)
  {
    var invocation = (IInvocationOperation)context.Operation;
    IMethodSymbol method = invocation.TargetMethod;

    if (!string.Equals(method.Name, SendMethodName, StringComparison.Ordinal)) return;
    if (!IsSenderType(method.ContainingType, senderInterface)) return;
    if (IsExemptGeneratedCode(context)) return;

    // Report the receiver's declared type so the message names what the author actually wrote
    // (Mediator, Sender, MediatorSender, ...), falling back to the declaring interface.
    string receiverName =
      invocation.Instance?.Type?.Name
      ?? method.ContainingType.Name;

    context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), receiverName));
  }

  private static bool IsSenderType(INamedTypeSymbol? containingType, INamedTypeSymbol senderInterface)
  {
    if (containingType is null) return false;

    if (SymbolEqualityComparer.Default.Equals(containingType.OriginalDefinition, senderInterface))
      return true;

    // Covers IMediator (: ISender) and any concrete implementation, e.g. the Mediator class or a
    // state's Sender property type.
    return containingType.AllInterfaces
      .Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, senderInterface));
  }

  private static bool IsExemptGeneratedCode(OperationAnalysisContext context)
  {
    string path = context.Operation.Syntax.SyntaxTree.FilePath ?? string.Empty;

    // Razor/cshtml-generated trees carry user-authored @code — analyze them.
    if (IsRazorGeneratedPath(path)) return false;

    if (IsGeneratedPath(path)) return true;

    // Secondary net for generators that mark their output (the TimeWarp.State ActionSet
    // dispatchers do not, which is why the path check above is load-bearing).
    return HasGeneratedCodeAttribute(context.ContainingSymbol);
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

  private static bool HasGeneratedCodeAttribute(ISymbol? symbol)
  {
    for (ISymbol? current = symbol; current is not null; current = current.ContainingSymbol)
    {
      if (current is INamespaceSymbol) break;

      if (current.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == GeneratedCodeAttributeFullName))
      {
        return true;
      }
    }

    return false;
  }
}
