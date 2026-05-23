namespace TimeWarp.Architecture.Components;

public interface IAttributeComponent
{
  IReadOnlyDictionary<string, object> Attributes { get; set; }
}
