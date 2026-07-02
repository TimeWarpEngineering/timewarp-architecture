#region Purpose
// Base exception type for Api server feature failures; a convention anchor with no derived types in the template.
#endregion

namespace TimeWarp.Architecture.Features;

public class BaseException : Exception
{
  public BaseException() { }

  public BaseException(string message) : base(message) { }
}
