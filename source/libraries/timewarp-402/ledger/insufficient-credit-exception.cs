#region Purpose
// Fail-closed debit when a principal lacks sufficient balance for metered work.
#endregion

namespace TimeWarp.X402;

using TimeWarp.Identity;

/// <summary>Thrown by <see cref="ICreditLedger.DebitAsync"/> when balance is too low.</summary>
public sealed class InsufficientCreditException : InvalidOperationException
{
  public PrincipalId PrincipalId { get; }
  public decimal Requested { get; }
  public decimal Available { get; }

  public InsufficientCreditException()
    : this(default, 0m, 0m)
  {
  }

  public InsufficientCreditException(string message)
    : base(message)
  {
  }

  public InsufficientCreditException(string message, Exception innerException)
    : base(message, innerException)
  {
  }

  public InsufficientCreditException(PrincipalId principalId, decimal requested, decimal available)
    : base($"Insufficient credit for principal {principalId}: requested {requested}, available {available}.")
  {
    PrincipalId = principalId;
    Requested = requested;
    Available = available;
  }
}
