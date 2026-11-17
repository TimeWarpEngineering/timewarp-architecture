#region Purpose
// Type and member rules the contract-agreement analyzer (TWA0039–TWA0043) uses to decide whether
// a contract member and an aggregate-root member agree: name lookup, compatible types, collection
// element types, value-wrapper unwrapping, mirrored enums, and the disagreement messages.
#endregion

#region Design
// Names match exactly (ordinal). The one name rule is the key: a contract member named
// "<RootName>Id" (RoleId on a role response) maps to the root's "Id" when the root declares no
// member of that exact name, because every Entity<TId> exposes its key as Id while contracts name
// the key after the resource. Nothing else is renamed: drift is reported, never mapped.
// Compatible types, in order:
//   1. The same type (SymbolEqualityComparer.Default), and the same nullable annotation when both
//      sides carry one (string vs string? is drift; oblivious metadata is not judged).
//   2. The root type is a value wrapper and the contract type is compatible with what it wraps. A
//      wrapper is a non-System type with a public instance property named Value (typed ids: RoleId
//      wraps Guid) or with exactly one public instance property (a single-value child record:
//      PermissionGrant wraps its PermissionId string). System types never unwrap, so Nullable<T>
//      (Value + HasValue) does not make Guid? agree with Guid. Unwrapping stops after two levels.
//   3. Both sides are collections (IEnumerable<T>, not string) with compatible element types:
//      List<string> agrees with IReadOnlyList<PermissionGrant> and with IEnumerable<string>.
//   4. Both sides are enums with the same member names (task 475). Contracts must not reference
//      domain, so a wire enum mirrors the domain enum (FeedbackStatus in Features.Feedback and in
//      Features.Feedback.Domain); the names are what travel. Values are not compared.
//   5. Both sides are Nullable<T> of compatible T (FeedbackStatus? and the domain's FeedbackStatus?).
// Write side (TWA0039 details, TWA0040 Set elements): Guid? against Guid is still drift, because a
// contract that may send null would write null into a member the root says is never null. Only the
// two-sided form unwraps.
// Read side (TWA0041 projections, ReadCompatible): the contract may widen a root T to T? (Guid? for
// Guid, string? for string). A handler may redact a projected value for one audience:
// FeedbackActivityEntry.AuthorPrincipalId is null on the filer's copy. The member still projects
// from the root, so it is checked, not [Computed]. Narrowing (T against a root T?) is still drift.
// A class-typed member that none of these accepts may still project member by member; that walk
// belongs to ContractAgreementProjection. Messages use the short type name unless both sides
// print the same, which is the mirrored-type case; then they use the full name.
// Contract members come from metadata or source alike; the analyzer only reads symbols, never
// syntax or validator bodies (461 resolution: metadata is enough).
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Member lookup and type compatibility for ContractAgreementAnalyzer.</summary>
internal static class ContractAgreementTypes
{
  private const string KeyMemberName = "Id";
  private const string WrapperValueName = "Value";
  private const int MaxUnwrapDepth = 2;

  /// <summary>Short type text for diagnostic messages.</summary>
  public static string Display(ITypeSymbol type) =>
    type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);

  /// <summary>Public instance properties of <paramref name="type"/> and its base types, most-derived first.</summary>
  public static Dictionary<string, IPropertySymbol> PublicProperties(INamedTypeSymbol type)
  {
    Dictionary<string, IPropertySymbol> properties = new(StringComparer.Ordinal);
    for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
    {
      foreach (IPropertySymbol property in DeclaredPublicProperties(current))
      {
        properties.TryAdd(property.Name, property);
      }
    }

    return properties;
  }

  /// <summary>
  /// Public instance properties declared on <paramref name="type"/> and on base types in the same
  /// assembly. Foundation bases (BaseResponse, ListResponse) live in another assembly and are skipped.
  /// </summary>
  public static List<IPropertySymbol> ContractProperties(INamedTypeSymbol type)
  {
    List<IPropertySymbol> properties = [];
    HashSet<string> seen = new(StringComparer.Ordinal);
    for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
    {
      if (!SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, type.ContainingAssembly)) break;

      foreach (IPropertySymbol property in DeclaredPublicProperties(current))
      {
        if (seen.Add(property.Name))
        {
          properties.Add(property);
        }
      }
    }

    return properties;
  }

  /// <summary>Properties an interface declares, including the interfaces it extends.</summary>
  public static List<IPropertySymbol> InterfaceProperties(INamedTypeSymbol contractInterface)
  {
    List<IPropertySymbol> properties = [];
    HashSet<string> seen = new(StringComparer.Ordinal);
    foreach (INamedTypeSymbol current in new[] { contractInterface }.Concat(contractInterface.AllInterfaces))
    {
      foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
      {
        if (!property.IsStatic && !property.IsIndexer && seen.Add(property.Name))
        {
          properties.Add(property);
        }
      }
    }

    return properties;
  }

  /// <summary>Find the root member a contract member names: exact, or the "&lt;Root&gt;Id" key rule.</summary>
  public static IPropertySymbol? FindRootMember(
    INamedTypeSymbol root,
    Dictionary<string, IPropertySymbol> rootProperties,
    string contractMemberName)
  {
    if (rootProperties.TryGetValue(contractMemberName, out IPropertySymbol? exact)) return exact;

    bool namesTheKey = string.Equals(contractMemberName, root.Name + KeyMemberName, StringComparison.Ordinal);
    return namesTheKey && rootProperties.TryGetValue(KeyMemberName, out IPropertySymbol? key) ? key : null;
  }

  /// <summary>True when a contract type agrees with a root type (see Design).</summary>
  public static bool Compatible(ITypeSymbol contract, ITypeSymbol root) => Compatible(contract, root, depth: 0);

  /// <summary>
  /// True when a query-response type agrees with a root type it projects from: <see cref="Compatible(ITypeSymbol, ITypeSymbol)"/>,
  /// or the contract widens the root's T to T? (read widening, see Design).
  /// </summary>
  public static bool ReadCompatible(ITypeSymbol contract, ITypeSymbol root)
  {
    if (Compatible(contract, root)) return true;

    if (NullableArgument(contract) is { } contractValue && NullableArgument(root) is null)
    {
      return Compatible(contractValue, root);
    }

    return !contract.IsValueType
      && contract.NullableAnnotation == NullableAnnotation.Annotated
      && root.NullableAnnotation != NullableAnnotation.Annotated
      && Compatible(contract.WithNullableAnnotation(NullableAnnotation.NotAnnotated), root);
  }

  /// <summary>Null when <paramref name="member"/> agrees with the root member of its name, else why not.</summary>
  public static string? Disagreement(INamedTypeSymbol root, Dictionary<string, IPropertySymbol> rootProperties, IPropertySymbol member)
  {
    IPropertySymbol? rootMember = FindRootMember(root, rootProperties, member.Name);
    if (rootMember is null) return MissingMember(root, member.Name);

    return Compatible(member.Type, rootMember.Type) ? null : TypeMismatch(root, rootMember, member);
  }

  /// <summary>Message for a contract member with no member of that name on <paramref name="root"/>.</summary>
  public static string MissingMember(INamedTypeSymbol root, string memberName) =>
    $"'{root.Name}' has no public member '{memberName}'; names match exactly, so rename the side that drifted";

  /// <summary>Message for a contract member whose type does not agree with the root member's.</summary>
  public static string TypeMismatch(INamedTypeSymbol root, IPropertySymbol rootMember, IPropertySymbol member)
  {
    string rootType = Display(rootMember.Type);
    string contractType = Display(member.Type);
    if (rootType == contractType)
    {
      rootType = rootMember.Type.ToDisplayString();
      contractType = member.Type.ToDisplayString();
    }

    string message = $"'{root.Name}.{rootMember.Name}' is {rootType} but the contract member is {contractType}";
    return EnumOf(member.Type) is { } contractEnum && EnumOf(rootMember.Type) is { } rootEnum
      ? message + EnumDifference(contractEnum, rootEnum)
      : message;
  }

  /// <summary>A class that can be projected member by member: not System, not string, not a collection.</summary>
  public static bool IsProjectableClass(ITypeSymbol type) =>
    type is INamedTypeSymbol { TypeKind: TypeKind.Class, SpecialType: SpecialType.None } named
    && !IsSystemType(named)
    && !TryGetElement(named, out _);

  /// <summary>Element type of a collection (array or IEnumerable&lt;T&gt;); string is not a collection.</summary>
  public static bool TryGetElement(ITypeSymbol type, out ITypeSymbol element)
  {
    element = null!;
    if (type.SpecialType == SpecialType.System_String) return false;

    if (type is IArrayTypeSymbol array)
    {
      element = array.ElementType;
      return true;
    }

    if (type is not INamedTypeSymbol named) return false;

    INamedTypeSymbol? enumerable =
      named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
        ? named
        : named.AllInterfaces.FirstOrDefault(static candidate =>
          candidate.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
    if (enumerable is null) return false;

    element = enumerable.TypeArguments[0];
    return true;
  }

  private static bool Compatible(ITypeSymbol contract, ITypeSymbol root, int depth)
  {
    if (SameType(contract, root)) return true;

    if (contract.TypeKind == TypeKind.Enum && root.TypeKind == TypeKind.Enum)
    {
      return EnumNames(contract).SetEquals(EnumNames(root));
    }

    if (NullableArgument(contract) is { } contractValue && NullableArgument(root) is { } rootValue)
    {
      return Compatible(contractValue, rootValue, depth);
    }

    if (depth < MaxUnwrapDepth && TryUnwrap(root, out ITypeSymbol wrapped) && Compatible(contract, wrapped, depth + 1))
    {
      return true;
    }

    return depth < MaxUnwrapDepth
      && TryGetElement(contract, out ITypeSymbol contractElement)
      && TryGetElement(root, out ITypeSymbol rootElement)
      && Compatible(contractElement, rootElement, depth + 1);
  }

  private static bool SameType(ITypeSymbol contract, ITypeSymbol root)
  {
    if (!SymbolEqualityComparer.Default.Equals(contract, root)) return false;
    if (contract.IsValueType) return true;
    if (contract.NullableAnnotation == NullableAnnotation.None || root.NullableAnnotation == NullableAnnotation.None) return true;
    return contract.NullableAnnotation == root.NullableAnnotation;
  }

  private static bool TryUnwrap(ITypeSymbol type, out ITypeSymbol wrapped)
  {
    wrapped = null!;
    if (type is not INamedTypeSymbol named || named.SpecialType != SpecialType.None) return false;
    if (IsSystemType(named)) return false;

    List<IPropertySymbol> properties = [.. DeclaredPublicProperties(named)];
    IPropertySymbol? value =
      properties.Find(static property => property.Name == WrapperValueName)
      ?? (properties.Count == 1 ? properties[0] : null);
    if (value is null) return false;

    wrapped = value.Type;
    return true;
  }

  private static ITypeSymbol? NullableArgument(ITypeSymbol type) =>
    type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
      ? nullable.TypeArguments[0]
      : null;

  private static ITypeSymbol? EnumOf(ITypeSymbol type)
  {
    ITypeSymbol candidate = NullableArgument(type) ?? type;
    return candidate.TypeKind == TypeKind.Enum ? candidate : null;
  }

  private static HashSet<string> EnumNames(ITypeSymbol enumType) =>
    new(
      enumType.GetMembers().OfType<IFieldSymbol>().Where(static field => field.HasConstantValue).Select(static field => field.Name),
      StringComparer.Ordinal);

  private static string EnumDifference(ITypeSymbol contractEnum, ITypeSymbol rootEnum)
  {
    HashSet<string> contractNames = EnumNames(contractEnum);
    HashSet<string> rootNames = EnumNames(rootEnum);
    string onlyContract = string.Join(", ", contractNames.Where(name => !rootNames.Contains(name)).OrderBy(static name => name, StringComparer.Ordinal));
    string onlyRoot = string.Join(", ", rootNames.Where(name => !contractNames.Contains(name)).OrderBy(static name => name, StringComparer.Ordinal));
    return $"; the enum member names differ (contract only: {(onlyContract.Length == 0 ? "none" : onlyContract)}; root only: {(onlyRoot.Length == 0 ? "none" : onlyRoot)})";
  }

  private static bool IsSystemType(INamedTypeSymbol type)
  {
    string? namespaceName = type.ContainingNamespace?.ToDisplayString();
    return namespaceName is not null
      && (namespaceName == "System" || namespaceName.StartsWith("System.", StringComparison.Ordinal));
  }

  private static IEnumerable<IPropertySymbol> DeclaredPublicProperties(INamedTypeSymbol type) =>
    type.GetMembers()
      .OfType<IPropertySymbol>()
      .Where(static property =>
        !property.IsStatic
        && !property.IsIndexer
        && property.DeclaredAccessibility == Accessibility.Public
        && property.GetMethod is not null);
}
