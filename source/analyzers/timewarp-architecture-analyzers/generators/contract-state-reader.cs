#region Purpose
// Reads one [ContractState] state into a ContractStateModel: the contracts, their request and response, which hooks are
// required, and the web-spa types the generated handlers derive from.
#endregion

#region Design
// The attribute is matched by simple name ContractStateAttribute plus a base chain that reaches
// TimeWarp.Mediator.GeneratedRequestPatternAttribute, so the template's sourceName rename of the web-spa namespace
// does not matter. The web-spa types (IBaseAction, DefaultApiHandler`3) are looked up in the attribute's namespace,
// where they live together in features/base. IWebServerApiService and IConfirmationService are found by name in the
// host's own source; ApiService = typeof(...) on the attribute overrides the API service.
// Contract shape: a static contract class with exactly one nested Query or Command and a nested Response, the
// shape every [ApiEndpoint] contract has. Anything else is TWE015 and the contract is skipped, so one bad contract
// does not hide the others.
// Request copy: every public, non-static, settable (set or init) property of the request, base types included, is
// copied into a new request in the handler, so the page's bound model is never changed. Nothing is stamped: a
// request does not name its caller (task 458-008 removed IAuthApiRequest.UserId).
// [Destructive] is read from the request first, then the contract class, the same rule EndpointMetadata uses for the
// transports' hint. A [Destructive(false)] reads as not destructive.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;
using System.Collections.Generic;
using System.Threading;

internal static class ContractStateReader
{
  internal const string AttributeName = "ContractStateAttribute";
  private const string PatternAttributeMetadataName = "TimeWarp.Mediator.GeneratedRequestPatternAttribute";
  private const string DestructiveAttributeMetadataName = "TimeWarp.Contracts.DestructiveAttribute";
  private const string TrackActionMetadataName = "TimeWarp.Features.ActionTracking.TrackActionAttribute";
  private const string DefaultApiServiceName = "IWebServerApiService";
  private const string ConfirmationServiceName = "IConfirmationService";
  private static readonly SymbolDisplayFormat Qualified = SymbolDisplayFormat.FullyQualifiedFormat;

  /// <summary>True when the class syntax has an attribute whose name could be [ContractState].</summary>
  public static bool IsCandidate(SyntaxNode node) =>
    node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } declaration
    && declaration.AttributeLists.SelectMany(static list => list.Attributes).Any(static attribute => IsContractStateName(attribute.Name));

  /// <summary>Reads the state the syntax declares, or null when it carries no [ContractState].</summary>
  public static ContractStateModel? Read(GeneratorSyntaxContext context, CancellationToken cancellationToken)
  {
    if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol state) return null;
    AttributeData? attribute = state.GetAttributes().FirstOrDefault(IsContractState);
    if (attribute is null) return null;
    if (attribute.ApplicationSyntaxReference?.SyntaxTree != context.Node.SyntaxTree) return null;

    return Read(state, attribute, context.SemanticModel.Compilation, cancellationToken);
  }

  internal static ContractStateModel Read(INamedTypeSymbol state, AttributeData attribute, Compilation compilation, CancellationToken cancellationToken)
  {
    List<ContractStateDiagnostic> diagnostics = [];
    Location location = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None;
    void Report(string message) => diagnostics.Add(ToDiagnostic(message, location));

    string featuresNamespace = attribute.AttributeClass!.ContainingNamespace.ToDisplayString();
    INamedTypeSymbol? baseAction = compilation.GetTypeByMetadataName(featuresNamespace + ".IBaseAction");
    INamedTypeSymbol? handlerBase = compilation.GetTypeByMetadataName(featuresNamespace + ".DefaultApiHandler`3");
    if (baseAction is null) Report($"{featuresNamespace}.IBaseAction was not found; [ContractState] needs it beside the attribute");
    if (handlerBase is null) Report($"{featuresNamespace}.DefaultApiHandler<TAction, TRequest, TResponse> was not found; [ContractState] needs it beside the attribute");

    IMethodSymbol? handlerConstructor = handlerBase?.InstanceConstructors.FirstOrDefault(static c => c.Parameters.Length >= 4);
    ITypeSymbol? storeType = handlerConstructor?.Parameters[0].Type;
    ITypeSymbol? publisherType = handlerConstructor?.Parameters[3].Type;
    if (handlerBase is not null && (storeType is null || publisherType is null))
      Report("DefaultApiHandler has no (store, apiService, logger, publisher, …) constructor");

    ITypeSymbol? apiService = NamedTypeArgument(attribute, "ApiService") ?? FindSourceType(compilation, DefaultApiServiceName, cancellationToken);
    if (apiService is null) Report($"no {DefaultApiServiceName} in this project; set ApiService = typeof(...) on [ContractState]");

    if (state.IsGenericType) Report($"{state.Name} is generic; a [ContractState] state must not be");

    HashSet<INamedTypeSymbol> drafts = new(NamedTypeArrayArgument(attribute, "DraftRequests"), SymbolEqualityComparer.Default);
    List<INamedTypeSymbol> contracts = ContractArguments(attribute);
    foreach (INamedTypeSymbol draft in drafts.Where(draft => !contracts.Contains(draft, SymbolEqualityComparer.Default)))
      Report($"DraftRequests names {draft.Name}, which is not one of the [ContractState] contracts");

    List<ContractActionModel> actions = [];
    HashSet<string> names = new(StringComparer.Ordinal);
    foreach (INamedTypeSymbol contract in contracts)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (!names.Add(contract.Name))
      {
        Report($"{contract.Name} is listed twice, or two contracts share the name");
        continue;
      }

      if (state.GetMembers(contract.Name + "ActionSet").Any() || state.GetMembers(contract.Name).Any())
      {
        Report($"{state.Name} already declares {contract.Name}ActionSet or {contract.Name}; remove the hand-written action set or the contract from [ContractState]");
        continue;
      }

      ContractActionModel? action = ReadContract(contract, drafts.Contains(contract), Report);
      if (action is not null) actions.Add(action);
    }

    INamedTypeSymbol? confirmation = actions.Any(static a => a.IsDestructive)
      ? FindSourceType(compilation, ConfirmationServiceName, cancellationToken)
      : null;
    if (actions.Any(static a => a.IsDestructive) && confirmation is null)
      Report($"a [Destructive] contract needs {ConfirmationServiceName} in this project");

    List<string> headers = [];
    List<string> hint = [];
    for (INamedTypeSymbol? current = state; current is not null; current = current.ContainingType)
    {
      headers.Insert(0, $"partial {(current.IsRecord ? "record" : "class")} {current.Name}");
      hint.Insert(0, current.Name);
    }

    string? ns = state.ContainingNamespace is { IsGlobalNamespace: false } containing ? containing.ToDisplayString() : null;
    // Missing infrastructure emits nothing for the state (TWE015 says why); a bad contract only drops that contract.
    bool usable = baseAction is not null && storeType is not null && publisherType is not null && apiService is not null
      && !state.IsGenericType && (confirmation is not null || !actions.Any(static a => a.IsDestructive));

    return new ContractStateModel
    (
      ns,
      new EquatableArray<string>(headers),
      state.Name,
      state.ToDisplayString(Qualified),
      $"{ns ?? "Global"}.{string.Join("+", hint)}.ContractState.g.cs",
      baseAction?.ToDisplayString(Qualified) ?? "",
      handlerBase is null ? "" : "global::" + featuresNamespace + ".DefaultApiHandler",
      storeType?.ToDisplayString(Qualified) ?? "",
      publisherType?.ToDisplayString(Qualified) ?? "",
      apiService?.ToDisplayString(Qualified) ?? "",
      confirmation?.ToDisplayString(Qualified),
      compilation.GetTypeByMetadataName(TrackActionMetadataName)?.ToDisplayString(Qualified),
      new EquatableArray<ContractActionModel>(usable ? actions : []),
      new EquatableArray<ContractStateDiagnostic>(diagnostics)
    );
  }

  /// <summary>True for [ContractState]: simple name ContractStateAttribute deriving from GeneratedRequestPatternAttribute.</summary>
  public static bool IsContractState(AttributeData attribute)
  {
    if (attribute.AttributeClass is not { Name: AttributeName } attributeClass) return false;
    for (INamedTypeSymbol? current = attributeClass.BaseType; current is not null; current = current.BaseType)
    {
      if (current.ToDisplayString() == PatternAttributeMetadataName) return true;
    }

    return false;
  }

  private static ContractActionModel? ReadContract(INamedTypeSymbol contract, bool isDraft, Action<string> report)
  {
    INamedTypeSymbol[] requests = [.. contract.GetTypeMembers().Where(static t => t.Name is "Query" or "Command")];
    INamedTypeSymbol? response = contract.GetTypeMembers("Response").FirstOrDefault();
    if (requests.Length != 1 || response is null)
    {
      report($"{contract.Name} needs exactly one nested Query or Command and a nested Response to be a [ContractState] contract");
      return null;
    }

    INamedTypeSymbol request = requests[0];
    bool hasParameterlessConstructor = request.InstanceConstructors
      .Any(static c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
    if (!hasParameterlessConstructor)
    {
      report($"{contract.Name}.{request.Name} has no public parameterless constructor, so the handler cannot copy it");
      return null;
    }

    List<string> copied = [];
    HashSet<string> seen = new(StringComparer.Ordinal);
    for (INamedTypeSymbol? current = request; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
    {
      foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
      {
        if (property.IsStatic || property.IsIndexer || property.DeclaredAccessibility != Accessibility.Public) continue;
        if (property.SetMethod is not { DeclaredAccessibility: Accessibility.Public }) continue;
        if (!seen.Add(property.Name)) continue;

        copied.Add(property.Name);
      }
    }

    copied.Sort(StringComparer.Ordinal);
    bool isQuery = request.Name == "Query";
    return new ContractActionModel
    (
      contract.Name,
      request.ToDisplayString(Qualified),
      response.ToDisplayString(Qualified),
      isQuery,
      isDraft,
      IsDestructive(request) ?? IsDestructive(contract) ?? false,
      RequestIsOptional: isQuery,
      new EquatableArray<string>(copied)
    );
  }

  private static bool? IsDestructive(INamedTypeSymbol symbol)
  {
    AttributeData? attribute = symbol.GetAttributes()
      .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == DestructiveAttributeMetadataName);
    if (attribute is null) return null;
    return attribute.ConstructorArguments.FirstOrDefault().Value is not false;
  }

  private static List<INamedTypeSymbol> ContractArguments(AttributeData attribute) =>
  [
    .. attribute.ConstructorArguments
      .SelectMany(static argument => argument.Kind == TypedConstantKind.Array ? argument.Values : [argument])
      .Select(static value => value.Value)
      .OfType<INamedTypeSymbol>()
  ];

  private static IEnumerable<INamedTypeSymbol> NamedTypeArrayArgument(AttributeData attribute, string name) =>
    attribute.NamedArguments
      .Where(argument => argument.Key == name && argument.Value.Kind == TypedConstantKind.Array && !argument.Value.IsNull)
      .SelectMany(static argument => argument.Value.Values)
      .Select(static value => value.Value)
      .OfType<INamedTypeSymbol>();

  private static INamedTypeSymbol? NamedTypeArgument(AttributeData attribute, string name) =>
    attribute.NamedArguments
      .Where(argument => argument.Key == name)
      .Select(static argument => argument.Value.Value)
      .OfType<INamedTypeSymbol>()
      .FirstOrDefault();

  private static INamedTypeSymbol? FindSourceType(Compilation compilation, string name, CancellationToken cancellationToken) =>
    compilation.GetSymbolsWithName(candidate => candidate == name, SymbolFilter.Type, cancellationToken)
      .OfType<INamedTypeSymbol>()
      .FirstOrDefault(static type => type.TypeKind == TypeKind.Interface);

  private static bool IsContractStateName(NameSyntax name)
  {
    string text = name switch
    {
      QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
      AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
      SimpleNameSyntax simple => simple.Identifier.Text,
      _ => name.ToString(),
    };
    return text is "ContractState" or AttributeName;
  }

  private static ContractStateDiagnostic ToDiagnostic(string message, Location location)
  {
    FileLinePositionSpan span = location.GetLineSpan();
    return new ContractStateDiagnostic(message, span.Path ?? "", location.SourceSpan, span.Span);
  }
}
