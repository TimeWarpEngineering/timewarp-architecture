namespace TimeWarp.Architecture.Pages;

[Page("/")]
public partial class Index : BaseComponent
{
  private async Task FiveSecondTaskButtonClick() =>
    await Send(new ApplicationState.FiveSecondTask.Action());

  private async Task TwoSecondTaskButtonClick() =>
    await Send(new ApplicationState.TwoSecondTaskAction());

  private async Task ModalButtonClick() =>
    await Send(new ApplicationState.SetActiveModalAction(ModalId: AboutModal.ModalId));
}
