namespace TimeWarp.Blazor.BookFeature
{
  using Microsoft.AspNetCore.Mvc;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Server.Features.Base;

  [Route(GetBooksRequest.Route)]
  public class GetWeatherForecastsController : BaseController<GetBooksRequest, GetBooksResponse>
  {
    [HttpGet]
    public async Task<IActionResult> Process(GetBooksRequest aRequest) => await Send(aRequest);
  }
}
