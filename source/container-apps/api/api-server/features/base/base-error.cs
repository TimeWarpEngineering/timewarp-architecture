#region Purpose
// Base message-bearing error type for Api server features; a convention anchor with no derived types in the template.
#endregion

namespace TimeWarp.Architecture.Features;

public class BaseError
{
  public BaseError(string message)
  {
    Message = message;
  }

  public string Message { get; set; }
}
