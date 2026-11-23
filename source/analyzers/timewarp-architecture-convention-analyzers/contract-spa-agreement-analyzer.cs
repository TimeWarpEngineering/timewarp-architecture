#region Purpose
// TWA0044–TWA0046: an SPA handler that sends a contract agrees with that contract — [Destructive] and ConfirmAsync,
// [CatalogAction] Permissions, and a mock response factory (task 483, the 464 Option A checks).
#endregion

#region Design
// The SPA's server-bound handlers derive from ApiHandler<TAction, TRequest, TResponse> (web-spa features/base); the
// contract is TRequest's containing class. Matching is by name (ApiHandler, arity 3) so the template's namespace
// rename does not matter. SPA gating is build_property.UsingMicrosoftNETSdkBlazorWebAssembly, as TWA0022 does.
// - TWA0044 (464 A2): a [Destructive] request (or contract) whose handler does not override ConfirmAsync, or a handler
//   that overrides ConfirmAsync for a request that is not [Destructive]. Both directions, so the SPA's confirm and the
//   transports' destructive hint cannot disagree. A [ContractState] action set is correct by construction: the
//   generator overrides ConfirmAsync exactly when the request is [Destructive] and requires the Confirm hook.
// - TWA0045 (464 A3): an Action with [CatalogAction] whose handler sends a contract must list the contract's
//   [EndpointAuthorize] Policy in Permissions, so the palette and agent tools never offer an action the server
//   refuses. Description is not compared: the 9 catalog descriptions are written for the palette and agent reader
//   ("your feedback", "Ctrl-K, the panel") and [Operation] Description for the server transports, so equality would
//   replace written-for-audience text (task 483 Notes). A [ContractState] action has no [CatalogAction] (TimeWarp.State
//   makes its catalog entry from the declaration), so the rule does not apply to it.
// - TWA0046 (464 A4): a handler that injects IWebServerApiService, or a [ContractState] contract on that service,
//   whose contract has no GetMockResponseFactory(). MockWebApiService is that service, and it otherwise falls through
//   to the real API with only a Debug log. Handlers on another API service (IApiServerApiService) are not mocked there. A contract that cannot be mocked
//   carries [NoMockResponse("reason")] (TimeWarp.Architecture.Attributes); a blank reason does not opt out.
// Retired by the generator: none. 464 A1 (SPA coverage of every contract) is not built here; see task 483 Notes.
// Generated code is not analyzed (GeneratedCodeAnalysisFlags.None): ContractState handlers are checked through the
// [ContractState] attribute instead, which is source, so the report lands where the developer can act.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;

/// <summary>Roslyn analyzer for TWA0044–TWA0046: SPA handlers agree with the contracts they send.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContractSpaAgreementAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0044 ([Destructive] and ConfirmAsync disagree).</summary>
  public const string DestructiveId = "TWA0044";

  /// <summary>Diagnostic identifier TWA0045 ([CatalogAction] disagrees with the contract).</summary>
  public const string CatalogId = "TWA0045";

  /// <summary>Diagnostic identifier TWA0046 (contract has no mock response factory).</summary>
  public const string MockId = "TWA0046";

  private const string Category = "Design";
  private const string BlazorWasmSdkProperty = "build_property.UsingMicrosoftNETSdkBlazorWebAssembly";
  private const string DestructiveAttributeName = "TimeWarp.Contracts.DestructiveAttribute";
  private const string CatalogActionAttributeName = "TimeWarp.State.CatalogActionAttribute";
  private const string MockFactoryName = "GetMockResponseFactory";
  private const string WebServerApiServiceName = "IWebServerApiService";

  private static readonly DiagnosticDescriptor Destructive = new
  (
    DestructiveId,
    title: "[Destructive] and ConfirmAsync disagree",
    messageFormat: "Handler '{0}' sends '{1}': {2}",
    Category,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "A [Destructive] request is confirmed before it is sent, and a confirmed request is [Destructive], so the SPA and the transports' destructive hint agree."
  );

  private static readonly DiagnosticDescriptor Catalog = new
  (
    CatalogId,
    title: "[CatalogAction] disagrees with the contract it sends",
    messageFormat: "[CatalogAction] on '{0}' (sends '{1}'): {2}",
    Category,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "The catalog's Permissions include the contract's [EndpointAuthorize] Policy, so the palette and agent tools never offer an action the server refuses."
  );

  private static readonly DiagnosticDescriptor Mock = new
  (
    MockId,
    title: "SPA sends a contract that has no mock response factory",
    messageFormat: "'{0}' sends '{1}', which has no static {2}(); add one, or mark the contract [NoMockResponse(reason)]",
    Category,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "Without a factory the mock service sends the request to the real API and logs only at Debug."
  );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Destructive, Catalog, Mock);

  /// <summary>Registers the named-type action that reports TWA0044–TWA0046.</summary>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static start =>
    {
      if (!start.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(BlazorWasmSdkProperty, out string? wasm)
          || !string.Equals(wasm, "true", System.StringComparison.OrdinalIgnoreCase))
      {
        return;
      }

      start.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    });
  }

  private static void AnalyzeType(SymbolAnalysisContext context)
  {
    var type = (INamedTypeSymbol)context.Symbol;
    if (type.Locations.FirstOrDefault(static l => l.IsInSource) is not { } location) return;

    AnalyzeContractState(context, type);

    if (type.IsAbstract || FindApiHandler(type) is not { } apiHandler) return;
    if (apiHandler.TypeArguments[0] is not INamedTypeSymbol action || apiHandler.TypeArguments[1] is not INamedTypeSymbol request) return;
    INamedTypeSymbol? contract = request.ContainingType;
    string handlerName = type.ToDisplayString();
    string requestName = request.ToDisplayString();

    bool destructive = IsDestructive(request) ?? (contract is null ? null : IsDestructive(contract)) ?? false;
    bool confirms = OverridesConfirm(type, apiHandler);
    if (destructive && !confirms)
    {
      context.ReportDiagnostic(Diagnostic.Create(Destructive, location, handlerName, requestName,
        "the request is [Destructive] but the handler does not override ConfirmAsync; ask before sending it"));
    }
    else if (!destructive && confirms)
    {
      context.ReportDiagnostic(Diagnostic.Create(Destructive, location, handlerName, requestName,
        "the handler overrides ConfirmAsync but the request is not [Destructive]; mark the request [Destructive]"));
    }

    if (contract is not null)
    {
      AnalyzeCatalog(context, action, request, contract);
      if (InjectsWebServerApi(type) && !HasMockFactory(contract))
        context.ReportDiagnostic(Diagnostic.Create(Mock, location, handlerName, requestName, MockFactoryName));
    }
  }

  private static void AnalyzeContractState(SymbolAnalysisContext context, INamedTypeSymbol type)
  {
    foreach (AttributeData attribute in type.GetAttributes().Where(static a => a.AttributeClass?.Name == "ContractStateAttribute"))
    {
      TypedConstant apiService = attribute.NamedArguments.FirstOrDefault(static a => a.Key == "ApiService").Value;
      if (apiService.Value is INamedTypeSymbol { Name: not WebServerApiServiceName }) continue;

      Location location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
        ?? type.Locations[0];
      IEnumerable<INamedTypeSymbol> contracts = attribute.ConstructorArguments
        .SelectMany(static argument => argument.Kind == TypedConstantKind.Array ? argument.Values : [argument])
        .Select(static value => value.Value)
        .OfType<INamedTypeSymbol>();
      foreach (INamedTypeSymbol contract in contracts.Where(static c => !HasMockFactory(c)))
      {
        context.ReportDiagnostic(Diagnostic.Create(Mock, location, type.ToDisplayString(), contract.ToDisplayString(), MockFactoryName));
      }
    }
  }

  private static void AnalyzeCatalog(SymbolAnalysisContext context, INamedTypeSymbol action, INamedTypeSymbol request, INamedTypeSymbol contract)
  {
    AttributeData? catalog = action.GetAttributes().FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == CatalogActionAttributeName);
    if (catalog is null) return;

    Location location = catalog.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? action.Locations[0];
    string actionName = action.ToDisplayString();
    string requestName = request.ToDisplayString();

    string? policy = NamedString(contract.GetAttributes().FirstOrDefault(static a => a.AttributeClass?.Name == "EndpointAuthorizeAttribute"), "Policy");
    if (!string.IsNullOrEmpty(policy))
    {
      TypedConstant permissions = catalog.NamedArguments.FirstOrDefault(static a => a.Key == "Permissions").Value;
      bool listed = permissions.Kind == TypedConstantKind.Array && !permissions.IsNull
        && permissions.Values.Any(value => string.Equals(value.Value as string, policy, System.StringComparison.Ordinal));
      if (!listed)
      {
        context.ReportDiagnostic(Diagnostic.Create(Catalog, location, actionName, requestName,
          $"Permissions must include the contract's [EndpointAuthorize] Policy \"{policy}\""));
      }
    }
  }

  private static bool InjectsWebServerApi(INamedTypeSymbol handler) =>
    handler.InstanceConstructors.SelectMany(static c => c.Parameters).Any(static p => p.Type.Name == WebServerApiServiceName);

  private static INamedTypeSymbol? FindApiHandler(INamedTypeSymbol type)
  {
    for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
    {
      if (current.Name == "ApiHandler" && current.TypeArguments.Length == 3) return current;
    }

    return null;
  }

  private static bool OverridesConfirm(INamedTypeSymbol type, INamedTypeSymbol apiHandler)
  {
    for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
    {
      if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, apiHandler.OriginalDefinition)) return false;
      if (current.GetMembers("ConfirmAsync").OfType<IMethodSymbol>().Any(static m => m.IsOverride)) return true;
    }

    return false;
  }

  private static bool? IsDestructive(INamedTypeSymbol symbol)
  {
    AttributeData? attribute = symbol.GetAttributes().FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == DestructiveAttributeName);
    return attribute is null ? null : attribute.ConstructorArguments.FirstOrDefault().Value is not false;
  }

  private static bool HasMockFactory(INamedTypeSymbol contract)
  {
    // Same predicate as MockResponseFactoryRegistryGenerator: only a public, static, parameterless factory is registered.
    bool registered = contract.GetMembers(MockFactoryName).OfType<IMethodSymbol>()
      .Any(static m => m is { IsStatic: true, Parameters.Length: 0, DeclaredAccessibility: Accessibility.Public });
    if (registered) return true;
    AttributeData? optOut = contract.GetAttributes().FirstOrDefault(static a => a.AttributeClass?.Name == "NoMockResponseAttribute");
    return optOut?.ConstructorArguments.FirstOrDefault().Value is string reason && !string.IsNullOrWhiteSpace(reason);
  }

  private static string? NamedString(AttributeData? attribute, string name) =>
    attribute?.NamedArguments.FirstOrDefault(argument => argument.Key == name).Value.Value as string;
}
