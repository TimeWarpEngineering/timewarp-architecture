#region Purpose
// Marker interface tagging aggregate roots — the consistency boundary the domain invariants
// guard validates before every persisted change, and that TWA0011 requires to declare a nested
// Invariants validator.
#endregion

namespace TimeWarp.Foundation.Entities;

/// <summary>
/// Marker interface for an aggregate root: the consistency boundary that
/// <c>DomainInvariantsGuard</c> validates before every persisted change. An aggregate root
/// inherits <see cref="Entity{TId}"/>, is constructed only through a fail-closed static
/// <c>Create</c> factory, exposes only named mutation methods (no public setters), and declares
/// a private nested <c>Invariants</c> validator (enforced at build time by rule TWA0011; the
/// validator must be <c>private</c> per rule TWA0012 so it is never picked up by request-validator
/// auto-registration).
/// </summary>
/// <example>
/// <code>
/// public sealed class Order : Entity&lt;OrderId&gt;, IAggregateRoot
/// {
///   private Order(OrderId id, string customerName) : base(id)
///   {
///     CustomerName = customerName;
///   }
///
///   public string CustomerName { get; private set; }
///
///   public static Order Create(string customerName)
///   {
///     ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
///     return new Order(OrderId.New(), customerName);
///   }
///
///   public void Rename(string customerName)
///   {
///     ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
///     CustomerName = customerName;
///   }
///
///   private sealed class Invariants : AbstractValidator&lt;Order&gt;
///   {
///     public Invariants() => RuleFor(order => order.CustomerName).NotEmpty();
///   }
/// }
/// </code>
/// </example>
#pragma warning disable CA1040
public interface IAggregateRoot;
#pragma warning restore CA1040
