namespace TimeWarp.Blazor.Components
{
  using Microsoft.AspNetCore.Components;
  using System;
  using System.Collections.Generic;

  public class DisplayComponent : ComponentBase
  {
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object> Attributes { get; set; } = new Dictionary<string, Object>();
  }
}
