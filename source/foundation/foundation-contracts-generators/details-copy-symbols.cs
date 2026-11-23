#region Purpose
// Classifies details-interface symbols for the details-copy generator.
#endregion

#region Design
// I*Details matches ContractAgreementAnalyzer.IsDetailsInterface. Collection kinds are the ones
// From and Clone can construct. Other collection types stay scalars and are shared.
#endregion

namespace TimeWarp.Foundation.Contracts.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class DetailsCopySymbols
{
  private static readonly SymbolDisplayFormat Format = new(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

  internal static DetailsMember Describe(ITypeSymbol type)
  {
    DetailsMember member = new()
    {
      TypeDisplay = Display(type),
      Nullable = IsNullable(type)
    };

    if (TryClassifyCollection(type, out DetailsCollectionKind kind, out ITypeSymbol? element, out ITypeSymbol? key, out ITypeSymbol? value))
    {
      member.Collection = kind;
      if (element is not null)
      {
        member.ElementDisplay = Display(element);
        member.ElementNullable = IsNullable(element);
        member.ElementCloneable = IsCloneableReference(element);
      }

      if (key is not null && value is not null)
      {
        member.KeyDisplay = Display(key);
        member.ValueDisplay = Display(value);
        member.KeyNullable = IsNullable(key);
        member.ValueNullable = IsNullable(value);
        member.KeyCloneable = IsCloneableReference(key);
        member.ValueCloneable = IsCloneableReference(value);
      }

      return member;
    }

    member.CloneableReference = IsCloneableReference(type);
    return member;
  }

  internal static bool NeedsDeepCopy(ITypeSymbol type) =>
    TryClassifyCollection(type, out _, out _, out _, out _) || IsCloneableReference(type);

  internal static bool TryClassifyCollection(
    ITypeSymbol type,
    out DetailsCollectionKind kind,
    out ITypeSymbol? element,
    out ITypeSymbol? key,
    out ITypeSymbol? value)
  {
    kind = DetailsCollectionKind.None;
    element = null;
    key = null;
    value = null;

    if (type is IArrayTypeSymbol { Rank: 1 } array)
    {
      kind = DetailsCollectionKind.Array;
      element = array.ElementType;
      return true;
    }

    if (type is not INamedTypeSymbol named || named.SpecialType == SpecialType.System_String)
    {
      return false;
    }

    INamedTypeSymbol definition = named.OriginalDefinition;
    if (IsType(definition, "System.Collections.Generic", "List", 1))
    {
      kind = DetailsCollectionKind.List;
      element = named.TypeArguments[0];
      return true;
    }

    if (IsType(definition, "System.Collections.Generic", "HashSet", 1))
    {
      kind = DetailsCollectionKind.HashSet;
      element = named.TypeArguments[0];
      return true;
    }

    if (IsType(definition, "System.Collections.Generic", "Dictionary", 2))
    {
      kind = DetailsCollectionKind.Dictionary;
      key = named.TypeArguments[0];
      value = named.TypeArguments[1];
      return true;
    }

    if (named.TypeKind != TypeKind.Interface)
    {
      return false;
    }

    if (IsType(definition, "System.Collections.Generic", "IList", 1)
      || IsType(definition, "System.Collections.Generic", "IReadOnlyList", 1)
      || IsType(definition, "System.Collections.Generic", "ICollection", 1)
      || IsType(definition, "System.Collections.Generic", "IReadOnlyCollection", 1)
      || IsType(definition, "System.Collections.Generic", "IEnumerable", 1))
    {
      kind = DetailsCollectionKind.List;
      element = named.TypeArguments[0];
      return true;
    }

    if (IsType(definition, "System.Collections.Generic", "ISet", 1)
      || IsType(definition, "System.Collections.Generic", "IReadOnlySet", 1))
    {
      kind = DetailsCollectionKind.HashSet;
      element = named.TypeArguments[0];
      return true;
    }

    if (IsType(definition, "System.Collections.Generic", "IDictionary", 2)
      || IsType(definition, "System.Collections.Generic", "IReadOnlyDictionary", 2))
    {
      kind = DetailsCollectionKind.Dictionary;
      key = named.TypeArguments[0];
      value = named.TypeArguments[1];
      return true;
    }

    return false;
  }

  internal static bool IsType(INamedTypeSymbol definition, string namespaceName, string name, int arity) =>
    definition.Name == name
    && definition.Arity == arity
    && definition.ContainingNamespace.ToDisplayString() == namespaceName;

  internal static bool IsCloneableReference(ITypeSymbol type)
  {
    ITypeSymbol current = type;
    if (current is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
      && nullable.TypeArguments.Length == 1)
    {
      current = nullable.TypeArguments[0];
    }

    if (current.IsValueType || current.SpecialType == SpecialType.System_String)
    {
      return false;
    }

    return ImplementsCloneable(current);
  }

  internal static bool ImplementsCloneable(ITypeSymbol type)
  {
    if (type.Name == "ICloneable" && type.ContainingNamespace.ToDisplayString() == "System")
    {
      return true;
    }

    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (iface.Name == "ICloneable" && iface.ContainingNamespace.ToDisplayString() == "System")
      {
        return true;
      }
    }

    return false;
  }

  internal static bool IsNullable(ITypeSymbol type) =>
    type.NullableAnnotation == NullableAnnotation.Annotated
    || type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };

  internal static bool IsMutable(INamedTypeSymbol details)
  {
    foreach (IPropertySymbol property in InterfaceProperties(details))
    {
      if (property.SetMethod is { IsInitOnly: false })
      {
        return true;
      }
    }

    return false;
  }

  internal static List<INamedTypeSymbol> DetailsInterfaces(INamedTypeSymbol type)
  {
    List<INamedTypeSymbol> interfaces = [];
    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (IsDetailsInterface(iface))
      {
        interfaces.Add(iface);
      }
    }

    return interfaces;
  }

  internal static bool IsDetailsInterface(INamedTypeSymbol type) =>
    type.TypeKind == TypeKind.Interface
    && type.Name.Length > "IDetails".Length
    && type.Name[0] == 'I'
    && type.Name.EndsWith("Details", StringComparison.Ordinal);

  internal static List<IPropertySymbol> InterfaceProperties(INamedTypeSymbol details)
  {
    List<IPropertySymbol> properties = [];
    HashSet<INamedTypeSymbol> seen = new(SymbolEqualityComparer.Default);
    CollectProperties(details, properties, seen);
    return properties;
  }

  internal static void CollectProperties(
    INamedTypeSymbol details,
    List<IPropertySymbol> properties,
    HashSet<INamedTypeSymbol> seen)
  {
    if (!seen.Add(details))
    {
      return;
    }

    foreach (ISymbol member in details.GetMembers())
    {
      if (member is IPropertySymbol { IsStatic: false, IsIndexer: false } property)
      {
        properties.Add(property);
      }
    }

    foreach (INamedTypeSymbol parent in details.Interfaces)
    {
      CollectProperties(parent, properties, seen);
    }
  }

  internal static bool HasFrom(INamedTypeSymbol type, INamedTypeSymbol details)
  {
    foreach (ISymbol member in type.GetMembers("From"))
    {
      if (member is IMethodSymbol { IsStatic: true } method
        && method.Parameters.Length == 1
        && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, details))
      {
        return true;
      }
    }

    return false;
  }

  internal static bool HasClone(INamedTypeSymbol type)
  {
    foreach (ISymbol member in type.GetMembers("Clone"))
    {
      if (member is IMethodSymbol { IsStatic: false } method && method.Parameters.Length == 0)
      {
        return true;
      }
    }

    return false;
  }

  internal static bool HasParameterlessConstructor(INamedTypeSymbol type)
  {
    foreach (IMethodSymbol constructor in type.InstanceConstructors)
    {
      if (constructor.Parameters.Length == 0)
      {
        return true;
      }
    }

    return false;
  }

  internal static bool HasGenericContainer(INamedTypeSymbol type)
  {
    for (INamedTypeSymbol? current = type.ContainingType; current is not null; current = current.ContainingType)
    {
      if (current.IsGenericType)
      {
        return true;
      }
    }

    return false;
  }

  internal static bool IsPartialChain(INamedTypeSymbol type, CancellationToken cancellationToken)
  {
    for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
    {
      if (current.DeclaringSyntaxReferences.Length == 0)
      {
        return false;
      }

      foreach (SyntaxReference reference in current.DeclaringSyntaxReferences)
      {
        if (reference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax declaration)
        {
          return false;
        }

        if (!declaration.Modifiers.Any(static token => token.IsKind(SyntaxKind.PartialKeyword)))
        {
          return false;
        }
      }
    }

    return true;
  }

  internal static bool DeclaredOn(INamedTypeSymbol type, INamedTypeSymbol containing)
  {
    for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
    {
      if (SymbolEqualityComparer.Default.Equals(current, containing))
      {
        return true;
      }
    }

    return false;
  }

  internal static bool CanUse(IMethodSymbol? method, INamedTypeSymbol declaring, INamedTypeSymbol current)
  {
    if (method is null)
    {
      return false;
    }

    if (SymbolEqualityComparer.Default.Equals(declaring, current))
    {
      return true;
    }

    return method.DeclaredAccessibility switch
    {
      Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal => true,
      Accessibility.Internal or Accessibility.ProtectedAndInternal =>
        SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, current.ContainingAssembly),
      _ => false
    };
  }

  internal static IFieldSymbol? BackingField(IPropertySymbol property)
  {
    foreach (ISymbol member in property.ContainingType.GetMembers())
    {
      if (member is IFieldSymbol field && SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, property))
      {
        return field;
      }
    }

    return null;
  }

  internal static string Display(ITypeSymbol type) => type.ToDisplayString(Format);
}
