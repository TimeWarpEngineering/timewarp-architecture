namespace TimeWarp.Blazor.Api.Features.Base
{
  using System;

  public abstract class BaseRequest
  {
    public Guid Id { get; }

    /// <summary>
    /// Every request should have unique Id
    /// </summary>
    public BaseRequest()
    {
      Id = Guid.NewGuid();
    }
  }
}
