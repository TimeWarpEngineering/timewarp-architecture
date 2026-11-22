#region Purpose
// Value-equal wrapper over ImmutableArray<T> so incremental generator models that hold lists still cache.
#endregion

#region Design
// ImmutableArray<T> compares by reference, so a record holding one is never equal across runs and every
// downstream step reruns. This struct compares element by element with EqualityComparer<T>.Default. A default
// (uninitialized) array is treated as empty. Kept minimal: the contract-state generator is the only user.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>An immutable array that compares by its elements.</summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
  where T : IEquatable<T>
{
  private readonly ImmutableArray<T> Items;

  public EquatableArray(IEnumerable<T> items)
  {
    Items = items.ToImmutableArray();
  }

  public int Count => Items.IsDefault ? 0 : Items.Length;

  public T this[int index] => Items[index];

  public bool Equals(EquatableArray<T> other) =>
    AsArray().SequenceEqual(other.AsArray());

  public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

  public override int GetHashCode()
  {
    int hash = 17;
    foreach (T item in AsArray())
    {
      hash = unchecked((hash * 31) + EqualityComparer<T>.Default.GetHashCode(item));
    }

    return hash;
  }

  public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)AsArray()).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  private ImmutableArray<T> AsArray() => Items.IsDefault ? ImmutableArray<T>.Empty : Items;

  public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

  public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
