#region Purpose
// Serializable success/failure envelope for SignalR hub method returns, because OneOf discriminated unions do not survive SignalR serialization.
#endregion

namespace TimeWarp.Architecture.Types;

public sealed class SignalrResult<TSuccess, TFailure>
{
  public bool IsSuccess { get; set; }
  public TSuccess? Success { get; set; }
  public TFailure? Failure { get; set; }
}
