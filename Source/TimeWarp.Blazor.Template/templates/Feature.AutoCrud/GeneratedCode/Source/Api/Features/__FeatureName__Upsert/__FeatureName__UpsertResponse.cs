namespace __RootNamespace__.Features.__FeatureName__s
{
  using System;
  using System.Collections.Generic;
  using __RootNamespace__.Features.Bases;

  public class __FeatureName__UpsertResponse : BaseResponse
  {
    public __FeatureName__Dto __FeatureName__ { get; set; }
    public __FeatureName__UpsertResponse() { }

    public __FeatureName__UpsertResponse(Guid aCorrelationId) : base(aCorrelationId) { }
  }
}
