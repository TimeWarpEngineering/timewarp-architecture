namespace TimeWarp.Architecture.Features;

/// <summary>
/// Base Response used for all Requests
/// </summary>
/// <remarks>
/// Response should be immutable reference types.
/// Remember to declare your properties as init only.
/// </remarks>
public abstract record BaseResponse : BaseMessage
{
  public BaseResponse() : base()
  {
  }
}
