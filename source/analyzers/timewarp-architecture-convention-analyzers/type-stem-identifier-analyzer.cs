#region Purpose
// TWA0023: a named type that already names the role is the identifier (interfaces drop a leading I).
#endregion

#region Design
// Type-stem identity, not casing: HttpClient HttpClient and HttpClient httpClient both pass.
// Match: unwrap Nullable<T> / nullable annotations; skip the skip-set; stem =
// OriginalDefinition.Name (no arity: ILogger<T> → ILogger); strip leading I on interfaces when
// Length > 1, name[0] == 'I', and name[1] is uppercase; identifier must end with the stem
// (OrdinalIgnoreCase). Vendor-prefix clipping (TimeWarpTerminal → Terminal) is attribute-only.
//
// In: fields, properties (not indexers), parameters (methods, ctors, primary ctors, lambdas),
// locals, out-var / deconstruction / is designations, foreach, catch. Locals via syntax
// (VariableDeclarator / SingleVariableDesignation / ForEachStatement / CatchDeclaration) —
// RegisterSymbolAction does not support SymbolKind.Local.
//
// Out: method/type/event names, extension this, setter value, discards (`_` identifier;
// IDiscardSymbol is not ILocalSymbol; IParameterSymbol.IsDiscard), implicit/compiler-
// generated, overrides and explicit interface implementations (and their parameters), indexers,
// anonymous-type members, enum members (named values, not a role of the enum type),
// TypeKind.Error / type parameters / pointers / function pointers / dynamic. Record positional
// parameters: analyze the parameter; skip the synthesized property (IsImplicitlyDeclared or
// declaring syntax is ParameterSyntax) so we do not double-report.
//
// Catch and lambda parameters are in — noise is why the rule ships isEnabledByDefault: false.
// Razor @code is out of v1 (GeneratedCodeAnalysisFlags.None, like TWA0009/TWA0021).
// Locals/foreach have no AttributeTargets.Local; their hatch is #pragma or editorconfig.
//
// Skip set is a hard-coded ImmutableHashSet, not editorconfig-configurable in v1:
// - SpecialType primitives: Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64,
//   IntPtr, UIntPtr, Char, String, Object, Single, Double, Decimal. Also TypeKind.Array.
// - Do not skip DateTime, Guid, TimeSpan, CancellationToken, enums, ILogger<T>,
//   IHttpClientFactory (factory fails; httpClientFactory is the rule working).
// - Untyped boxes by OriginalDefinition namespace + MetadataName:
//   System.Collections.Generic.List`1, Dictionary`2, HashSet`1, Queue`1, Stack`1,
//   IEnumerable`1, ICollection`1, IList`1, IReadOnlyList`1, IReadOnlyCollection`1,
//   IDictionary`2, IReadOnlyDictionary`2; System.Linq.IQueryable`1;
//   ImmutableArray`1, ImmutableList`1, ImmutableDictionary`2, ImmutableHashSet`1;
//   ConcurrentDictionary`2, ConcurrentBag`1; Span`1, ReadOnlySpan`1, Memory`1, ReadOnlyMemory`1;
//   Task, Task`1, ValueTask, ValueTask`1; Action / Action`N and Func`N (all BCL arities);
//   ValueTuple* / Tuple*; non-generic System.Collections.IEnumerable / IList / IDictionary.
//
// Opt-out: [TypeStemIdentifier(reason)] matched by simple name TypeStemIdentifierAttribute
// (no ProjectReference to Attributes). Ctor argument 0 must be a non-empty non-whitespace
// constant string; empty / "   " still flags TWA0023 (no second id).
#endregion

namespace TimeWarp.Architecture.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeStemIdentifierAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWA0023";

  private const string OptOutAttributeName = "TypeStemIdentifierAttribute";
  private const string SetterValueParameterName = "value";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Identifier does not use the type stem",
      messageFormat: "Identifier '{0}' must end with type stem '{1}' (the type name already names the role; qualify with a prefix if there are two of this type)",
      category: "Naming",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: false,
      description: "For a named type that already names the role, the identifier is the type name (interfaces drop a leading I). Qualify with a prefix and keep the type as the head when there are two of the same type."
    );

  private static readonly ImmutableHashSet<string> UntypedBoxMetadataNames = CreateUntypedBoxMetadataNames();

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);
    context.RegisterSyntaxNodeAction(AnalyzeVariableDeclarator, SyntaxKind.VariableDeclarator);
    context.RegisterSyntaxNodeAction(AnalyzeSingleVariableDesignation, SyntaxKind.SingleVariableDesignation);
    context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
    context.RegisterSyntaxNodeAction(AnalyzeCatchDeclaration, SyntaxKind.CatchDeclaration);
  }

  private static void AnalyzeField(SymbolAnalysisContext context)
  {
    var field = (IFieldSymbol)context.Symbol;
    if (field.IsImplicitlyDeclared) return;
    if (field.ContainingType?.TypeKind == TypeKind.Enum) return;
    if (IsCompilerGeneratedName(field.Name)) return;

    Location? location = GetIdentifierLocation(field, context.CancellationToken);
    if (location is null) return;

    AnalyzeIdentifier(field.Type, field.Name, location, field.GetAttributes(), context.ReportDiagnostic);
  }

  private static void AnalyzeProperty(SymbolAnalysisContext context)
  {
    var property = (IPropertySymbol)context.Symbol;
    if (property.IsIndexer) return;
    if (property.IsImplicitlyDeclared) return;
    if (property.IsOverride) return;
    if (property.ExplicitInterfaceImplementations.Length > 0) return;
    if (property.ContainingType?.IsAnonymousType == true) return;
    if (IsRecordPositionalProperty(property, context.CancellationToken)) return;
    if (IsCompilerGeneratedName(property.Name)) return;

    Location? location = GetIdentifierLocation(property, context.CancellationToken);
    if (location is null) return;

    AnalyzeIdentifier(property.Type, property.Name, location, property.GetAttributes(), context.ReportDiagnostic);
  }

  private static void AnalyzeParameter(SymbolAnalysisContext context)
  {
    var parameter = (IParameterSymbol)context.Symbol;
    if (ShouldSkipParameter(parameter)) return;

    Location? location = GetIdentifierLocation(parameter, context.CancellationToken);
    if (location is null) return;

    AnalyzeIdentifier(parameter.Type, parameter.Name, location, parameter.GetAttributes(), context.ReportDiagnostic);
  }

  private static void AnalyzeVariableDeclarator(SyntaxNodeAnalysisContext context)
  {
    var variableDeclarator = (VariableDeclaratorSyntax)context.Node;
    if (context.SemanticModel.GetDeclaredSymbol(variableDeclarator, context.CancellationToken) is not ILocalSymbol local)
      return;

    AnalyzeLocal(local, variableDeclarator.Identifier.GetLocation(), context.ReportDiagnostic);
  }

  private static void AnalyzeSingleVariableDesignation(SyntaxNodeAnalysisContext context)
  {
    var singleVariableDesignation = (SingleVariableDesignationSyntax)context.Node;
    if (context.SemanticModel.GetDeclaredSymbol(singleVariableDesignation, context.CancellationToken) is not ILocalSymbol local)
      return;

    AnalyzeLocal(local, singleVariableDesignation.Identifier.GetLocation(), context.ReportDiagnostic);
  }

  private static void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
  {
    var forEachStatement = (ForEachStatementSyntax)context.Node;
    if (context.SemanticModel.GetDeclaredSymbol(forEachStatement, context.CancellationToken) is not ILocalSymbol local)
      return;

    AnalyzeLocal(local, forEachStatement.Identifier.GetLocation(), context.ReportDiagnostic);
  }

  private static void AnalyzeCatchDeclaration(SyntaxNodeAnalysisContext context)
  {
    var catchDeclaration = (CatchDeclarationSyntax)context.Node;
    if (catchDeclaration.Identifier.IsKind(SyntaxKind.None)) return;
    if (context.SemanticModel.GetDeclaredSymbol(catchDeclaration, context.CancellationToken) is not ILocalSymbol local)
      return;

    AnalyzeLocal(local, catchDeclaration.Identifier.GetLocation(), context.ReportDiagnostic);
  }

  private static void AnalyzeLocal(ILocalSymbol local, Location location, Action<Diagnostic> report)
  {
    if (local.IsImplicitlyDeclared) return;
    if (IsCompilerGeneratedName(local.Name)) return;

    AnalyzeIdentifier(local.Type, local.Name, location, local.GetAttributes(), report);
  }

  private static void AnalyzeIdentifier(
    ITypeSymbol type,
    string identifier,
    Location location,
    ImmutableArray<AttributeData> attributes,
    Action<Diagnostic> report)
  {
    if (string.IsNullOrEmpty(identifier)) return;
    if (string.Equals(identifier, "_", StringComparison.Ordinal)) return;
    if (HasReasonedTypeStemOptOut(attributes)) return;

    ITypeSymbol unwrapped = UnwrapNullable(type);
    if (ShouldSkipType(unwrapped)) return;

    string? stem = GetStem(unwrapped);
    if (stem is null) return;
    if (identifier.EndsWith(stem, StringComparison.OrdinalIgnoreCase)) return;

    report(Diagnostic.Create(Rule, location, identifier, stem));
  }

  private static bool ShouldSkipParameter(IParameterSymbol parameter)
  {
    if (parameter.IsThis) return true;
    if (parameter.IsDiscard) return true;
    if (parameter.IsImplicitlyDeclared) return true;
    if (IsCompilerGeneratedName(parameter.Name)) return true;

    if (parameter.ContainingSymbol is IPropertySymbol { IsIndexer: true })
      return true;

    if (parameter.ContainingSymbol is not IMethodSymbol method)
      return false;

    if (method.IsOverride) return true;
    if (method.ExplicitInterfaceImplementations.Length > 0) return true;
    if (method.AssociatedSymbol is IPropertySymbol { IsIndexer: true }) return true;
    if (method.AssociatedSymbol is IEventSymbol) return true;
    if (method.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove) return true;
    if (method.MethodKind == MethodKind.PropertySet
        && string.Equals(parameter.Name, SetterValueParameterName, StringComparison.Ordinal))
    {
      return true;
    }

    return false;
  }

  private static bool IsRecordPositionalProperty(IPropertySymbol property, CancellationToken cancellationToken)
  {
    if (property.ContainingType?.IsRecord != true) return false;

    foreach (SyntaxReference syntaxReference in property.DeclaringSyntaxReferences)
    {
      if (syntaxReference.GetSyntax(cancellationToken) is ParameterSyntax)
        return true;
    }

    return false;
  }

  private static bool HasReasonedTypeStemOptOut(ImmutableArray<AttributeData> attributes)
  {
    foreach (AttributeData attributeData in attributes)
    {
      string? name = attributeData.AttributeClass?.Name;
      if (name is null) continue;
      if (!name.Equals(OptOutAttributeName, StringComparison.Ordinal)
          && !name.EndsWith(OptOutAttributeName, StringComparison.Ordinal))
      {
        continue;
      }

      if (attributeData.ConstructorArguments.Length < 1) continue;
      if (attributeData.ConstructorArguments[0].Value is string reason
          && !string.IsNullOrWhiteSpace(reason))
      {
        return true;
      }
    }

    return false;
  }

  private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol named
        && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        && named.TypeArguments.Length == 1)
    {
      return named.TypeArguments[0];
    }

    return type;
  }

  private static bool ShouldSkipType(ITypeSymbol type)
  {
    if (type.TypeKind is TypeKind.Error or TypeKind.TypeParameter or TypeKind.Pointer
        or TypeKind.FunctionPointer or TypeKind.Dynamic or TypeKind.Array)
    {
      return true;
    }

    if (IsPrimitiveSpecialType(type.SpecialType)) return true;

    if (type is INamedTypeSymbol named)
    {
      string metadataName = MetadataNameOf(named.OriginalDefinition);
      if (UntypedBoxMetadataNames.Contains(metadataName)) return true;
    }

    return false;
  }

  private static string? GetStem(ITypeSymbol type)
  {
    string name = type.OriginalDefinition.Name;
    if (string.IsNullOrEmpty(name)) return null;

    if (type.TypeKind == TypeKind.Interface && IsInterfaceIPrefix(name))
    {
      name = name[1..];
      if (name.Length == 0) return null;
    }

    return name;
  }

  private static bool IsInterfaceIPrefix(string name) =>
    name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]);

  private static bool IsPrimitiveSpecialType(SpecialType specialType) =>
    specialType is
      SpecialType.System_Boolean or
      SpecialType.System_Byte or
      SpecialType.System_SByte or
      SpecialType.System_Int16 or
      SpecialType.System_UInt16 or
      SpecialType.System_Int32 or
      SpecialType.System_UInt32 or
      SpecialType.System_Int64 or
      SpecialType.System_UInt64 or
      SpecialType.System_IntPtr or
      SpecialType.System_UIntPtr or
      SpecialType.System_Char or
      SpecialType.System_String or
      SpecialType.System_Object or
      SpecialType.System_Single or
      SpecialType.System_Double or
      SpecialType.System_Decimal;

  private static string MetadataNameOf(INamedTypeSymbol named)
  {
    if (named.ContainingNamespace is not { IsGlobalNamespace: false } namespaceSymbol)
      return named.MetadataName;

    return namespaceSymbol.ToDisplayString() + "." + named.MetadataName;
  }

  private static Location? GetIdentifierLocation(ISymbol symbol, CancellationToken cancellationToken)
  {
    foreach (SyntaxReference syntaxReference in symbol.DeclaringSyntaxReferences)
    {
      SyntaxNode syntax = syntaxReference.GetSyntax(cancellationToken);
      SyntaxToken identifier = syntax switch
      {
        VariableDeclaratorSyntax variableDeclarator => variableDeclarator.Identifier,
        PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Identifier,
        ParameterSyntax parameter => parameter.Identifier,
        _ => default
      };

      if (identifier != default && !identifier.IsKind(SyntaxKind.None))
        return identifier.GetLocation();
    }

    return null;
  }

  private static bool IsCompilerGeneratedName(string name) =>
    name.Length > 0 && name[0] == '<';

  private static ImmutableHashSet<string> CreateUntypedBoxMetadataNames()
  {
    IEqualityComparer<string> comparer = StringComparer.Ordinal;
    ImmutableHashSet<string>.Builder builder = ImmutableHashSet.CreateBuilder(comparer);

    builder.UnionWith(
    [
      "System.Collections.Generic.List`1",
      "System.Collections.Generic.Dictionary`2",
      "System.Collections.Generic.HashSet`1",
      "System.Collections.Generic.Queue`1",
      "System.Collections.Generic.Stack`1",
      "System.Collections.Generic.IEnumerable`1",
      "System.Collections.Generic.ICollection`1",
      "System.Collections.Generic.IList`1",
      "System.Collections.Generic.IReadOnlyList`1",
      "System.Collections.Generic.IReadOnlyCollection`1",
      "System.Collections.Generic.IDictionary`2",
      "System.Collections.Generic.IReadOnlyDictionary`2",
      "System.Linq.IQueryable`1",
      "System.Collections.Immutable.ImmutableArray`1",
      "System.Collections.Immutable.ImmutableList`1",
      "System.Collections.Immutable.ImmutableDictionary`2",
      "System.Collections.Immutable.ImmutableHashSet`1",
      "System.Collections.Concurrent.ConcurrentDictionary`2",
      "System.Collections.Concurrent.ConcurrentBag`1",
      "System.Span`1",
      "System.ReadOnlySpan`1",
      "System.Memory`1",
      "System.ReadOnlyMemory`1",
      "System.Threading.Tasks.Task",
      "System.Threading.Tasks.Task`1",
      "System.Threading.Tasks.ValueTask",
      "System.Threading.Tasks.ValueTask`1",
      "System.Collections.IEnumerable",
      "System.Collections.IList",
      "System.Collections.IDictionary"
    ]);

    builder.Add("System.Action");
    for (int arity = 1; arity <= 16; arity++)
      builder.Add("System.Action`" + arity);

    for (int arity = 1; arity <= 17; arity++)
      builder.Add("System.Func`" + arity);

    builder.Add("System.ValueTuple");
    for (int arity = 1; arity <= 8; arity++)
    {
      builder.Add("System.ValueTuple`" + arity);
      builder.Add("System.Tuple`" + arity);
    }

    return builder.ToImmutable();
  }
}
