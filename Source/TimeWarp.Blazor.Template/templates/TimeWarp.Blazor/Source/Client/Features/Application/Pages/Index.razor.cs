namespace TimeWarp.Blazor.Pages
{
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Applications;
  using TimeWarp.Blazor.Features.Bases;

  public partial class Index : BaseComponent
  {
    private const string RouteTemplate = "/";

    public static string GetRoute() => RouteTemplate;

    private async Task FiveSecondTaskButtonClick() =>
      await Send(new ApplicationState.FiveSecondTaskAction());

    private async Task TwoSecondTaskButtonClick() =>
      await Send(new ApplicationState.TwoSecondTaskAction());
  }
}
