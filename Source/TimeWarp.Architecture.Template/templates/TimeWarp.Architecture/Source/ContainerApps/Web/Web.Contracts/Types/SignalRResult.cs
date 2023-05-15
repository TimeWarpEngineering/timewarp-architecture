namespace TimeWarp.Architecture.Types;

public sealed class SignalrResult<TSuccess, TFailure>
{
  public bool IsSuccess { get; set; }
  public TSuccess? Success { get; set; }
  public TFailure? Failure { get; set; }
}
