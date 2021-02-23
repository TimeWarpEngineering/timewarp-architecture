namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  
  public class __FeatureName__CreateHandler : IRequestHandler<__FeatureName__CreateRequest, __FeatureName__CreateResponse>
  {

    public async Task<__FeatureName__CreateResponse> Handle
    (
      __FeatureName__CreateRequest a__FeatureName__CreateRequest,
      CancellationToken aCancellationToken
    )
    {
      var response = new __FeatureName__CreateResponse(a__FeatureName__CreateRequest.CorrelationId);

      return await Task.FromResult(response);
    }
  }
}
