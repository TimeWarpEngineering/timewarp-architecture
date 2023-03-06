namespace TimeWarp.Architecture.Components;

public interface IAttributeComponent
{
  [Parameter(CaptureUnmatchedValues = true)]
  IReadOnlyDictionary<string, object> Attributes { get; set; }
}
