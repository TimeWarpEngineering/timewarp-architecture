namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  
  public class __FeatureName__GetHandler : IRequestHandler<__FeatureName__GetRequest, __FeatureName__GetResponse>
  {
    private readonly ApplicationDbContext DbContext;
    private IConfigurationProvider ConfigurationProvider;
    public __FeatureName__GetHandler
    (
        ApplicationDbContext aDbContext,
        IConfigurationProvider aConfigurationProvider
    )
    {
        ConfigurationProvider = aConfigurationProvider;
        DbContext = aDbContext;
    }

    public async Task<__FeatureName__ReadResponse> Handle
    (
      __FeatureName__GetRequest a__FeatureName__GetRequest,
      CancellationToken aCancellationToken
    )
    {
        __FeatureName__ReadResponse response;
        if (String.IsNullOrEmpty(a__FeatureName__GetRequest.PageIndex.ToString()) 
                && String.IsNullOrEmpty(a__FeatureName__GetRequest.PageSize.ToString()))
        {
            var response = new __FeatureName__ReadResponse(a__FeatureName__ReadRequest.CorrelationId) 
            {
                __FeatureName__s = await DbContext.__FeatureName__Entities.ProjectTo<__FeatureName__Dto>(ConfigurationProvider).ToListAsync()
            };
        } else
        {
            var response = new __FeatureName__ReadResponse(a__FeatureName__ReadRequest.CorrelationId)
            {
              __FeatureName__s = await DbContext.__FeatureName__Entities.ProjectTo<__FeatureName__Dto>(ConfigurationProvider).OrderBy(c => c.Name).Skip(a__FeatureName__GetRequest.PageSize * a__FeatureName__GetRequest.PageIndex).Take(a__FeatureName__GetRequest.PageSize).ToListAsync();
      };
        }

        return await Task.FromResult(response);
    }
  }
}
