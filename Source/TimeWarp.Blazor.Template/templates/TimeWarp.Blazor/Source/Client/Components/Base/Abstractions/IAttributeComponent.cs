namespace TimeWarp.Blazor.Components
{
  using Microsoft.AspNetCore.Components;
  using System.Collections.Generic;

  public interface IAttributeComponent
  {
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object> Attributes { get; set; }
  }
}
