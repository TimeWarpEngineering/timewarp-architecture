namespace TimeWarp.Architecture.Pages;

[UsedImplicitly]
[Page("/")]
public partial class HomePage : BaseComponent
{
  [Inject]
  private ILogger<HomePage> Logger { get; set; } = null!;

  private async Task FiveSecondTaskButtonClick() =>
    await Send(new ActionTrackingState.FiveSecondTask.Action());

  private async Task TwoSecondTaskButtonClick() =>
    await Send(new ActionTrackingState.TwoSecondTask.Action());

  private async Task ModalButtonClick() =>
    await Send(new ApplicationState.SetActiveModal.Action(ModalId: AssemblyInfoModal.ModalId));

  protected override void OnInitialized()
  {
    base.OnInitialized();
    Logger.LogDebug("This is a debug message");
    Logger.LogInformation("This is an info message");
    Logger.LogWarning("This is a warning message");
    Logger.LogError("This is an error message");
    // Logger.LogCritical("This is a critical message");
  }
}
