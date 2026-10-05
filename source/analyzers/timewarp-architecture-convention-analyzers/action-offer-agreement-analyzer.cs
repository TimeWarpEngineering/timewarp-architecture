#region Purpose
// Enforces TWA0029/TWA0030: every [ActionOffer] record names a [CatalogAction(Name = …)] in the SPA, and its properties match that action's constructor parameters.
#endregion

#region Design
// Task 280: hypermedia approach B lets the server offer a client catalog action by name with
// arguments keyed by the action's constructor parameters. Server code cannot reference SPA types,
// so the agreement lives in shared contracts as [ActionOffer(name)] records and is checked here, in
// the one compilation that sees both sides — the SPA (Blazor WASM SDK gate, like TWA0022: the server
// compilation also references web-spa for prerendering, but its own source declares no catalog
// actions, and the WASM gate keeps hosts and test projects silent).
// Lives in the convention analyzers (TWA), not TimeWarp.State (TWS): "offer" is this template's
// hypermedia contract (OfferedAction, ContextualActionArguments, the palette runner), not a library
// concept — TimeWarp.State only provides the catalog. If offers become a TimeWarp.State concept the
// rule moves with them.
// TWA0029: an offer's name must equal an EXPLICIT [CatalogAction] Name declared by exactly ONE action
// in this compilation. The derived default (<State>.<ActionSet>) deliberately does not count, so
// renaming an action set can never silently change what the server offers (task 280 requirement 1).
// A name two actions declare is reported too (message lists them, ordered), never resolved by
// whichever action the concurrent symbol pass saw first; TimeWarp.State's TWS0006 also rejects the
// duplicate itself. Zero catalog actions is no special case: every offer then reports TWA0029.
// Only a [CatalogAction] whose OWN declaration (attribute list's parent) is a ClassDeclarationSyntax
// counts: TimeWarp.State's generator takes ClassDeclarationSyntax targets only, so a record (or
// struct) Action is never cataloged and an offer naming it gets TWA0029.
// TWA0030: the action's catalog parameters come from the first ConstructorDeclarationSyntax among
// the DescendantNodes() of that class declaration — exactly TimeWarp.State's
// ActionSetConstructorParser (one partial declaration; a static or nested-type constructor counts
// when it comes first; none = parameterless). The constructor symbol is matched by syntax reference,
// not a semantic model (RS1030). Each public instance property of the record (inherited ones
// included; [JsonIgnore] skips one only when its Condition is absent or Always — any other condition
// still writes it) binds the parameter named by its wire name — [JsonPropertyName] or the
// System.Text.Json camelCase policy the contract seam uses — with the same type.
// Nullable properties (an annotated reference type or Nullable<T>): ContextualActionArguments.Bind
// treats a JSON null as missing whatever the parameter's own annotation, so a nullable property
// bound to a REQUIRED parameter (no default value) is a mismatch, and one bound to an optional
// parameter counts as possibly omitted in the ordering pass below. Every required parameter must
// be bound or listed in UserInput; every UserInput entry must be a required parameter no property
// binds. Optional parameters may stay unbound, but only as a trailing run: the palette merges
// UserInput into the offered arguments and Bind refuses any present argument after an omitted
// optional parameter (positional arrays cannot leave holes), so TWA0030 reports a bound parameter
// after an unbound or nullable-bound optional one. Not modelled: [JsonIgnore(Condition =
// WhenWritingNull / WhenWritingDefault)] on a non-nullable property (omitted only for a default
// value the server would not offer).
// Offer discovery: this compilation's source plus referenced assemblies that define
// ActionOfferAttribute or reference one that does (IAssemblySymbol.TypeNames is a cheap name set),
// so framework assemblies are never walked. Reported at CompilationEnd: TWA0029 on the record (or
// Location.None when the record comes from metadata — contracts are a referenced assembly), TWA0030
// on the action's [CatalogAction] attribute.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

/// <summary>Roslyn analyzer for TWA0029/TWA0030: server offer records must agree with the SPA's catalog actions.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ActionOfferAgreementAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0029 (offer names no catalog action).</summary>
  public const string UnknownNameDiagnosticId = "TWA0029";

  /// <summary>Diagnostic identifier TWA0030 (offer record does not match the action's parameters).</summary>
  public const string ArgumentMismatchDiagnosticId = "TWA0030";

  private const string Category = "Design";
  private const string BlazorWasmSdkProperty = "build_property.UsingMicrosoftNETSdkBlazorWebAssembly";
  private const string CatalogActionAttributeFullName = "TimeWarp.State.CatalogActionAttribute";
  private const string ActionOfferAttributeSimpleName = "ActionOfferAttribute";
  private const string JsonIgnoreAttributeSimpleName = "JsonIgnoreAttribute";
  private const string JsonPropertyNameAttributeSimpleName = "JsonPropertyNameAttribute";
  private const int JsonIgnoreConditionAlways = 1; // System.Text.Json.Serialization.JsonIgnoreCondition.Always

  private static readonly DiagnosticDescriptor UnknownName =
    new
    (
      UnknownNameDiagnosticId,
      title: "Offer names no single catalog action",
      messageFormat: "Offer '{0}' names catalog action '{1}', but {2}",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "An [ActionOffer] record names the client catalog action the server offers. Exactly one action must declare the same name explicitly with [CatalogAction(Name = …)] from the shared contracts constant; the derived default name does not count, so renaming an action set cannot silently change what the server offers.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  private static readonly DiagnosticDescriptor ArgumentMismatch =
    new
    (
      ArgumentMismatchDiagnosticId,
      title: "Offer record does not match the catalog action's parameters",
      messageFormat: "Offer '{0}' does not match catalog action '{1}': {2}",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "An [ActionOffer] record's public properties are the arguments the server binds, keyed by camelCase name. Each must name a constructor parameter of the offered action with the same type; every required parameter must have a property or be listed in UserInput, every UserInput entry must be a required parameter no property binds, a nullable property cannot bind a required parameter, and no argument may follow an optional parameter that is omitted or may be null.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(UnknownName, ArgumentMismatch);

  /// <summary>Registers symbol/compilation actions that report TWA0029 and TWA0030.</summary>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(StartCompilation);
  }

  private static void StartCompilation(CompilationStartAnalysisContext context)
  {
    if (!IsBlazorWebAssemblyCompilation(context.Options)) return;

    INamedTypeSymbol? catalogActionAttribute = context.Compilation.GetTypeByMetadataName(CatalogActionAttributeFullName);
    if (catalogActionAttribute is null) return;

    List<IAssemblySymbol>? offerAssemblies = OfferAssemblies(context.Compilation);
    if (offerAssemblies is null) return;

    ConcurrentBag<(INamedTypeSymbol Action, AttributeData Attribute)> catalogActions = [];
    ConcurrentBag<INamedTypeSymbol> sourceOffers = [];

    context.RegisterSymbolAction(symbolContext =>
    {
      var type = (INamedTypeSymbol)symbolContext.Symbol;
      foreach (AttributeData attribute in type.GetAttributes())
      {
        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, catalogActionAttribute))
        {
          catalogActions.Add((type, attribute));
        }
        else if (attribute.AttributeClass?.Name == ActionOfferAttributeSimpleName)
        {
          sourceOffers.Add(type);
        }
      }
    }, SymbolKind.NamedType);

    context.RegisterCompilationEndAction(endContext =>
    {
      Dictionary<string, List<(INamedTypeSymbol Action, AttributeData Attribute)>> byName = new(StringComparer.Ordinal);
      foreach ((INamedTypeSymbol action, AttributeData attribute) in catalogActions)
      {
        if (ExplicitName(attribute) is not { } name) continue;
        if (CatalogDeclaration(attribute, endContext.CancellationToken) is null) continue;
        if (!byName.TryGetValue(name, out List<(INamedTypeSymbol Action, AttributeData Attribute)>? declarers))
        {
          byName[name] = declarers = [];
        }

        declarers.Add((action, attribute));
      }

      IEnumerable<INamedTypeSymbol> offers = sourceOffers.Concat(
        offerAssemblies
          .SelectMany(static assembly => HostedRouteDiscovery.GetAllTypes(assembly.GlobalNamespace))
          .Where(static type => OfferAttribute(type) is not null));

      foreach (INamedTypeSymbol offer in offers)
      {
        AttributeData offerAttribute = OfferAttribute(offer)!;
        if (offerAttribute.ConstructorArguments.FirstOrDefault().Value is not string catalogName) continue;

        byName.TryGetValue(catalogName, out List<(INamedTypeSymbol Action, AttributeData Attribute)>? declarers);
        if (declarers is not [var target])
        {
          string problem = declarers is null
            ? $"no [CatalogAction] in this compilation sets Name = \"{catalogName}\"; set [CatalogAction(Name = <shared constant>)] on the offered action"
            : $"more than one [CatalogAction] sets that Name ({string.Join(", ", declarers.Select(static declarer => declarer.Action.ToDisplayString()).OrderBy(static name => name, StringComparer.Ordinal))}); catalog names must be unique";
          endContext.ReportDiagnostic(Diagnostic.Create(
            UnknownName,
            offer.Locations.FirstOrDefault(location => location.SourceTree is { } tree && endContext.Compilation.ContainsSyntaxTree(tree))
              ?? Location.None,
            offer.ToDisplayString(),
            catalogName,
            problem));
          continue;
        }

        Location location = target.Attribute.ApplicationSyntaxReference?.GetSyntax(endContext.CancellationToken).GetLocation()
          ?? target.Action.Locations.FirstOrDefault()
          ?? Location.None;
        foreach (string problem in Mismatches(offer, offerAttribute, target.Action, target.Attribute, endContext.CancellationToken))
        {
          endContext.ReportDiagnostic(Diagnostic.Create(
            ArgumentMismatch,
            location,
            offer.ToDisplayString(),
            catalogName,
            problem));
        }
      }
    });
  }

  private static bool IsBlazorWebAssemblyCompilation(AnalyzerOptions options) =>
    options.AnalyzerConfigOptionsProvider.GlobalOptions
      .TryGetValue(BlazorWasmSdkProperty, out string? value)
    && string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

  /// <summary>Referenced assemblies that define ActionOfferAttribute or reference one that does (source offers come from the symbol action); null when no ActionOfferAttribute is visible at all.</summary>
  private static List<IAssemblySymbol>? OfferAssemblies(Compilation compilation)
  {
    IAssemblySymbol[] referenced = [.. compilation.SourceModule.ReferencedAssemblySymbols];
    HashSet<IAssemblySymbol> definers = new(
      referenced.Where(static assembly => assembly.TypeNames.Contains(ActionOfferAttributeSimpleName)),
      SymbolEqualityComparer.Default);
    bool sourceDefines = compilation.Assembly.TypeNames.Contains(ActionOfferAttributeSimpleName);
    if (definers.Count == 0 && !sourceDefines) return null;

    return
    [
      .. referenced.Where(assembly =>
        definers.Contains(assembly)
        || assembly.Modules.Any(module => module.ReferencedAssemblySymbols.Any(definers.Contains)))
    ];
  }

  private static AttributeData? OfferAttribute(INamedTypeSymbol type) =>
    type.GetAttributes().FirstOrDefault(static attribute => attribute.AttributeClass?.Name == ActionOfferAttributeSimpleName);

  private static string? ExplicitName(AttributeData catalogAction)
  {
    foreach (KeyValuePair<string, TypedConstant> argument in catalogAction.NamedArguments)
    {
      if (argument.Key == "Name" && argument.Value.Value is string name && !string.IsNullOrWhiteSpace(name))
      {
        return name;
      }
    }

    return null;
  }

  private static IEnumerable<string> Mismatches
  (
    INamedTypeSymbol offer,
    AttributeData offerAttribute,
    INamedTypeSymbol action,
    AttributeData catalogAction,
    CancellationToken cancellationToken
  )
  {
    IParameterSymbol[] parameters = CatalogParameters(action, catalogAction, cancellationToken);
    HashSet<string> bound = new(StringComparer.Ordinal);
    HashSet<string> nullableBound = new(StringComparer.Ordinal);

    foreach (IPropertySymbol property in ArgumentProperties(offer))
    {
      string wireName = WireName(property);
      IParameterSymbol? parameter = parameters.FirstOrDefault(candidate => candidate.Name == wireName);
      if (parameter is null)
      {
        yield return $"property '{property.Name}' binds '{wireName}', which is not a constructor parameter of {action.ToDisplayString()}";
        continue;
      }

      bound.Add(wireName);
      if (!SymbolEqualityComparer.Default.Equals(property.Type, parameter.Type))
      {
        yield return $"property '{property.Name}' is {property.Type.ToDisplayString()} but parameter '{wireName}' is {parameter.Type.ToDisplayString()}";
      }
      else if (IsNullable(property))
      {
        nullableBound.Add(wireName);
        if (!parameter.HasExplicitDefaultValue)
        {
          yield return $"property '{property.Name}' is nullable but parameter '{wireName}' is required; the client binder treats null as missing";
        }
      }
    }

    string[] userInput = UserInput(offerAttribute);
    foreach (string input in userInput)
    {
      IParameterSymbol? parameter = parameters.FirstOrDefault(candidate => candidate.Name == input);
      if (parameter?.HasExplicitDefaultValue != false)
      {
        yield return $"UserInput '{input}' is not a required constructor parameter of {action.ToDisplayString()}";
      }
      else if (bound.Contains(input))
      {
        yield return $"UserInput '{input}' is also bound by a property; the user cannot replace an offered argument";
      }
    }

    string? omittedOptional = null;
    foreach (IParameterSymbol parameter in parameters)
    {
      bool present = bound.Contains(parameter.Name) || (!parameter.HasExplicitDefaultValue && userInput.Contains(parameter.Name));
      if (!present && !parameter.HasExplicitDefaultValue)
      {
        yield return $"required parameter '{parameter.Name}' has no property and is not listed in UserInput";
        continue;
      }

      if (present && omittedOptional is not null)
      {
        yield return $"parameter '{parameter.Name}' is bound after {omittedOptional}; the client binder cannot leave a positional hole";
      }

      if (parameter.HasExplicitDefaultValue && omittedOptional is null)
      {
        if (!present)
        {
          omittedOptional = $"the omitted optional parameter '{parameter.Name}'";
        }
        else if (nullableBound.Contains(parameter.Name))
        {
          omittedOptional = $"the optional parameter '{parameter.Name}', which a null (nullable property) omits";
        }
      }
    }
  }

  /// <summary>A null value is possible: an annotated reference type or Nullable&lt;T&gt;.</summary>
  private static bool IsNullable(IPropertySymbol property) =>
    property.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
    || (property.Type.IsReferenceType && property.NullableAnnotation == NullableAnnotation.Annotated);

  /// <summary>
  /// The class declaration the [CatalogAction] attribute list belongs to — what TimeWarp.State's
  /// generator targets; null for any other declaration kind (record, struct), which is never cataloged.
  /// </summary>
  private static ClassDeclarationSyntax? CatalogDeclaration(AttributeData catalogAction, CancellationToken cancellationToken) =>
    catalogAction.ApplicationSyntaxReference?.GetSyntax(cancellationToken).Parent?.Parent as ClassDeclarationSyntax;

  /// <summary>
  /// Parameters of the first constructor declared anywhere inside the class declaration carrying
  /// [CatalogAction] (TimeWarp.State's ActionSetConstructorParser); none when there is no such constructor.
  /// </summary>
  private static IParameterSymbol[] CatalogParameters(INamedTypeSymbol action, AttributeData catalogAction, CancellationToken cancellationToken)
  {
    if (CatalogDeclaration(catalogAction, cancellationToken) is not { } declaration) return [];

    ConstructorDeclarationSyntax? constructor = declaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
    if (constructor is null) return [];

    IMethodSymbol? symbol = ConstructorsWithin(action).FirstOrDefault(candidate =>
      candidate.DeclaringSyntaxReferences.Any(reference =>
        reference.SyntaxTree == constructor.SyntaxTree && reference.Span == constructor.Span));
    return symbol is null ? [] : [.. symbol.Parameters];
  }

  private static IEnumerable<IMethodSymbol> ConstructorsWithin(INamedTypeSymbol type) =>
    type.Constructors.Concat(type.GetTypeMembers().SelectMany(ConstructorsWithin));

  private static IEnumerable<IPropertySymbol> ArgumentProperties(INamedTypeSymbol offer)
  {
    HashSet<string> seen = new(StringComparer.Ordinal);
    for (INamedTypeSymbol? type = offer; type is not null && type.SpecialType != SpecialType.System_Object; type = type.BaseType)
    {
      foreach (IPropertySymbol property in type.GetMembers().OfType<IPropertySymbol>())
      {
        if (property.IsStatic || property.IsIndexer || property.GetMethod is null) continue;
        if (property.DeclaredAccessibility != Accessibility.Public) continue;
        if (property.GetAttributes().Any(IgnoredAlways)) continue;
        if (!seen.Add(property.Name)) continue;
        yield return property;
      }
    }
  }

  /// <summary>[JsonIgnore] with no Condition or Condition = Always; any other condition still writes the property.</summary>
  private static bool IgnoredAlways(AttributeData attribute)
  {
    if (attribute.AttributeClass?.Name != JsonIgnoreAttributeSimpleName) return false;

    foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
    {
      if (argument.Key == "Condition")
      {
        return argument.Value.Value is int condition && condition == JsonIgnoreConditionAlways;
      }
    }

    return true;
  }

  private static string[] UserInput(AttributeData offerAttribute)
  {
    foreach (KeyValuePair<string, TypedConstant> argument in offerAttribute.NamedArguments)
    {
      if (argument.Key == "UserInput" && argument.Value.Kind == TypedConstantKind.Array)
      {
        return [.. argument.Value.Values.Select(static value => value.Value).OfType<string>()];
      }
    }

    return [];
  }

  private static string WireName(IPropertySymbol property)
  {
    AttributeData? jsonName = property.GetAttributes()
      .FirstOrDefault(static attribute => attribute.AttributeClass?.Name == JsonPropertyNameAttributeSimpleName);
    return jsonName?.ConstructorArguments.FirstOrDefault().Value is string name
      ? name
      : CamelCase(property.Name);
  }

  /// <summary>System.Text.Json's JsonNamingPolicy.CamelCase (the contract seam's policy), ported for netstandard2.0.</summary>
  internal static string CamelCase(string name)
  {
    if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0])) return name;

    StringBuilder builder = new(name);
    for (int i = 0; i < builder.Length; i++)
    {
      if (i == 1 && !char.IsUpper(builder[i])) break;

      bool hasNext = i + 1 < builder.Length;
      if (i > 0 && hasNext && !char.IsUpper(builder[i + 1]))
      {
        if (builder[i + 1] == ' ')
        {
          builder[i] = char.ToLowerInvariant(builder[i]);
        }

        break;
      }

      builder[i] = char.ToLowerInvariant(builder[i]);
    }

    return builder.ToString();
  }
}
