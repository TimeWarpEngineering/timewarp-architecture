namespace TimeWarp.Blazor.Features.Bases
{
  using System;

  public interface IApiRequest
  {
    public Guid CorrelationId { get; set; }
    string GetRoute();
    HttpVerb GetHttpVerb();
  }
}
