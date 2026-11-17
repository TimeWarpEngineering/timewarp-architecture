#region Purpose
// Pipeline models for ContractsGenerator: one emitted member group (ContractPart) and one partial
// contract type with its merged parts (ContractTarget).
#endregion

namespace TimeWarp.Foundation.Contracts.Generators;

using System.Collections.Immutable;
using System.Linq;

/// <summary>One attribute's generated members: its kind, optional base interface, and member text.</summary>
internal readonly record struct ContractPart(string Kind, string? BaseType, string Body);

// Equatable so RegisterSourceOutput skips when parts/identity are unchanged (IReadOnlyList used
// reference equality and re-emitted on every transform). ImmutableArray.Equals is also by
// backing-array reference, so this type SequenceEquals the arrays. Containers are outer -> inner,
// excluding the target class.
internal readonly record struct ContractTarget(
  string Namespace,
  ImmutableArray<string> Containers,
  string ClassName,
  string HintBase,
  ImmutableArray<ContractPart> Parts)
{
  public bool Equals(ContractTarget other) =>
    Namespace == other.Namespace
    && ClassName == other.ClassName
    && HintBase == other.HintBase
    && Containers.SequenceEqual(other.Containers)
    && Parts.SequenceEqual(other.Parts);

  public override int GetHashCode()
  {
    HashCode hashCode = new();
    hashCode.Add(Namespace);
    hashCode.Add(ClassName);
    hashCode.Add(HintBase);
    foreach (string container in Containers)
      hashCode.Add(container);
    foreach (ContractPart part in Parts)
      hashCode.Add(part);
    return hashCode.ToHashCode();
  }
}
