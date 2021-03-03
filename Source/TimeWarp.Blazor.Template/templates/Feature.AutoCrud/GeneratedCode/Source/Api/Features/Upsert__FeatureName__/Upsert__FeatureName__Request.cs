namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using __RootNamespace__.Features.Bases;
  using System;

  public class Upsert__FeatureName__Request : BaseApiRequest, IRequest<Upsert__FeatureName__Response>
  {
    public const string RouteTemplate = "api/__FeatureName__s/Upsert";
    public Guid Id { get; private set; }
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

    internal override string GetRoute() => $"{RouteTemplate}?{nameof(Id)}={Id}&{nameof(CorrelationId)}={CorrelationId}";
  }
}