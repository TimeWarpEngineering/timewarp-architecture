namespace __RootNamespace__.Features.__FeatureName__s
{
  using BlazorState;

  public partial class __FeatureName__State : State<__FeatureName__State>
  {
    public int PageSize { get; private set; }
    public int PageIndex { get; private set; }

    private Dictionary<string, __FeatureName__> ___FeatureName__s;

    public __FeatureName__ Current__FeatureName__ { get; private set; }

    public IReadOnlyDictionary<string, __FeatureName__> __FeatureName__s => ___FeatureName__s;

    public IReadOnlyList<__FeatureName__> __FeatureName__sAsList => ___FeatureName__s.Values.ToList();

    public __FeatureName__State()
    {
      Initialize();
    }

    public override void Initialize()
    {
      PageIndex = 0;
      PageSize = 20;
      ___FeatureName__s = new Dictionary<string, __FeatureName__>();
    }
  }
}
