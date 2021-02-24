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

    internal override string RouteFactory =>
      $"{Route}?{nameof(ItemId)}={ItemId}"
      .Replace
      (
        $"{{{nameof(ItemId)}}}",
        ItemId.ToString(),
        System.StringComparison.OrdinalIgnoreCase
      );
  }
}