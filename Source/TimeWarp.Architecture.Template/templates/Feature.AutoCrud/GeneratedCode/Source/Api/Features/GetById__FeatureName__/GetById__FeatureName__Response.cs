namespace __RootNamespace__.Features.__FeatureName__s
{
  using System;
  using System.Collections.Generic;
  using __RootNamespace__.Features.Bases;

  public class GetById__FeatureName__Response : BaseResponse
  {
    public __FeatureName__Dto __FeatureName__ { get; set; }
    public GetById__FeatureName__Response() { }

    public GetById__FeatureName__Response(Guid aCorrelationId) : base(aCorrelationId) { }
  }
}
