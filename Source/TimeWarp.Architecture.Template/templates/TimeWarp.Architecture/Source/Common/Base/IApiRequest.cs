namespace TimeWarp.Architecture.Features;

public interface IApiRequest
{
  public Guid CorrelationId { get; init; }
  string GetRoute();
  HttpVerb GetHttpVerb();
}
