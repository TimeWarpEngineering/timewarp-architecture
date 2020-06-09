namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System.Text.Json.Serialization;
  using __RootNamespace__.Features.Bases;

  public class __RequestName__Request : BaseApiRequest, IRequest<__RequestName__Response>
  {
    public const string Route = "api/__FeatureName__/__RequestName__";

    /// <summary>
    /// The Number of days of forecasts to get
    /// </summary>
    /// <example>5</example>
    public int Days { get; set; }

    internal override string RouteFactory => $"{Route}?{nameof(Id)}={Id}";
  }
}