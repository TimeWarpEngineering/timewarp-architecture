namespace TimeWarp.Architecture.Features.Hello;

public record HelloResponse : BaseResponse
{
  public HelloResponse(Guid aCorrelationId) : base(aCorrelationId) { }
}
