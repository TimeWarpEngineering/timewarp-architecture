#region Purpose
// Reads a Command or Response symbol into a details-copy plan.
#endregion

#region Design
// I*Details matches ContractAgreementAnalyzer.IsDetailsInterface: an interface whose name is longer
// than "IDetails", starts with I, and ends with Details. Commands get one From per such interface.
// Responses get Clone only when one of those interfaces has a non-init setter. Unknown collection
// types are left as scalars (shared). Explicit init-only implementations are skipped because an
// object initializer cannot name them and a later assignment cannot call init.
#endregion

namespace TimeWarp.Foundation.Contracts.Generators;

using Microsoft.CodeAnalysis;

internal static class DetailsCopyReader
{
  internal static DetailsPlan? TryPlan(INamedTypeSymbol type, CancellationToken cancellationToken)
  {
    if (type.TypeKind != TypeKind.Class || type.IsStatic || type.IsAbstract || type.IsGenericType)
    {
      return null;
    }

    if (type.Name is not ("Command" or "Response"))
    {
      return null;
    }

    if (DetailsCopySymbols.HasGenericContainer(type) || !DetailsCopySymbols.IsPartialChain(type, cancellationToken))
    {
      return null;
    }

    List<INamedTypeSymbol> details = DetailsCopySymbols.DetailsInterfaces(type);
    if (details.Count == 0)
    {
      return null;
    }

    DetailsPlan plan = new()
    {
      Namespace = type.ContainingNamespace is { IsGlobalNamespace: false } containing
        ? containing.ToDisplayString()
        : "",
      ClassName = type.Name
    };

    for (INamedTypeSymbol? container = type.ContainingType; container is not null; container = container.ContainingType)
    {
      plan.Containers.Add(container.Name);
    }

    plan.Containers.Reverse();

    if (type.Name == "Command" && DetailsCopySymbols.HasParameterlessConstructor(type))
    {
      foreach (INamedTypeSymbol iface in details)
      {
        if (DetailsCopySymbols.HasFrom(type, iface))
        {
          continue;
        }

        DetailsFromPart part = new() { InterfaceDisplay = DetailsCopySymbols.Display(iface) };
        HashSet<string> names = [];
        foreach (IPropertySymbol property in DetailsCopySymbols.InterfaceProperties(iface))
        {
          if (!names.Add(property.Name))
          {
            continue;
          }

          DetailsMember? member = CreateFromMember(property, iface, type);
          if (member is not null)
          {
            part.Members.Add(member);
          }
        }

        plan.Froms.Add(part);
      }
    }

    if (type.Name == "Response" && details.Exists(DetailsCopySymbols.IsMutable) && !DetailsCopySymbols.HasClone(type))
    {
      plan.EmitClone = true;
      plan.AddCloneable = !DetailsCopySymbols.ImplementsCloneable(type);
      AddCloneMembers(type, plan.CloneMembers);
    }

    if (plan.Froms.Count == 0 && !plan.EmitClone)
    {
      return null;
    }

    return plan;
  }

  private static DetailsMember? CreateFromMember(
    IPropertySymbol interfaceProperty,
    INamedTypeSymbol iface,
    INamedTypeSymbol type)
  {
    if (interfaceProperty.GetMethod is null || interfaceProperty.Type.TypeKind == TypeKind.Error)
    {
      return null;
    }

    if (type.FindImplementationForInterfaceMember(interfaceProperty) is not IPropertySymbol implementation)
    {
      return null;
    }

    if (!DetailsCopySymbols.DeclaredOn(type, implementation.ContainingType) || implementation.SetMethod is null)
    {
      return null;
    }

    bool explicitOnly = implementation.ExplicitInterfaceImplementations.Length > 0;
    if (explicitOnly && implementation.SetMethod.IsInitOnly)
    {
      return null;
    }

    DetailsMember member = DetailsCopySymbols.Describe(interfaceProperty.Type);
    member.ReadName = interfaceProperty.Name;
    member.WriteName = explicitOnly ? interfaceProperty.Name : implementation.Name;
    member.WriteKind = explicitOnly ? DetailsWriteKind.Interface : DetailsWriteKind.Initializer;
    member.InterfaceDisplay = DetailsCopySymbols.Display(iface);
    return member;
  }

  private static void AddCloneMembers(INamedTypeSymbol type, List<DetailsMember> members)
  {
    HashSet<ISymbol> covered = new(SymbolEqualityComparer.Default);
    for (INamedTypeSymbol? current = type;
      current is not null && current.SpecialType != SpecialType.System_Object;
      current = current.BaseType)
    {
      foreach (ISymbol symbol in current.GetMembers())
      {
        if (symbol.IsStatic || symbol.IsImplicitlyDeclared)
        {
          continue;
        }

        if (symbol is IPropertySymbol property)
        {
          if (!covered.Add(property.OriginalDefinition))
          {
            continue;
          }

          if (property.OverriddenProperty is not null)
          {
            covered.Add(property.OverriddenProperty.OriginalDefinition);
          }

          DetailsMember? member = CreateCloneProperty(property, type);
          if (member is not null)
          {
            members.Add(member);
          }
        }
        else if (symbol is IFieldSymbol { IsConst: false } field)
        {
          DetailsMember? member = CreateCloneField(field, type);
          if (member is not null)
          {
            members.Add(member);
          }
        }
      }
    }
  }

  private static DetailsMember? CreateCloneProperty(IPropertySymbol property, INamedTypeSymbol type)
  {
    if (property.IsIndexer || property.Type.TypeKind == TypeKind.Error || !DetailsCopySymbols.NeedsDeepCopy(property.Type))
    {
      return null;
    }

    IFieldSymbol? backing = DetailsCopySymbols.BackingField(property);
    bool explicitOnly = property.ExplicitInterfaceImplementations.Length > 0;
    bool readable = DetailsCopySymbols.CanUse(property.GetMethod, property.ContainingType, type);
    bool assignable = DetailsCopySymbols.CanUse(property.SetMethod, property.ContainingType, type)
      && property.SetMethod is { IsInitOnly: false };

    DetailsMember member = DetailsCopySymbols.Describe(property.Type);
    member.ReadName = property.Name;
    member.WriteName = property.Name;
    if (explicitOnly && readable)
    {
      member.InterfaceDisplay = DetailsCopySymbols.Display(property.ExplicitInterfaceImplementations[0].ContainingType);
    }

    if (explicitOnly && assignable)
    {
      member.WriteKind = DetailsWriteKind.Interface;
    }
    else if (!explicitOnly && assignable)
    {
      member.WriteKind = DetailsWriteKind.Property;
    }
    else if (backing is not null)
    {
      member.WriteKind = DetailsWriteKind.UnsafeField;
      member.FieldOwnerDisplay = DetailsCopySymbols.Display(backing.ContainingType);
      member.FieldName = backing.Name;
    }
    else
    {
      return null;
    }

    if (!readable)
    {
      if (backing is null)
      {
        return null;
      }

      member.ReadViaField = true;
      member.FieldOwnerDisplay = DetailsCopySymbols.Display(backing.ContainingType);
      member.FieldName = backing.Name;
    }

    return member;
  }

  private static DetailsMember? CreateCloneField(IFieldSymbol field, INamedTypeSymbol type)
  {
    if (field.Type.TypeKind == TypeKind.Error || !DetailsCopySymbols.NeedsDeepCopy(field.Type))
    {
      return null;
    }

    bool direct = SymbolEqualityComparer.Default.Equals(field.ContainingType, type) && !field.IsReadOnly;
    DetailsMember member = DetailsCopySymbols.Describe(field.Type);
    member.ReadName = field.Name;
    member.WriteName = field.Name;
    if (direct)
    {
      member.WriteKind = DetailsWriteKind.Property;
      return member;
    }

    member.WriteKind = DetailsWriteKind.UnsafeField;
    member.FieldOwnerDisplay = DetailsCopySymbols.Display(field.ContainingType);
    member.FieldName = field.Name;
    if (!SymbolEqualityComparer.Default.Equals(field.ContainingType, type))
    {
      member.ReadViaField = true;
    }

    return member;
  }
}
