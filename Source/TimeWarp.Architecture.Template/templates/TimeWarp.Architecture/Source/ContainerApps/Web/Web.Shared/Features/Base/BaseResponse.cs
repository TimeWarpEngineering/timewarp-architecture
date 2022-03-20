namespace TimeWarp.Architecture.Features.Bases
{
  using System;

  public abstract class BaseResponse
  {
    /// <summary>
    /// Used to correlate request and response
    /// </summary>
    public Guid CorrelationId { get; set; }

    public BaseResponse(Guid aCorrelationId) : this()
    {
      CorrelationId = aCorrelationId;
    }
    public BaseResponse() { }
  }
}
