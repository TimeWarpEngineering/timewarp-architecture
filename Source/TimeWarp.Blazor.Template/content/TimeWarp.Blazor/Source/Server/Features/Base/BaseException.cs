namespace TimeWarp.Blazor.Features.Bases.Server
{
  using System;

  public class BaseException : Exception
  {
    public BaseException() { }

    public BaseException(string aMessage) : base(aMessage) { }
  }
}
