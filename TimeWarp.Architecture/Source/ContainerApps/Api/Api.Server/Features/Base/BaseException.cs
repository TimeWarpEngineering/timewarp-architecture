namespace TimeWarp.Architecture.Features;

public class BaseException : Exception
{
  public BaseException() { }

  public BaseException(string aMessage) : base(aMessage) { }
}
