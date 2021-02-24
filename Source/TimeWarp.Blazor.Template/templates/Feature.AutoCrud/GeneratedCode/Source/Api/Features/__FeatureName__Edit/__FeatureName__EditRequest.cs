namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using __RootNamespace__.Features.Bases;

  public class __RequestName__Request : BaseApiRequest, IRequest<__RequestName__Response>
  {
    public const string RouteTemplate = "api/__FeatureName__s/__FeatureName__Edit";

    /// <summary>
    /// Set Properties and Update Docs
    /// </summary>
    /// <example>TODO</example>
    public int Id{ get; set; }

    internal override string GetRoute() => $"{RouteTemplate}?{nameof(Id)}={Id}&{nameof(CorrelationId)}={CorrelationId}";
  }
}