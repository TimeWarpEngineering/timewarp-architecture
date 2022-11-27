namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using __RootNamespace__.Features.Bases;

  public class Delete__FeatureName__Request : BaseApiRequest, IRequest<Delete__FeatureName__Response>
  {
    public const string RouteTemplate = "api/__FeatureName__s/Delete";

    /// <summary>
    /// Guid ID uses to identify product item that you want to delete.
    /// </summary>
    /// <example>efd66079-23a4-4166-a806-08d8de1257c1</example>
    public string ItemId { get; set; }

    internal override string GetRoute() => $"{RouteTemplate}?{nameof(ItemId)}={ItemId}";
  }
}