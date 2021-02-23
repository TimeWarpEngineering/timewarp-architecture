namespace __RootNamespace__.Features.__FeatureName__s
{
  using System;
  using System.Collections.Generic;
  using __RootNamespace__.Features.Bases;

  public class __FeatureName__CreateResponse : BaseResponse
  {
    public __FeatureName__Dto __FeatureName__ { get; set; }
    public __FeatureName__CreateResponse() { }

    public __FeatureName__CreateResponse(Guid aCorrelationId) : base(aCorrelationId) { }
  }
}
