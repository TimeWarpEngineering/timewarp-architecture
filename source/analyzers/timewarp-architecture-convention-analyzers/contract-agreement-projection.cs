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
//   Wrapper member: a response that is not a ListResponse<T> wraps its projection when its only
//     non-[Computed] member has no root member of its name and its type is a class declared in the
//     response's own assembly (or a collection of one): GetFeedbackForAdmin.Response.Item
//     (FeedbackAdminView) and ListMyFeedback.Response.Items (EmailCopyAvailable is [Computed]). The
//     wrapped type is walked against the root, one level only, like a ListResponse<T> is checked
//     through T. At most one wrapper per response: a response that projects any other member
//     projects directly, so an unmatched class member there (GetFeedback.Response.Attachments
//     without [Computed]) is reported itself as missing, not member by member. This is not a name
//     mapping: a member that drifted still fails, because the wrapped class's members are then
//     checked against the root.
// Leaf types use ContractAgreementTypes.ReadCompatible: a response may widen a root T to T? (a
// value the handler redacts for one audience), which the write-side checks do not allow.
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
      if (!IsComputed(listItem)) Walk(listItem, root, walked, problems);
      return problems;
    }

    if (TryGetWrapper(response, root, out INamedTypeSymbol wrapped))
    {
      if (!IsComputed(wrapped)) Walk(wrapped, root, walked, problems);
      return problems;
    }

    Walk(response, root, walked, problems);
    return problems;
  }

  /// <summary>True when <paramref name="response"/> wraps its projection in its one non-computed member (see Design).</summary>
  private static bool TryGetWrapper(INamedTypeSymbol response, INamedTypeSymbol root, out INamedTypeSymbol wrapped)
  {
    wrapped = null!;
    List<IPropertySymbol> members = [.. ContractAgreementTypes.ContractProperties(response).Where(static member => !IsComputed(member))];
    if (members.Count != 1) return false;

    IPropertySymbol member = members[0];
    Dictionary<string, IPropertySymbol> rootProperties = ContractAgreementTypes.PublicProperties(root);
    return ContractAgreementTypes.FindRootMember(root, rootProperties, member.Name) is null
      && TryGetWrapped(member.Type, response.ContainingAssembly, out wrapped);
  }

  private static void Walk(
    INamedTypeSymbol projected,
    INamedTypeSymbol source,
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
        problems.Add(new Problem(projected, member, source, ContractAgreementTypes.MissingMember(source, member.Name)));
        continue;
      }

      if (ContractAgreementTypes.ReadCompatible(member.Type, sourceMember.Type)) continue;

      if (TryGetChildren(member.Type, sourceMember.Type, out INamedTypeSymbol contractChild, out INamedTypeSymbol sourceChild))
      {
        if (!IsComputed(contractChild)) Walk(contractChild, sourceChild, walked, problems);
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
