#region Purpose
// One classification of I*Details, From, and Clone for the convention analyzers and the details-copy generator.
#endregion

#region Design
// Linked into the convention-analyzers assembly and foundation-contracts-generators. No
// ProjectReference joins those packages, same as HostedRouteDiscovery. The predicates used to
// be copied in DetailsCopyAnalyzer, DetailsCopySymbols, and ContractAgreementAnalyzer, and the
// copies disagreed: GetMembers("Clone") misses an explicit ICloneable.Clone, so TWA0047 stayed
// quiet and the generator emitted a public Clone that interface dispatch never calls.
// CloneMethods therefore takes a parameterless instance Clone by name, the method
// FindImplementationForInterfaceMember returns for System.ICloneable.Clone when that method is
// declared on the type, and any declared method whose ExplicitInterfaceImplementations include
// that interface method. A clone inherited from a base type is not this type's clone.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class DetailsCopyClassification
{
  internal static bool IsDetailsInterface(INamedTypeSymbol type) =>
    type.TypeKind == TypeKind.Interface
    && type.Name.Length > "IDetails".Length
    && type.Name[0] == 'I'
    && type.Name.EndsWith("Details", StringComparison.Ordinal);

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

  internal static List<IPropertySymbol> InterfaceProperties(INamedTypeSymbol details)
  {
    List<IPropertySymbol> properties = [];
    HashSet<INamedTypeSymbol> seen = new(SymbolEqualityComparer.Default);
    CollectProperties(details, properties, seen);
    return properties;
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

  internal static bool HasClone(INamedTypeSymbol type) => CloneMethods(type).Any();

  internal static IEnumerable<IMethodSymbol> CloneMethods(INamedTypeSymbol type)
  {
    HashSet<ISymbol> seen = new(SymbolEqualityComparer.Default);
    foreach (ISymbol member in type.GetMembers("Clone"))
    {
      if (member is IMethodSymbol method && IsOwnClone(method, type) && seen.Add(method))
      {
        yield return method;
      }
    }

    IMethodSymbol? interfaceClone = CloneableClone(type);
    if (interfaceClone is null)
    {
      yield break;
    }

    if (type.FindImplementationForInterfaceMember(interfaceClone) is IMethodSymbol implementation
      && IsOwnClone(implementation, type)
      && seen.Add(implementation))
    {
      yield return implementation;
    }

    foreach (ISymbol member in type.GetMembers())
    {
      if (member is not IMethodSymbol method || !IsOwnClone(method, type))
      {
        continue;
      }

      foreach (IMethodSymbol implemented in method.ExplicitInterfaceImplementations)
      {
        if (SymbolEqualityComparer.Default.Equals(implemented, interfaceClone) && seen.Add(method))
        {
          yield return method;
          break;
        }
      }
    }
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
        if (reference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax declaration
          || !declaration.Modifiers.Any(static token => token.IsKind(SyntaxKind.PartialKeyword)))
        {
          return false;
        }
      }
    }

    return true;
  }

  private static void CollectProperties(
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

  private static IMethodSymbol? CloneableClone(INamedTypeSymbol type)
  {
    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (iface.Name != "ICloneable" || iface.ContainingNamespace.ToDisplayString() != "System")
      {
        continue;
      }

      foreach (ISymbol member in iface.GetMembers("Clone"))
      {
        if (member is IMethodSymbol { IsStatic: false } method && method.Parameters.Length == 0)
        {
          return method;
        }
      }
    }

    return null;
  }

  private static bool IsOwnClone(IMethodSymbol method, INamedTypeSymbol type) =>
    !method.IsStatic
    && method.Parameters.Length == 0
    && SymbolEqualityComparer.Default.Equals(method.ContainingType, type);
}
