namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System.Text.Json.Serialization;
  using __RootNamespace__.Features.Bases;

  public class __RequestName__Request : BaseRequest, IRequest<__RequestName__Response>
  {
    public const string Route = "api/__FeatureName__/__RequestName__";

    [JsonIgnore]
    public string RouteFactory => $"{Route}?{nameof(Id)}={Id}";

    // Add Specific Request Properties
  }
}
