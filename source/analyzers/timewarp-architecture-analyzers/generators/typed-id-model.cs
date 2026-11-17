#region Purpose
// Pipeline models for TypedIdSourceGenerator: one typed id, one [TypedId] declaration candidate,
// and the equatable EF-pass input.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;

/// <summary>A valid [TypedId] struct: its name, fully qualified name, namespace, and hint stem.</summary>
internal readonly record struct TypedIdModel(
  string Name,
  string FullyQualifiedName,
  string NamespaceName,
  string HintName);

/// <summary>A [TypedId]-attributed declaration: either a valid model or an invalid shape to report.</summary>
internal readonly record struct TypedIdCandidate(TypedIdModel? Model, string? InvalidName, Location? InvalidLocation);

/// <summary>Equatable EF-pass input so the EF output only regenerates when the id set or EF-ness changes.</summary>
internal sealed class TypedIdEfModel : IEquatable<TypedIdEfModel>
{
  public TypedIdEfModel(bool hasEf, ImmutableArray<TypedIdModel> ids)
  {
    HasEf = hasEf;
    Ids = ids;
  }

  public bool HasEf { get; }
  public ImmutableArray<TypedIdModel> Ids { get; }

  public bool Equals(TypedIdEfModel? other) =>
    other is not null && HasEf == other.HasEf && Ids.SequenceEqual(other.Ids);

  public override bool Equals(object? obj) => Equals(obj as TypedIdEfModel);

  public override int GetHashCode()
  {
    int hash = HasEf ? 17 : 31;
    foreach (TypedIdModel model in Ids)
      hash = unchecked((hash * 397) ^ model.FullyQualifiedName.GetHashCode(StringComparison.Ordinal));
    return hash;
  }
}
