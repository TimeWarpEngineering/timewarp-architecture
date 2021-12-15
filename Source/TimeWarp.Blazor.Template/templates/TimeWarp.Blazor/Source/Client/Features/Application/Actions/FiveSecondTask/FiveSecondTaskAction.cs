namespace TimeWarp.Blazor.Features.Applications
{
  using TimeWarp.Blazor.Features.Bases;

  internal partial class ApplicationState
  {
    [TrackProcessing]
    public class FiveSecondTaskAction : BaseAction { }
  }
}
