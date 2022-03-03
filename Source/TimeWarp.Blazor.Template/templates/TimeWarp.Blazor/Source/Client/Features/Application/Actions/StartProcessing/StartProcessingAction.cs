﻿namespace TimeWarp.Blazor.Features.Applications
{
  using TimeWarp.Blazor.Features.Bases;

  internal partial class ApplicationState
  {
    public class StartProcessingAction : BaseAction
    {
      public string ActionName { get; set; }
    }
  }
}