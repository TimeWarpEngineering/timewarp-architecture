namespace TimeWarp.Architecture.Pages;

[Page("/")]
public partial class Index : BaseComponent
{
  private async Task FiveSecondTaskButtonClick() =>
    await Send(new ApplicationState.FiveSecondTaskAction());

  private async Task TwoSecondTaskButtonClick() =>
    await Send(new ApplicationState.TwoSecondTaskAction());
}
