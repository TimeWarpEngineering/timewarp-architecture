namespace __RootNamespace__.Features.__FeatureName__s
{
  using System;
  using System.Collections.Generic;
  using __RootNamespace__.Features.Bases;

  public class __RequestName__Response : BaseResponse
  {
    /// <summary>
    /// a default constructor is required for deserialization
    /// </summary>
    public __RequestName__Response() { }

    public __RequestName__Response(Guid aRequestId)
    {

      RequestId = aRequestId;
    }
  }
}
