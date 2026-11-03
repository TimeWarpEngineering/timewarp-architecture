#region Purpose
// Collector for TypedIdSourceGenerator: finds [TypedId] declarations in syntax, and builds the
// EF-pass model from them plus [TypedId] types in marked referenced assemblies.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>Builds TypedIdCandidate and TypedIdEfModel values for TypedIdSourceGenerator.</summary>
internal static class TypedIdCollector
{
  private const string AttributeSimpleName = "TypedId";
  private const string MarkerAttributeSimpleName = "TypedIdsEmbeddedAttribute";
  private const string ValueConverterMetadataName =
    "Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter`2";

  internal static bool IsCandidate(SyntaxNode node) => node switch
  {
    StructDeclarationSyntax { AttributeLists.Count: > 0 } => true,
    RecordDeclarationSyntax { AttributeLists.Count: > 0 } record =>
      record.IsKind(SyntaxKind.RecordStructDeclaration),
    _ => false,
  };

  internal static TypedIdCandidate? GetCandidate(GeneratorSyntaxContext context, CancellationToken cancellationToken)
  {
    var declaration = (TypeDeclarationSyntax)context.Node;

    if (!HasTypedIdAttribute(declaration.AttributeLists))
      return null;

    if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol symbol)
      return null;

    bool isPartial = symbol.DeclaringSyntaxReferences
      .Select(reference => reference.GetSyntax(cancellationToken))
      .OfType<TypeDeclarationSyntax>()
      .Any(static d => d.Modifiers.Any(SyntaxKind.PartialKeyword));

    if (IsValidTypedIdShape(symbol) && isPartial)
      return new TypedIdCandidate(ToModel(symbol), null, null);

    return new TypedIdCandidate(null, symbol.Name, declaration.Identifier.GetLocation());
  }

  private static bool IsValidTypedIdShape(INamedTypeSymbol symbol) =>
    symbol is { TypeKind: TypeKind.Struct, IsRecord: true, IsReadOnly: true, IsGenericType: false };

  private static TypedIdModel ToModel(INamedTypeSymbol symbol)
  {
    string fullyQualified = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string hint = fullyQualified.Replace("global::", string.Empty, StringComparison.Ordinal)
      .Replace('.', '_');
    string ns = symbol.ContainingNamespace.IsGlobalNamespace
      ? string.Empty
      : symbol.ContainingNamespace.ToDisplayString();
    return new TypedIdModel(symbol.Name, fullyQualified, ns, hint);
  }

  private static bool HasTypedIdAttribute(SyntaxList<AttributeListSyntax> attributeLists) =>
    attributeLists
      .SelectMany(static list => list.Attributes)
      .Any(static attribute => IsTypedIdAttributeName(attribute.Name.ToString()));

  private static bool IsTypedIdAttributeName(string name)
  {
    int dot = name.LastIndexOf('.', StringComparison.Ordinal);
    if (dot >= 0)
      name = name.Substring(dot + 1);

    return name is AttributeSimpleName or AttributeSimpleName + "Attribute";
  }

  private static bool HasTypedIdAttribute(INamedTypeSymbol type) =>
    type.GetAttributes().Any(static attr =>
    {
      string? name = attr.AttributeClass?.Name;
      return name is AttributeSimpleName or AttributeSimpleName + "Attribute";
    });

  internal static TypedIdEfModel BuildEfModel(
    Compilation compilation,
    ImmutableArray<TypedIdCandidate> candidates,
    CancellationToken cancellationToken)
  {
    if (compilation.GetTypeByMetadataName(ValueConverterMetadataName) is null)
      return new TypedIdEfModel(hasEf: false, ImmutableArray<TypedIdModel>.Empty);

    var typedIds = new List<TypedIdModel>();

    foreach (TypedIdCandidate candidate in candidates)
    {
      if (candidate.Model is TypedIdModel model)
        typedIds.Add(model);
    }

    // MSBuild passes the transitive reference closure to the compiler, so this covers transitive
    // references too. The marker filter keeps this from walking framework assemblies' namespaces.
    foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!HasEmbeddedMarker(assembly))
        continue;

      CollectTypedIds(assembly.GlobalNamespace, typedIds);
    }

    typedIds.Sort(static (a, b) => string.CompareOrdinal(a.FullyQualifiedName, b.FullyQualifiedName));

    var seen = new HashSet<string>(StringComparer.Ordinal);
    ImmutableArray<TypedIdModel>.Builder unique = ImmutableArray.CreateBuilder<TypedIdModel>(typedIds.Count);
    foreach (TypedIdModel model in typedIds)
    {
      if (seen.Add(model.FullyQualifiedName))
        unique.Add(model);
    }

    return new TypedIdEfModel(hasEf: true, unique.ToImmutable());
  }

  private static bool HasEmbeddedMarker(IAssemblySymbol assembly) =>
    assembly.GetAttributes().Any(static attr => attr.AttributeClass?.Name == MarkerAttributeSimpleName);

  private static void CollectTypedIds(INamespaceSymbol root, List<TypedIdModel> results)
  {
    foreach (INamedTypeSymbol type in GetAllTypes(root))
    {
      if (!IsValidMetadataTypedId(type) || !HasTypedIdAttribute(type))
        continue;

      results.Add(ToModel(type));
    }
  }

  // IsRecord does not round-trip through metadata for record structs (they compile to plain structs),
  // so referenced-assembly ids are validated by the generated member shape instead. A [TypedId] type in
  // a successfully-built referenced assembly has that shape: TWE006 breaks invalid shapes at its own
  // compile time, and a type that never ran the generator lacks Value/From and fails here.
  private static bool IsValidMetadataTypedId(INamedTypeSymbol type) =>
    type is { TypeKind: TypeKind.Struct, IsGenericType: false }
    && type.GetMembers("Value").OfType<IPropertySymbol>().Any(static p => IsSystemGuid(p.Type))
    && type.GetMembers("From").OfType<IMethodSymbol>().Any(static m =>
      m.IsStatic && m.Parameters.Length == 1 && IsSystemGuid(m.Parameters[0].Type));

  private static bool IsSystemGuid(ITypeSymbol type) =>
    type is INamedTypeSymbol
    {
      Name: "Guid",
      ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true },
    };

  private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol root)
  {
    foreach (INamespaceOrTypeSymbol member in root.GetMembers())
    {
      switch (member)
      {
        case INamespaceSymbol ns:
          foreach (INamedTypeSymbol nested in GetAllTypes(ns))
            yield return nested;
          break;
        case INamedTypeSymbol type:
          yield return type;
          foreach (INamedTypeSymbol nested in GetNestedTypes(type))
            yield return nested;
          break;
        default:
          break;
      }
    }
  }

  private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
  {
    foreach (INamedTypeSymbol nested in type.GetTypeMembers())
    {
      yield return nested;
      foreach (INamedTypeSymbol deeper in GetNestedTypes(nested))
        yield return deeper;
    }
  }

}
