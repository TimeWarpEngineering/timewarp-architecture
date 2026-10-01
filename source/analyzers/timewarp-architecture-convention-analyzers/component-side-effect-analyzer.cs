#region Purpose
// TWA0026: SPA components only dispatch TimeWarp.State actions — any member of a ComponentBase
// type that navigates, calls JS, hits an API / HttpClient, writes browser storage, or calls a
// first-party [SideEffectService] is flagged. TWA0027: [DirectComponentSideEffect] needs a reason.
#endregion

#region Design
// Task 260 adopted "every web-spa user interaction is a TimeWarp.State action, and components only
// dispatch" (tw-blazor skill, "User interactions are actions"); task 265 makes it compiler-checked
// per the standing "prefer analyzers over convention-by-memory" directive.
//
// Scope is EVERY member of a type deriving from Microsoft.AspNetCore.Components.ComponentBase,
// lifecycle overrides included (OnInitialized*, OnAfterRender*, Dispose) — not only event handlers.
// A lifecycle redirect is still a side effect the component performs; it belongs in a handler.
// The type checked is the NEAREST containing named type of the operation, so a non-component class
// nested inside a component is not a component. Handlers, services, static helpers and every other
// non-component type are never flagged — that is where the work belongs.
//
// Targets, matched on the receiver type (instance methods: ContainingType; classic extension
// methods: the `this` parameter type, so JSRuntimeExtensions.InvokeVoidAsync and
// HttpClientJsonExtensions.GetFromJsonAsync match their receivers):
//   - NavigationManager (and subtypes): NavigateTo, NavigateToLogin, Refresh.
//   - IJSRuntime / IJSObjectReference (and implementers): every member except Dispose/DisposeAsync.
//   - TimeWarp.Foundation.IApiService and every interface/class that implements it
//     (IWebServerApiService, IApiServerApiService, decorators): every member.
//   - System.Net.Http.HttpClient: Send*/Get*/Post*/Put*/Delete*/Patch* members.
//   - Blazored ISessionStorageService / ILocalStorageService (async and sync): SetItem*,
//     RemoveItem*, Clear* — reads stay legal (a component may read to render).
//   - First-party services carrying [SideEffectService] (by simple name, on the declaring type,
//     the receiver type, or any base/interface of the receiver).
// Framework types are a closed list in this analyzer because they cannot carry our attribute.
// First-party ceremony / JS-module services use the marker attribute instead of a list here: more
// are expected (every new JS module or ceremony client), and the marker keeps the rule's knowledge
// beside the service rather than in an analyzer edit. IApiService is listed by full name because it
// lives in Foundation, which does not reference the Attributes package.
//
// Both Invocation and MethodReference operations are analyzed (a method group like
// `OnClick=NavigationManager.Refresh` produces no Invocation at the reference site) — same reason
// as TWA0022.
//
// Gating and generated-code handling copy TWA0022 / TWA0025: SPA client code only
// (build_property.UsingMicrosoftNETSdkBlazorWebAssembly), razor/cshtml-generated trees ARE
// analyzed (user `@code` lives there; #line maps diagnostics to the .razor), other generated trees
// are exempt.
//
// Opt-out: [DirectComponentSideEffect("reason")] on the component class (any partial, or the
// razor `@attribute`), an enclosing type, or the member (method, property, constructor). A
// non-empty, non-whitespace reason suppresses TWA0026 in that scope. An empty or whitespace reason
// does NOT opt out and is itself reported as TWA0027 on the attribute — a separate id so the
// missing reason is visible even when the component has no remaining violation.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using Microsoft.CodeAnalysis.Operations;

/// <summary>Roslyn analyzer for TWA0026 (components only dispatch actions) and TWA0027 (opt-out needs a reason).</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentSideEffectAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0026.</summary>
  public const string DiagnosticId = "TWA0026";

  /// <summary>Diagnostic identifier TWA0027.</summary>
  public const string EmptyReasonDiagnosticId = "TWA0027";

  private const string BlazorWasmSdkProperty = "build_property.UsingMicrosoftNETSdkBlazorWebAssembly";
  private const string ComponentBaseFullName = "Microsoft.AspNetCore.Components.ComponentBase";
  private const string NavigationManagerFullName = "Microsoft.AspNetCore.Components.NavigationManager";
  private const string JsRuntimeFullName = "Microsoft.JSInterop.IJSRuntime";
  private const string JsObjectReferenceFullName = "Microsoft.JSInterop.IJSObjectReference";
  private const string ApiServiceFullName = "TimeWarp.Foundation.IApiService";
  private const string HttpClientFullName = "System.Net.Http.HttpClient";
  private const string GeneratedCodeAttributeFullName = "System.CodeDom.Compiler.GeneratedCodeAttribute";
  private const string OptOutAttributeName = "DirectComponentSideEffectAttribute";
  private const string SideEffectServiceAttributeName = "SideEffectServiceAttribute";

  private static readonly ImmutableHashSet<string> NavigationMethodNames =
    ImmutableHashSet.Create(StringComparer.Ordinal, "NavigateTo", "NavigateToLogin", "Refresh");

  private static readonly ImmutableArray<string> HttpClientMethodPrefixes =
    ImmutableArray.Create("Send", "Get", "Post", "Put", "Delete", "Patch");

  private static readonly ImmutableArray<string> StorageWriteMethodPrefixes =
    ImmutableArray.Create("SetItem", "RemoveItem", "Clear");

  private static readonly ImmutableHashSet<string> BrowserStorageFullNames =
    ImmutableHashSet.Create
    (
      StringComparer.Ordinal,
      "Blazored.SessionStorage.ISessionStorageService",
      "Blazored.SessionStorage.ISyncSessionStorageService",
      "Blazored.LocalStorage.ILocalStorageService",
      "Blazored.LocalStorage.ISyncLocalStorageService"
    );

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Components only dispatch TimeWarp.State actions",
      messageFormat: "Component calls {0} directly; dispatch a TimeWarp.State action whose handler does it",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Every SPA user interaction is a TimeWarp.State action and components only dispatch. Navigation, JS interop, API/HttpClient calls, browser-storage writes and first-party [SideEffectService] calls belong in action handlers. [DirectComponentSideEffect(reason)] on the component or member opts out."
    );

  private static readonly DiagnosticDescriptor EmptyReasonRule =
    new
    (
      EmptyReasonDiagnosticId,
      title: "[DirectComponentSideEffect] requires a non-empty reason",
      messageFormat: "[DirectComponentSideEffect] on '{0}' has an empty reason; state why the component performs the side effect itself (an empty reason does not opt out of TWA0026)",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "The TWA0026 opt-out is a stated decision, not a silent override: the reason must be non-empty and non-whitespace."
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, EmptyReasonRule);

  /// <summary>Registers operation and symbol actions that report TWA0026 / TWA0027.</summary>
  public override void Initialize(AnalysisContext context)
  {
    // Deliberately NOT GeneratedCodeAnalysisFlags.None — razor `@code` blocks live in generated
    // trees and are the primary target of this rule. See the Design region.
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      if (!IsBlazorWebAssemblyCompilation(startContext.Options)) return;

      INamedTypeSymbol? componentBase = startContext.Compilation.GetTypeByMetadataName(ComponentBaseFullName);
      if (componentBase is null) return;

      startContext.RegisterOperationAction
      (
        operationContext =>
        {
          var invocation = (IInvocationOperation)operationContext.Operation;
          Analyze(operationContext, invocation.TargetMethod, componentBase);
        },
        OperationKind.Invocation
      );

      startContext.RegisterOperationAction
      (
        operationContext =>
        {
          var reference = (IMethodReferenceOperation)operationContext.Operation;
          Analyze(operationContext, reference.Method, componentBase);
        },
        OperationKind.MethodReference
      );

      startContext.RegisterSymbolAction
      (
        AnalyzeOptOutReason,
        SymbolKind.NamedType,
        SymbolKind.Method,
        SymbolKind.Property
      );
    });
  }

  private static bool IsBlazorWebAssemblyCompilation(AnalyzerOptions options) =>
    options.AnalyzerConfigOptionsProvider.GlobalOptions
      .TryGetValue(BlazorWasmSdkProperty, out string? value)
    && string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

  private static void Analyze(OperationAnalysisContext context, IMethodSymbol method, INamedTypeSymbol componentBase)
  {
    INamedTypeSymbol? receiverType = GetReceiverType(method);
    if (receiverType is null) return;
    if (!IsSideEffect(method, receiverType)) return;
    if (!IsInComponent(context.ContainingSymbol, componentBase)) return;
    if (IsExemptGeneratedCode(context)) return;
    if (HasOptOut(context.ContainingSymbol)) return;

    string member = $"{receiverType.Name}.{method.Name}";
    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), member));
  }

  private static INamedTypeSymbol? GetReceiverType(IMethodSymbol method)
  {
    // Unreduced classic extension method: the receiver is the `this` parameter.
    if (method.IsExtensionMethod && method.ReducedFrom is null && method.Parameters.Length > 0)
    {
      return method.Parameters[0].Type as INamedTypeSymbol;
    }

    return method.ReducedFrom is not null
      ? method.ReceiverType as INamedTypeSymbol
      : method.ContainingType;
  }

  private static bool IsSideEffect(IMethodSymbol method, INamedTypeSymbol receiverType)
  {
    string name = method.Name;

    if (IsOrDerivesFrom(receiverType, NavigationManagerFullName))
      return NavigationMethodNames.Contains(name);

    if (IsOrImplements(receiverType, JsRuntimeFullName) || IsOrImplements(receiverType, JsObjectReferenceFullName))
      return !name.StartsWith("Dispose", StringComparison.Ordinal);

    if (IsOrImplements(receiverType, ApiServiceFullName))
      return true;

    if (IsOrDerivesFrom(receiverType, HttpClientFullName))
      return StartsWithAny(name, HttpClientMethodPrefixes);

    if (IsBrowserStorage(receiverType))
      return StartsWithAny(name, StorageWriteMethodPrefixes);

    return HasSideEffectServiceMarker(method.ContainingType) || HasSideEffectServiceMarker(receiverType);
  }

  private static bool StartsWithAny(string name, ImmutableArray<string> prefixes) =>
    prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));

  private static bool IsOrDerivesFrom(INamedTypeSymbol type, string fullName)
  {
    for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
    {
      if (current.ToDisplayString() == fullName) return true;
    }

    return false;
  }

  private static bool IsOrImplements(INamedTypeSymbol type, string fullName) =>
    type.OriginalDefinition.ToDisplayString() == fullName
    || type.AllInterfaces.Any(i => i.OriginalDefinition.ToDisplayString() == fullName);

  private static bool IsBrowserStorage(INamedTypeSymbol type) =>
    BrowserStorageFullNames.Contains(type.OriginalDefinition.ToDisplayString())
    || type.AllInterfaces.Any(i => BrowserStorageFullNames.Contains(i.OriginalDefinition.ToDisplayString()));

  private static bool HasSideEffectServiceMarker(INamedTypeSymbol? type)
  {
    if (type is null) return false;

    for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
    {
      if (HasAttributeNamed(current, SideEffectServiceAttributeName)) return true;
    }

    return type.AllInterfaces.Any(i => HasAttributeNamed(i, SideEffectServiceAttributeName));
  }

  private static bool HasAttributeNamed(ISymbol symbol, string attributeName) =>
    symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == attributeName);

  private static bool IsInComponent(ISymbol? containingSymbol, INamedTypeSymbol componentBase)
  {
    INamedTypeSymbol? type = null;
    for (ISymbol? current = containingSymbol; current is not null; current = current.ContainingSymbol)
    {
      if (current is INamedTypeSymbol namedType)
      {
        type = namedType;
        break;
      }
    }

    for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
    {
      if (SymbolEqualityComparer.Default.Equals(current, componentBase)) return true;
    }

    return false;
  }

  private static bool HasOptOut(ISymbol? containingSymbol)
  {
    for (ISymbol? current = containingSymbol; current is not null; current = current.ContainingSymbol)
    {
      if (current is INamespaceSymbol) break;

      if (HasReasonedOptOut(current)) return true;

      // Accessor bodies: the attribute sits on the property / event, not the accessor.
      if (current is IMethodSymbol { AssociatedSymbol: { } associated } && HasReasonedOptOut(associated))
        return true;
    }

    return false;
  }

  private static bool HasReasonedOptOut(ISymbol symbol) =>
    symbol.GetAttributes().Any(attribute =>
      attribute.AttributeClass?.Name == OptOutAttributeName
      && !string.IsNullOrWhiteSpace(GetReason(attribute)));

  private static string? GetReason(AttributeData attribute) =>
    attribute.ConstructorArguments.Length > 0
      ? attribute.ConstructorArguments[0].Value as string
      : null;

  private static void AnalyzeOptOutReason(SymbolAnalysisContext context)
  {
    foreach (AttributeData attribute in context.Symbol.GetAttributes())
    {
      if (attribute.AttributeClass?.Name != OptOutAttributeName) continue;
      if (!string.IsNullOrWhiteSpace(GetReason(attribute))) continue;

      Location location =
        attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
        ?? context.Symbol.Locations.FirstOrDefault()
        ?? Location.None;

      context.ReportDiagnostic(Diagnostic.Create(EmptyReasonRule, location, context.Symbol.Name));
    }
  }

  private static bool IsExemptGeneratedCode(OperationAnalysisContext context)
  {
    string path = context.Operation.Syntax.SyntaxTree.FilePath ?? string.Empty;

    // Razor/cshtml-generated trees carry user-authored @code — analyze them.
    if (IsRazorGeneratedPath(path)) return false;

    if (IsGeneratedPath(path)) return true;

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
