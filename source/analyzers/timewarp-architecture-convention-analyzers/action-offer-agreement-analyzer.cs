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
// TWA0029: an offer's name must equal an EXPLICIT [CatalogAction] Name declared in this compilation.
// The derived default (<State>.<ActionSet>) deliberately does not count, so renaming an action set
// can never silently change what the server offers (task 280 requirement 1).
// TWA0030: the action's catalog parameters come from its first explicit constructor (TimeWarp.State
// ActionCatalogParameter rule; none = parameterless). Each public instance property of the record
// (JsonIgnore skipped) binds the parameter named by its wire name — [JsonPropertyName] or the
// System.Text.Json camelCase policy the contract seam uses — with the same type. Every required
// parameter must be bound or listed in UserInput; every UserInput entry must be a required
// parameter no property binds. Optional parameters may stay unbound.
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

  private static readonly DiagnosticDescriptor UnknownName =
    new
    (
      UnknownNameDiagnosticId,
      title: "Offer names no catalog action",
      messageFormat: "Offer '{0}' names catalog action '{1}', but no [CatalogAction] in this compilation sets Name = \"{1}\"; set [CatalogAction(Name = <shared constant>)] on the offered action",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "An [ActionOffer] record names the client catalog action the server offers. The action must declare the same name explicitly with [CatalogAction(Name = …)] from the shared contracts constant; the derived default name does not count, so renaming an action set cannot silently change what the server offers.",
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
      description: "An [ActionOffer] record's public properties are the arguments the server binds, keyed by camelCase name. Each must name a constructor parameter of the offered action with the same type; every required parameter must have a property or be listed in UserInput, and every UserInput entry must be a required parameter no property binds.",
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
      if (catalogActions.IsEmpty) return;

      Dictionary<string, (INamedTypeSymbol Action, AttributeData Attribute)> byName = new(StringComparer.Ordinal);
      foreach ((INamedTypeSymbol action, AttributeData attribute) in catalogActions)
      {
        if (ExplicitName(attribute) is { } name && !byName.ContainsKey(name))
        {
          byName[name] = (action, attribute);
        }
      }

      IEnumerable<INamedTypeSymbol> offers = sourceOffers.Concat(
        offerAssemblies
          .SelectMany(static assembly => HostedRouteDiscovery.GetAllTypes(assembly.GlobalNamespace))
          .Where(static type => OfferAttribute(type) is not null));

      foreach (INamedTypeSymbol offer in offers)
      {
        AttributeData offerAttribute = OfferAttribute(offer)!;
        if (offerAttribute.ConstructorArguments.FirstOrDefault().Value is not string catalogName) continue;

        if (!byName.TryGetValue(catalogName, out (INamedTypeSymbol Action, AttributeData Attribute) target))
        {
          endContext.ReportDiagnostic(Diagnostic.Create(
            UnknownName,
            offer.Locations.FirstOrDefault(location => location.SourceTree is { } tree && endContext.Compilation.ContainsSyntaxTree(tree))
              ?? Location.None,
            offer.ToDisplayString(),
            catalogName));
          continue;
        }

        Location location = target.Attribute.ApplicationSyntaxReference?.GetSyntax(endContext.CancellationToken).GetLocation()
          ?? target.Action.Locations.FirstOrDefault()
          ?? Location.None;
        foreach (string problem in Mismatches(offer, offerAttribute, target.Action))
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

  private static IEnumerable<string> Mismatches(INamedTypeSymbol offer, AttributeData offerAttribute, INamedTypeSymbol action)
  {
    IParameterSymbol[] parameters = CatalogParameters(action);
    HashSet<string> bound = new(StringComparer.Ordinal);

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

    foreach (IParameterSymbol parameter in parameters)
    {
      if (!parameter.HasExplicitDefaultValue && !bound.Contains(parameter.Name) && !userInput.Contains(parameter.Name))
      {
        yield return $"required parameter '{parameter.Name}' has no property and is not listed in UserInput";
      }
    }
  }

  /// <summary>First explicit constructor's parameters (TimeWarp.State's ActionCatalogParameter rule); none when the action declares no constructor.</summary>
  private static IParameterSymbol[] CatalogParameters(INamedTypeSymbol action)
  {
    IMethodSymbol? constructor = action.InstanceConstructors
      .Where(static candidate => !candidate.IsImplicitlyDeclared)
      .OrderBy(static candidate => candidate.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue)
      .FirstOrDefault();
    return constructor is null ? [] : [.. constructor.Parameters];
  }

  private static IEnumerable<IPropertySymbol> ArgumentProperties(INamedTypeSymbol offer)
  {
    HashSet<string> seen = new(StringComparer.Ordinal);
    for (INamedTypeSymbol? type = offer; type is not null && type.SpecialType != SpecialType.System_Object; type = type.BaseType)
    {
      foreach (IPropertySymbol property in type.GetMembers().OfType<IPropertySymbol>())
      {
        if (property.IsStatic || property.IsIndexer || property.GetMethod is null) continue;
        if (property.DeclaredAccessibility != Accessibility.Public) continue;
        if (property.GetAttributes().Any(static attribute => attribute.AttributeClass?.Name == JsonIgnoreAttributeSimpleName)) continue;
        if (!seen.Add(property.Name)) continue;
        yield return property;
      }
    }
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
