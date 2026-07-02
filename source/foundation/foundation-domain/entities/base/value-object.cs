#region Purpose
// DDD base class giving domain value objects structural equality without per-type boilerplate.
#endregion

#region Design
// Subclasses supply only GetEqualityComponents; equality, hashing, and operator helpers derive
// from that single source so the two can never disagree.
// Equality requires exact runtime type match — a derived value object is never equal to its base,
// preventing accidental cross-type equivalence.
// Class (not record) so equality semantics stay explicit and mixed reference/value comparison
// bugs surface via the null-XOR EqualOperator helper.
#endregion

namespace TimeWarp.Foundation.Domain.Base;

// https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects
// https://enterprisecraftsmanship.com/posts/value-object-better-implementation/
public abstract class ValueObject
{
  protected static bool EqualOperator(ValueObject left, ValueObject right)
  {
    if (left is null ^ right is null)
    {
      return false;
    }

    return left?.Equals(right!) != false;
  }

  protected static bool NotEqualOperator(ValueObject left, ValueObject right)
  {
    return !(EqualOperator(left, right));
  }

  protected abstract IEnumerable<object> GetEqualityComponents();

  public override bool Equals(object? obj)
  {
    if (obj == null || obj.GetType() != GetType())
    {
      return false;
    }

    var other = (ValueObject)obj;
    return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
  }

  public override int GetHashCode()
  {
    return GetEqualityComponents()
        .Select(x => x != null ? x.GetHashCode() : 0)
        .Aggregate((x, y) => x ^ y);
  }
}
