namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using __RootNamespace__.Features.Bases;

  public class __RequestName__Request : BaseApiRequest, IRequest<__RequestName__Response>
  {
    public const string RouteTemplate = "api/__FeatureName__s/__RequestName__";

    /// <summary>
    /// Set Properties and Update Docs
    /// </summary>
    /// <example>TODO</example>
    public string ItemId { get; set; }

    internal override string GetRoute() => $"{RouteTemplate}?{nameof(ItemId)}={ItemId}&{nameof(CorrelationId)}={CorrelationId}";
  }
}