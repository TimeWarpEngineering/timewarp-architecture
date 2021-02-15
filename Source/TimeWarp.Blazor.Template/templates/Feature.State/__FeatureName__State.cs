namespace __RootNamespace__.Features.__FeatureName__s
{
  using BlazorState;

  public partial class __FeatureName__State : State<__FeatureName__State>
  {

    public int SamepleNumber { get; private set; }
    public __FeatureName__State() {}

    public override void Initialize() 
    {
      SamepleNumber = 10;
    }

  }
}
