namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using __RootNamespace__.Features.Bases;

  public class GetAll__FeatureName__Request : BaseApiRequest, IRequest<GetAll__FeatureName__Response>
  {
    public const string RouteTemplate = "api/__FeatureName__s/GetAll";

    /// <summary>
    /// Number of items that display in one page.
    /// </summary>
    /// <example>5</example>
    public int PageSize { get; set; }

    /// <summary>
    /// Number of pages for displaying items.
    /// </summary>
    /// <example>1</example>
    public int PageIndex { get; set; }

    internal override string GetRoute() => $"{RouteTemplate}?{nameof(PageIndex)}={PageIndex}?{nameof(PageSize)}={PageSize}";
  }
}