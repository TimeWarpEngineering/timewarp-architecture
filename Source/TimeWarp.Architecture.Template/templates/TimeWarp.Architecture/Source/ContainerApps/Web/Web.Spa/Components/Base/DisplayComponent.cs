namespace TimeWarp.Architecture.Components;

public class DisplayComponent : ComponentBase, IAttributeComponent
{
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
}
