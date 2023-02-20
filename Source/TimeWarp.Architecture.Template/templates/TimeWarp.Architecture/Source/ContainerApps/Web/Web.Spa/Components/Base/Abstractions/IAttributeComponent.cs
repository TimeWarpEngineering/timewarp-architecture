namespace TimeWarp.Architecture.Components;

public interface IAttributeComponent
{
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object> Attributes { get; set; }
}
