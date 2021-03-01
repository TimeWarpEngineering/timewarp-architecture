namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using __RootNamespace__.Features.Bases;

  public class __FeatureName__GetRequest : BaseApiRequest, IRequest<__FeatureName__GetResponse>
  {
    public const string RouteTemplate = "api/__FeatureName__s/__FeatureName__Get";

    /// <summary>
    /// Set Properties and Update Docs
    /// </summary>
    /// <example>TODO</example>
    public int PageSize { get; private set; }
    public int PageIndex { get; private set; }

    internal override string GetRoute() => $"{RouteTemplate}?{nameof(PageIndex)}={PageIndex}?{nameof(PageSize)}={PageSize}";
  }
}