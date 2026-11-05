#region Purpose
// Walks a query response for the contract-agreement analyzer (TWA0041) and returns each member that
// does not project from the aggregate root, including members of child classes (activity entries)
// and of a projection the response wraps in one member (an admin view, an item list).
#endregion

#region Design
// Task 475 (FeedbackItem, the second shape after Roles and Profile) added two walks to the 474
// member check; names still match exactly and every leaf still uses ContractAgreementTypes.
//   Child projection: a response member and the root member of the same name whose types are both
//     classes (or both collections of classes) that are not otherwise compatible are walked member
//     by member: FeedbackActivityEntry projects from FeedbackActivity. The child's problems name the
//     child types, so the message points at the entry, not at the list. Each (contract, root) type
//     pair is walked once, which also stops a cycle.
//   Wrapper member: on a response that is not a ListResponse<T>, a member with no root member of
//     its name whose type is a class declared in the response's own assembly (or a collection of
//     one) is the projection itself, wrapped: GetFeedbackForAdmin.Response.Item (FeedbackAdminView)
//     and ListMyFeedback.Response.Items. Its type is walked against the root, one level only, like a
//     ListResponse<T> is checked through T. This is not a name mapping: a member that drifted
//     still fails, because the wrapped class's members are then checked against the root.
// [Computed] on the response, a list item or wrapped class, a child class, or a member skips it.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Query-response projection walk for ContractAgreementAnalyzer (TWA0041).</summary>
internal static class ContractAgreementProjection
{
  private const string ComputedAttributeName = "ComputedAttribute";
  private const string ListResponseName = "ListResponse";

  /// <summary>One response member that does not project: the class that declares it, the member, the type it should project from, and why.</summary>
  public sealed record Problem(INamedTypeSymbol Projected, IPropertySymbol Member, INamedTypeSymbol Source, string Reason);

  /// <summary>Every member of <paramref name="response"/> (see Design) that does not project from <paramref name="root"/>.</summary>
  public static List<Problem> Problems(INamedTypeSymbol root, INamedTypeSymbol response)
  {
    List<Problem> problems = [];
    if (IsComputed(response)) return problems;

    HashSet<string> walked = new(StringComparer.Ordinal);
    INamedTypeSymbol? listItem = ListItemType(response);
    if (listItem is not null)
    {
      if (!IsComputed(listItem)) Walk(listItem, root, wrapperAllowed: false, walked, problems);
      return problems;
    }

    Walk(response, root, wrapperAllowed: true, walked, problems);
    return problems;
  }

  private static void Walk(
    INamedTypeSymbol projected,
    INamedTypeSymbol source,
    bool wrapperAllowed,
    HashSet<string> walked,
    List<Problem> problems)
  {
    if (!walked.Add(projected.ToDisplayString() + "<-" + source.ToDisplayString())) return;

    Dictionary<string, IPropertySymbol> sourceProperties = ContractAgreementTypes.PublicProperties(source);
    foreach (IPropertySymbol member in ContractAgreementTypes.ContractProperties(projected))
    {
      if (IsComputed(member)) continue;

      IPropertySymbol? sourceMember = ContractAgreementTypes.FindRootMember(source, sourceProperties, member.Name);
      if (sourceMember is null)
      {
        if (wrapperAllowed && TryGetWrapped(member.Type, projected.ContainingAssembly, out INamedTypeSymbol wrapped))
        {
          if (!IsComputed(wrapped)) Walk(wrapped, source, wrapperAllowed: false, walked, problems);
          continue;
        }

        problems.Add(new Problem(projected, member, source, ContractAgreementTypes.MissingMember(source, member.Name)));
        continue;
      }

      if (ContractAgreementTypes.Compatible(member.Type, sourceMember.Type)) continue;

      if (TryGetChildren(member.Type, sourceMember.Type, out INamedTypeSymbol contractChild, out INamedTypeSymbol sourceChild))
      {
        if (!IsComputed(contractChild)) Walk(contractChild, sourceChild, wrapperAllowed: false, walked, problems);
        continue;
      }

      problems.Add(new Problem(projected, member, source, ContractAgreementTypes.TypeMismatch(source, sourceMember, member)));
    }
  }

  private static bool TryGetChildren(ITypeSymbol contract, ITypeSymbol source, out INamedTypeSymbol contractChild, out INamedTypeSymbol sourceChild)
  {
    if (ContractAgreementTypes.TryGetElement(contract, out ITypeSymbol contractElement)
      && ContractAgreementTypes.TryGetElement(source, out ITypeSymbol sourceElement))
    {
      contract = contractElement;
      source = sourceElement;
    }

    contractChild = (contract as INamedTypeSymbol)!;
    sourceChild = (source as INamedTypeSymbol)!;
    return ContractAgreementTypes.IsProjectableClass(contract) && ContractAgreementTypes.IsProjectableClass(source);
  }

  private static bool TryGetWrapped(ITypeSymbol type, IAssemblySymbol contractAssembly, out INamedTypeSymbol wrapped)
  {
    if (ContractAgreementTypes.TryGetElement(type, out ITypeSymbol element)) type = element;

    wrapped = (type as INamedTypeSymbol)!;
    return ContractAgreementTypes.IsProjectableClass(type)
      && SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, contractAssembly);
  }

  private static INamedTypeSymbol? ListItemType(INamedTypeSymbol response)
  {
    for (INamedTypeSymbol? current = response.BaseType; current is not null; current = current.BaseType)
    {
      if (current.Name == ListResponseName && current.TypeArguments.Length == 1)
      {
        return current.TypeArguments[0] as INamedTypeSymbol;
      }
    }

    return null;
  }

  private static bool IsComputed(ISymbol symbol) =>
    symbol.GetAttributes().Any(static a => a.AttributeClass?.Name == ComputedAttributeName);
}
