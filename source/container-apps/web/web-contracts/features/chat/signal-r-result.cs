#region Purpose
// Serializable success/failure envelope for SignalR hub method returns, because OneOf discriminated unions do not survive SignalR serialization.
#endregion

#region Design
// Generic by shape but chat-owned by residence: the chat hub is its only consumer. If a second
// hub needs it, TWPA0009 will flag the cross-feature reference — promote it to a shared home then.
#endregion

namespace TimeWarp.Architecture.Features.Chat;

public sealed class SignalrResult<TSuccess, TFailure>
{
  public bool IsSuccess { get; set; }
  public TSuccess? Success { get; set; }
  public TFailure? Failure { get; set; }
}
