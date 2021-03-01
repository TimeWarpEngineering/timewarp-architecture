namespace __RootNamespace__.Features.__FeatureName__s
{
  using System;
  using System.Collections.Generic;
  using __RootNamespace__.Features.Bases;

  public class __FeatureName__GetResponse : BaseResponse
  {
    public List<__FeatureName__Dto> __FeatureName__s { get; set; }
    public __FeatureName__GetResponse() { }

    public __FeatureName__GetResponse(Guid aCorrelationId) : base(aCorrelationId) { }
  }
}
