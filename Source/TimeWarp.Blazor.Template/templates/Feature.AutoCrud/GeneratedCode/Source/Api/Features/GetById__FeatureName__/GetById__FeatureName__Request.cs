namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System;
  using __RootNamespace__.Features.Bases;

  public class GetById__FeatureName__Request : BaseApiRequest, IRequest<GetById__FeatureName__Response>
  {
    public const string RouteTemplate = "api/__FeatureName__s/Get";

    /// <summary>
    /// Guid ID for individual item.
    /// </summary>
    /// <example>82b85a2c-c5e4-4306-a803-08d8de1257c1</example>
    public Guid Id { get; set; }

    internal override string GetRoute() => $"{RouteTemplate}?{nameof(Id)}={Id}";
  }
}