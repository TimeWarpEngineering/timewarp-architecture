#nullable enable
namespace TimeWarp.Architecture.Components;

public abstract class DisplayComponent : ComponentBase, IAttributeComponent
{
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
}
