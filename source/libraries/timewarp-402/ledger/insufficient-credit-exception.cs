#region Purpose
// Fail-closed debit when a principal lacks sufficient balance for metered work.
#endregion

namespace TimeWarp.X402;

using TimeWarp.Identity;

/// <summary>Thrown by <see cref="ICreditLedger.DebitAsync"/> when balance is too low.</summary>
public sealed class InsufficientCreditException : InvalidOperationException
{
  /// <summary>Principal whose balance was insufficient for the debit.</summary>
  public PrincipalId PrincipalId { get; }
  /// <summary>Debit amount that was requested.</summary>
  public decimal Requested { get; }
  /// <summary>Balance present when the debit failed.</summary>
  public decimal Available { get; }

  /// <summary>Creates an exception with empty principal and zero amounts.</summary>
  public InsufficientCreditException()
    : this(default, 0m, 0m)
  {
  }

  /// <summary>Creates an exception with the specified message.</summary>
  public InsufficientCreditException(string message)
    : base(message)
  {
  }

  /// <summary>Creates an exception with the specified message and inner exception.</summary>
  public InsufficientCreditException(string message, Exception innerException)
    : base(message, innerException)
  {
  }

  /// <summary>Creates an exception describing an insufficient debit for the given principal.</summary>
  public InsufficientCreditException(PrincipalId principalId, decimal requested, decimal available)
    : base($"Insufficient credit for principal {principalId}: requested {requested}, available {available}.")
  {
    PrincipalId = principalId;
    Requested = requested;
    Available = available;
  }
}
