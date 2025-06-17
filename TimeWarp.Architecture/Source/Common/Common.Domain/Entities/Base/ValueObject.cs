﻿namespace TimeWarp.Architecture.Domain.Base;

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
