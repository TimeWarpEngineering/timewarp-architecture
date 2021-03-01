namespace __RootNamespace__.Features.__FeatureName__s
{
  using System;
  using System.Collections.Generic;
  using __RootNamespace__.Features.Bases;

  public class __RequestName__Response : BaseResponse
  {
    public string StatusMessage { get; set; }
    public __RequestName__Response() { }

    public __RequestName__Response(Guid aCorrelationId, string aStatusMessage) : base(aCorrelationId) { }
  }
}
