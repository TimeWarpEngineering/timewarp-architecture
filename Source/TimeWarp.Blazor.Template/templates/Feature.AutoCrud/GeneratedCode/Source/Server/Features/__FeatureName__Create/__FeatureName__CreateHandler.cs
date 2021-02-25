namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootspaceName__.Data;


  public class __FeatureName__CreateHandler : IRequestHandler<__FeatureName__CreateRequest, __FeatureName__CreateResponse>
  {
    private readonly ApplicationDbContext DbContext;
    private readonly IMapper Mapper;

    public __FeatureName__CreateHandler(
      ApplicationDbContext aDbContext,
      IMapper aMapper;
    )
    {
      Mpper = aMapper;
      DbContext = aDbContext;
    }

    public async Task<__FeatureName__CreateResponse> Handle
    (
      __FeatureName__CreateRequest a__FeatureName__CreateRequest,
      CancellationToken aCancellationToken
    )
    {
      __FeatureName__Entity mapped__FeatureName__ = Mapper.Map<__FeatureName__Entity>(a__FeatureName__CreateRequest);
      DbContext.__FeatureName__Entities.Add(mapped__FeatureName__);
      await DbContext.SaveChangesAsync();
      var response = new __FeatureName__CreateResponse(a__FeatureName__CreateRequest.CorrelationId);

      return await Task.FromResult(response);
    }
  }
}
