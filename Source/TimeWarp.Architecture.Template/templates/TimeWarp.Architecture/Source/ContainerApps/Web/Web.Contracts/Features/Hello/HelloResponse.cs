namespace TimeWarp.Architecture.Features.Hello;
using System;

public record HelloResponse : BaseResponse
{
  public HelloResponse(Guid aCorrelationId) : base(aCorrelationId) { }
}
