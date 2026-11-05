#region Purpose
// Proves a hand-written aggregate root satisfies the contracts of the [Operation] group it serves
// (TWA0039–TWA0043): details members exist on the root, Set-list commands have a mutation, query
// responses project from root members, and every other command in the group is accounted for.
#endregion

#region Design
// Task 461 resolution (Steve, 2026-11-05): no entity generator. Contracts are UI-first, the domain
// is domain-first, and this build check proves they agree. It runs where both sides are visible as
// metadata: the web-application compilation references web-contracts and web-domain. A
// CompilationAction scans the compilation's own types and every referenced assembly that
// references the IAggregateRoot assembly (roots) or the TimeWarp.Contracts assembly (operations);
// nothing else is enumerated. It reads symbols only (attributes, members, types).
// Reported once: a compilation skips a root/group pair when one of its references already sees
// the root's assembly and every operation assembly of the group, because that reference's own
// compilation reported it. web-application reports; web-server, web-infrastructure, and test
// projects that see both through web-application stay quiet.
// Linking rule (recorded in tw-aggregate-pattern): a root's slice namespace is its namespace with
// a trailing ".Domain" segment removed (Features.Admin.Roles.Domain -> Features.Admin.Roles). The
// root satisfies every operation group that has at least one operation whose request type is in
// that namespace; all operations of a linked group are then checked, wherever they live. No
// declaration on the entity. A group whose operations sit in the slice namespace of two or more
// roots is ambiguous: TWA0043 (warning) and no checks for it.
// Operations: types with TimeWarp.Contracts.OperationAttribute (FQN, like TWA0035) and a
// non-blank Group. Kind is the mediator interface: IQuery<T> is a query, anything else a command.
// The response is the first OneOf argument. The operation's name for Set detection is its
// container class (SetRolePermissions), since the request itself is always Command or Query.
// [NotAggregate("reason")] on the request removes it from every check; a blank reason does not.
// Checks per linked group, members matched by exact name (ContractAgreementTypes Design):
//   TWA0039 (error): each property of each I*Details interface a command implements exists on the
//     root with a compatible type. A details interface is an interface named I<Something>Details.
//   TWA0040 (error): a command whose container is named Set<Name> and declares exactly one
//     collection property needs a public root method Set<Name> (the root's own name stripped from
//     <Name> when it leads: SetRolePermissions on Role -> SetPermissions) with one collection
//     parameter of a compatible element type.
//   TWA0041 (error): each query-response member maps to a root member. A ListResponse<T> response
//     is checked through T. Members inherited from another assembly (BaseResponse, ListResponse)
//     are skipped. [Computed] on the response, the item class, or the member skips it. Child
//     classes and a wrapped projection are walked too (ContractAgreementProjection, task 475); the
//     message then names the child or wrapped class and the type it projects from.
//   TWA0042 (warning): a command that implements no details interface and is not a Set-list
//     command. Opt out with [NotAggregate("reason")] on the request.
// Drift is an error, not a silent mapping: the only name rule is the "<Root>Id" key (see Types).
// Locations: a symbol's own source location when it is in this compilation, else Location.None
// (metadata). The message names the root, the contract, and the member, so no location is needed.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Roslyn analyzer for TWA0039–TWA0043: an aggregate root agrees with its operation group's contracts.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ContractAgreementAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0039 (details member missing or incompatible on the root).</summary>
  public const string DetailsMemberId = "TWA0039";
  /// <summary>Diagnostic identifier TWA0040 (Set-list command has no matching root mutation).</summary>
  public const string SetMutationId = "TWA0040";
  /// <summary>Diagnostic identifier TWA0041 (query-response member does not project from the root).</summary>
  public const string ProjectionId = "TWA0041";
  /// <summary>Diagnostic identifier TWA0042 (command in a rooted group that no check covers).</summary>
  public const string UncoveredCommandId = "TWA0042";
  /// <summary>Diagnostic identifier TWA0043 (group links to more than one root).</summary>
  public const string AmbiguousRootId = "TWA0043";

  private const string Category = "Design";
  private const string OperationAttributeFullName = "TimeWarp.Contracts.OperationAttribute";
  private const string AggregateRootFullName = "TimeWarp.Foundation.Entities.IAggregateRoot";
  private const string NotAggregateAttributeName = "NotAggregateAttribute";
  private const string DomainNamespaceSuffix = ".Domain";
  private const string SetPrefix = "Set";

  private static readonly DiagnosticDescriptor DetailsMember =
    new
    (
      DetailsMemberId,
      title: "Aggregate root does not satisfy its group's details interface",
      messageFormat: "'{0}' does not satisfy '{1}.{2}' (operation group '{3}'): {4}",
      Category,
      DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Every member of an I*Details interface that a command in the group implements must exist on the aggregate root by exact name with a compatible type. Rename the side that drifted; names are never mapped.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  private static readonly DiagnosticDescriptor SetMutation =
    new
    (
      SetMutationId,
      title: "Set command has no matching mutation on the aggregate root",
      messageFormat: "'{0}' replaces one list ('{1}' of {2}), so '{3}' needs a public mutation '{4}' taking a collection of {2} (operation group '{5}')",
      Category,
      DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "A command named Set<Name> with exactly one collection member is a full replace of one child set; the root must expose Set<Name>(collection) with a compatible element type.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  private static readonly DiagnosticDescriptor Projection =
    new
    (
      ProjectionId,
      title: "Query response member does not project from the aggregate root",
      messageFormat: "'{0}.{1}' does not project from '{2}' (operation group '{3}'): {4}",
      Category,
      DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Each member of a query response in the group must map to a root member by exact name and compatible type. A value the handler computes is marked [Computed] on the member or the response.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  private static readonly DiagnosticDescriptor UncoveredCommand =
    new
    (
      UncoveredCommandId,
      title: "Command in a rooted operation group matches no agreement check",
      messageFormat: "'{0}' ('{1}') is in operation group '{2}', which '{3}' satisfies, but implements no I*Details interface and is not a Set command with one list; mark it [NotAggregate(\"reason\")] if it does not write the root",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Every command in a group linked to an aggregate root is either checked against the root or says why not. [NotAggregate] with a non-blank reason is the opt-out.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  private static readonly DiagnosticDescriptor AmbiguousRoot =
    new
    (
      AmbiguousRootId,
      title: "Operation group links to more than one aggregate root",
      messageFormat: "Operation group '{0}' has operations in the slice namespace of more than one aggregate root ({1}); no agreement check runs for it. Give each root's operations their own group.",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "A root links to the groups with operations in its slice namespace (its namespace without a trailing .Domain). Two roots in one slice namespace sharing a group cannot both be checked.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(DetailsMember, SetMutation, Projection, UncoveredCommand, AmbiguousRoot);

  /// <summary>Registers the compilation action that reports TWA0039–TWA0043.</summary>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationAction(Analyze);
  }

  private sealed record OperationShape
  (
    INamedTypeSymbol Request,
    string Name,
    string Group,
    string ContainerName,
    bool IsQuery,
    ITypeSymbol? Response,
    bool NotAggregate
  );

  private static void Analyze(CompilationAnalysisContext context)
  {
    Compilation compilation = context.Compilation;
    INamedTypeSymbol? operationAttribute = compilation.GetTypeByMetadataName(OperationAttributeFullName);
    INamedTypeSymbol? aggregateRoot = compilation.GetTypeByMetadataName(AggregateRootFullName);
    if (operationAttribute is null || aggregateRoot is null) return;

    List<INamedTypeSymbol> roots = [];
    List<OperationShape> operations = [];
    foreach (IAssemblySymbol assembly in new[] { compilation.Assembly }.Concat(compilation.SourceModule.ReferencedAssemblySymbols))
    {
      bool own = SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly);
      bool scanRoots = own || Sees(assembly, aggregateRoot.ContainingAssembly);
      bool scanOperations = own || Sees(assembly, operationAttribute.ContainingAssembly);
      if (!scanRoots && !scanOperations) continue;

      foreach (INamedTypeSymbol type in AllTypes(assembly.GlobalNamespace))
      {
        context.CancellationToken.ThrowIfCancellationRequested();
        if (scanRoots && IsAggregateRoot(type, aggregateRoot)) roots.Add(type);
        if (scanOperations && TryGetOperation(type, operationAttribute, out OperationShape? operation)) operations.Add(operation!);
      }
    }

    if (roots.Count == 0 || operations.Count == 0) return;

    foreach (IGrouping<string, OperationShape> group in operations.GroupBy(static o => o.Group, StringComparer.Ordinal).OrderBy(static g => g.Key, StringComparer.Ordinal))
    {
      HashSet<string> namespaces = [.. group.Select(static o => o.Request.ContainingNamespace.ToDisplayString())];
      List<INamedTypeSymbol> linked = [.. roots.Where(root => namespaces.Contains(SliceNamespace(root)))];
      if (linked.Count == 0) continue;

      List<OperationShape> groupOperations = [.. group.OrderBy(static o => o.Name, StringComparer.Ordinal)];
      if (AnotherReferenceJoins(compilation, linked, groupOperations)) continue;

      if (linked.Count > 1)
      {
        string rootNames = string.Join(", ", linked.Select(static root => root.ToDisplayString()).OrderBy(static n => n, StringComparer.Ordinal));
        context.ReportDiagnostic(Diagnostic.Create(AmbiguousRoot, Location.None, group.Key, rootNames));
        continue;
      }

      CheckGroup(context, linked[0], group.Key, groupOperations);
    }
  }

  private static void CheckGroup(CompilationAnalysisContext context, INamedTypeSymbol root, string group, List<OperationShape> operations)
  {
    Dictionary<string, IPropertySymbol> rootProperties = ContractAgreementTypes.PublicProperties(root);
    HashSet<string> checkedInterfaces = new(StringComparer.Ordinal);

    foreach (OperationShape operation in operations)
    {
      if (operation.NotAggregate) continue;

      if (operation.IsQuery)
      {
        CheckProjection(context, root, group, operation);
        continue;
      }

      bool covered = false;
      foreach (INamedTypeSymbol details in operation.Request.AllInterfaces.Where(IsDetailsInterface))
      {
        covered = true;
        if (checkedInterfaces.Add(details.ToDisplayString()))
        {
          CheckDetails(context, root, rootProperties, group, details);
        }
      }

      if (TryGetSetList(operation, out IPropertySymbol? list, out ITypeSymbol? element))
      {
        covered = true;
        CheckSetMutation(context, root, group, operation, list!, element!);
      }

      if (!covered)
      {
        context.ReportDiagnostic(Diagnostic.Create(UncoveredCommand, LocationOf(operation.Request, context.Compilation),
          operation.Request.ToDisplayString(), operation.Name, group, root.Name));
      }
    }
  }

  private static void CheckDetails(
    CompilationAnalysisContext context,
    INamedTypeSymbol root,
    Dictionary<string, IPropertySymbol> rootProperties,
    string group,
    INamedTypeSymbol details)
  {
    foreach (IPropertySymbol member in ContractAgreementTypes.InterfaceProperties(details))
    {
      string? problem = ContractAgreementTypes.Disagreement(root, rootProperties, member);
      if (problem is null) continue;

      context.ReportDiagnostic(Diagnostic.Create(DetailsMember, LocationOf(root, context.Compilation),
        root.Name, details.Name, member.Name, group, problem));
    }
  }

  private static void CheckSetMutation(
    CompilationAnalysisContext context,
    INamedTypeSymbol root,
    string group,
    OperationShape operation,
    IPropertySymbol list,
    ITypeSymbol element)
  {
    string rest = operation.ContainerName.Substring(SetPrefix.Length);
    if (rest.Length > root.Name.Length
      && rest.StartsWith(root.Name, StringComparison.Ordinal)
      && char.IsUpper(rest[root.Name.Length]))
    {
      rest = rest.Substring(root.Name.Length);
    }

    string mutationName = SetPrefix + rest;
    bool found = false;
    for (INamedTypeSymbol? current = root; current is not null && !found; current = current.BaseType)
    {
      found = current.GetMembers(mutationName)
        .OfType<IMethodSymbol>()
        .Any(method =>
          !method.IsStatic
          && method.DeclaredAccessibility == Accessibility.Public
          && method.Parameters.Length == 1
          && ContractAgreementTypes.TryGetElement(method.Parameters[0].Type, out ITypeSymbol parameterElement)
          && ContractAgreementTypes.Compatible(element, parameterElement));
    }

    if (found) return;

    context.ReportDiagnostic(Diagnostic.Create(SetMutation, LocationOf(root, context.Compilation),
      operation.Request.ToDisplayString(), list.Name, ContractAgreementTypes.Display(element), root.Name, mutationName, group));
  }

  private static void CheckProjection(
    CompilationAnalysisContext context,
    INamedTypeSymbol root,
    string group,
    OperationShape operation)
  {
    if (operation.Response is not INamedTypeSymbol response) return;

    foreach (ContractAgreementProjection.Problem problem in ContractAgreementProjection.Problems(root, response))
    {
      context.ReportDiagnostic(Diagnostic.Create(Projection, LocationOf(problem.Member, context.Compilation),
        problem.Projected.ToDisplayString(), problem.Member.Name, problem.Source.Name, group, problem.Reason));
    }
  }

  private static bool TryGetOperation(INamedTypeSymbol type, INamedTypeSymbol operationAttribute, out OperationShape? operation)
  {
    operation = null;
    AttributeData? attribute = type.GetAttributes()
      .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, operationAttribute));
    if (attribute is null) return false;

    string? name = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : null;
    string? group = attribute.NamedArguments.FirstOrDefault(static a => a.Key == "Group").Value.Value as string;
    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(group)) return false;

    INamedTypeSymbol? kind = RequestInterface(type);
    bool isQuery = kind?.Name == "IQuery";
    ITypeSymbol? response = kind?.TypeArguments[0];
    if (response is INamedTypeSymbol { Name: "OneOf", TypeArguments.Length: > 0 } oneOf)
    {
      response = oneOf.TypeArguments[0];
    }

    operation = new OperationShape(type, name!, group!, type.ContainingType?.Name ?? type.Name, isQuery, response, HasNotAggregateReason(type));
    return true;
  }

  private static INamedTypeSymbol? RequestInterface(INamedTypeSymbol type)
  {
    INamedTypeSymbol? request = null;
    foreach (INamedTypeSymbol candidate in type.AllInterfaces)
    {
      if (candidate.TypeArguments.Length != 1) continue;

      switch (candidate.Name)
      {
        case "IQuery":
        case "ICommand":
        case "IIdempotentCommand":
          return candidate;
        case "IRequest":
          request ??= candidate;
          break;
      }
    }

    return request;
  }

  private static bool TryGetSetList(OperationShape operation, out IPropertySymbol? list, out ITypeSymbol? element)
  {
    list = null;
    element = null;
    string name = operation.ContainerName;
    if (name.Length <= SetPrefix.Length || !name.StartsWith(SetPrefix, StringComparison.Ordinal) || !char.IsUpper(name[SetPrefix.Length]))
    {
      return false;
    }

    List<(IPropertySymbol Property, ITypeSymbol Element)> lists = [];
    foreach (IPropertySymbol property in ContractAgreementTypes.ContractProperties(operation.Request))
    {
      if (ContractAgreementTypes.TryGetElement(property.Type, out ITypeSymbol propertyElement))
      {
        lists.Add((property, propertyElement));
      }
    }

    if (lists.Count != 1) return false;

    (list, element) = lists[0];
    return true;
  }

  private static bool IsDetailsInterface(INamedTypeSymbol type) =>
    type.TypeKind == TypeKind.Interface
    && type.Name.Length > "IDetails".Length
    && type.Name[0] == 'I'
    && type.Name.EndsWith("Details", StringComparison.Ordinal);

  private static bool IsAggregateRoot(INamedTypeSymbol type, INamedTypeSymbol aggregateRoot) =>
    type.TypeKind == TypeKind.Class
    && !type.IsAbstract
    && type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, aggregateRoot));

  private static string SliceNamespace(INamedTypeSymbol root)
  {
    string name = root.ContainingNamespace.ToDisplayString();
    return name.EndsWith(DomainNamespaceSuffix, StringComparison.Ordinal)
      ? name.Substring(0, name.Length - DomainNamespaceSuffix.Length)
      : name;
  }

  private static bool HasNotAggregateReason(INamedTypeSymbol type) =>
    type.GetAttributes().Any(static a =>
      a.AttributeClass?.Name == NotAggregateAttributeName
      && a.ConstructorArguments.Length == 1
      && a.ConstructorArguments[0].Value is string reason
      && !string.IsNullOrWhiteSpace(reason));

  private static bool AnotherReferenceJoins(Compilation compilation, List<INamedTypeSymbol> roots, List<OperationShape> operations)
  {
    List<IAssemblySymbol> sides =
    [
      .. roots.Select(static r => r.ContainingAssembly),
      .. operations.Select(static o => o.Request.ContainingAssembly),
    ];

    return compilation.SourceModule.ReferencedAssemblySymbols
      .Any(reference => sides.All(side => Sees(reference, side)));
  }

  private static bool Sees(IAssemblySymbol assembly, IAssemblySymbol target) =>
    assembly.Identity.Name == target.Identity.Name
    || assembly.Modules.Any(module => module.ReferencedAssemblySymbols.Any(r => r.Identity.Name == target.Identity.Name));

  private static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol namespaceSymbol)
  {
    foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
    {
      foreach (INamedTypeSymbol nested in WithNested(type)) yield return nested;
    }

    foreach (INamespaceSymbol child in namespaceSymbol.GetNamespaceMembers())
    {
      foreach (INamedTypeSymbol type in AllTypes(child)) yield return type;
    }
  }

  private static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol type)
  {
    yield return type;
    foreach (INamedTypeSymbol nested in type.GetTypeMembers())
    {
      foreach (INamedTypeSymbol inner in WithNested(nested)) yield return inner;
    }
  }

  private static Location LocationOf(ISymbol symbol, Compilation compilation) =>
    symbol.Locations.FirstOrDefault(location =>
      location.IsInSource && location.SourceTree is not null && compilation.ContainsSyntaxTree(location.SourceTree))
    ?? Location.None;
}
