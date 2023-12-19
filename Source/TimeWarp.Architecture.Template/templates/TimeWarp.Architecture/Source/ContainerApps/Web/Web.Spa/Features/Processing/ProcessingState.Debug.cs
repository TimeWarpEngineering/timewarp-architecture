namespace TimeWarp.Architecture.Features.Processing;

internal partial class ProcessingState : State<ProcessingState>
{
  public override ProcessingState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    return new ProcessingState
    {
      Guid = new System.Guid(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString()),
    };
  }

  internal void Initialize(List<string> processingList)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    _ProcessingList = processingList;
  }
}
