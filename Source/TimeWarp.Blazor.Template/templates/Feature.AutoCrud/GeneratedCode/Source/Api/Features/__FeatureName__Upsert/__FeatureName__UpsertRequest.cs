namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using __RootNamespace__.Features.Bases;

  public class __FeatureName__UpsertRequest : BaseApiRequest, IRequest<__FeatureName__UpsertResponse>
  {
    public const string RouteTemplate = "api/__FeatureName__s/__FeatureName__CreateRequest";
    public int Id { get; set; }
    /// <summary>
    /// Name of the Item
    /// </summary>
    /// <example>Super Cool Item</example>
    public string Name { get; set; }
    /// <summary>
    /// Description of the Item
    /// </summary>
    /// <example>Super cool thing.</example>
    public string Description { get; set; }

    /// <summary>
    /// The Price of the Item
    /// </summary>
    /// <example>999.99</example>
    public decimal Price { get; set; }

    internal override string GetRoute() => $"{Route}?{nameof(Id)}={Id}"
      .Replace
      (
        $"{{{nameof(Id)}}}",
        ItemId.ToString(),
        System.StringComparison.OrdinalIgnoreCase
      );
  }
}