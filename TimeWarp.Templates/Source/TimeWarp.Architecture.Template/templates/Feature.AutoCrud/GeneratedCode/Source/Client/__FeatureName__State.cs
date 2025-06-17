namespace __RootNamespace__.Features.__FeatureName__s
{
  using BlazorState;
  using System.Collections.Generic;
  using System.Linq;
  using System;

  internal partial class __FeatureName__State : State<__FeatureName__State>
  {
    public int PageSize { get; private set; }
    public int PageIndex { get; private set; }

    private Dictionary<Guid, __FeatureName__Dto> ___FeatureName__s;

    public __FeatureName__Dto Current__FeatureName__ { get; private set; }

    public IReadOnlyDictionary<Guid, __FeatureName__Dto> __FeatureName__s => ___FeatureName__s;

    public IReadOnlyList<__FeatureName__Dto> __FeatureName__sAsList => ___FeatureName__s.Values.ToList();

    public __FeatureName__State()
    {
      Initialize();
    }

    public override void Initialize()
    {
      PageIndex = 0;
      PageSize = 20;
      ___FeatureName__s = new Dictionary<Guid, __FeatureName__Dto>();
    }
  }
}
