namespace BlazorHosted.Features.__FeatureName__
{
  using BlazorState;
  using System.Collections.Generic;

  public partial class __FeatureName__State : State<__FeatureName__State>
  {
    public List<__FeatureName__Dto> ___FeatureName__s { get; set; } = null!;

    public IReadOnlyList<__FeatureName__Dto> __FeatureName__s => ___FeatureName__Records.AsReadOnly();
    public __FeatureName__State() 
    {
      ___FeatureName__Records = new List<__FeatureName__Dto>();
    }

    public override void Initialize() 
    {
      ___FeatureName__s.Clear();
    }

  }
}
