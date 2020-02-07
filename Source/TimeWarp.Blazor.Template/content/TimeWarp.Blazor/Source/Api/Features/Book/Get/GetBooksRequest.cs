namespace TimeWarp.Blazor.BookFeature
{
  using MediatR;
  using System.Text.Json.Serialization;
  using TimeWarp.Blazor.Api.Features.Base;

  public class GetBooksRequest : BaseRequest, IRequest<GetBooksResponse>
  {
    public const string Route = "api/book";

    [JsonIgnore]
    public string RouteFactory => $"{Route}?{nameof(Id)}={Id}";
  }
}
