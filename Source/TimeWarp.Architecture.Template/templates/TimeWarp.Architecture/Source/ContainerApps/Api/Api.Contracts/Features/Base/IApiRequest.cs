namespace TimeWarp.Architecture.Features;

using System;

public interface IApiRequest
{
  public Guid CorrelationId { get; init; }
  string GetRoute();
  HttpVerb GetHttpVerb();
}
