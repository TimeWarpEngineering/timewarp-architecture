namespace TimeWarp.Architecture.Pages;

[Page("/")]
public partial class HomePage : BaseComponent
{
  private async Task FiveSecondTaskButtonClick() =>
    await Send(new ProcessingState.FiveSecondTask.Action());

  private async Task TwoSecondTaskButtonClick() =>
    await Send(new ProcessingState.TwoSecondTask.Action());

  private async Task ModalButtonClick() =>
    await Send(new ApplicationState.SetActiveModal.Action(ModalId: AboutModal.ModalId));
}
