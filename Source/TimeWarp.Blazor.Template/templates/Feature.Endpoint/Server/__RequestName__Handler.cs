namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  
  public class __RequestName__Handler : IRequestHandler<__RequestName__Request, __RequestName__Response>
  {

    public async Task<__RequestName__Response> Handle
    (
      __RequestName__Request a__RequestName__Request,
      CancellationToken aCancellationToken
    )
    {
      var response = new __RequestName__Response(a__RequestName__Request.CorrelationId);

      return await Task.FromResult(response);
    }
  }
}
