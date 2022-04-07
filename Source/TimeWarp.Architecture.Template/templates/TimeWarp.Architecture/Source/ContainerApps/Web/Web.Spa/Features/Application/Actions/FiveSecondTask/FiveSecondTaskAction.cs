namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Features.Bases;

internal partial class ApplicationState
{
  [TrackProcessing]
  public class FiveSecondTaskAction : BaseAction { }
}
