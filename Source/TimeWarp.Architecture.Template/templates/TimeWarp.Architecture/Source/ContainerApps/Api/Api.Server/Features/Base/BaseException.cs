namespace TimeWarp.Architecture.Features;

using System;

public class BaseException : Exception
{
  public BaseException() { }

  public BaseException(string aMessage) : base(aMessage) { }
}
