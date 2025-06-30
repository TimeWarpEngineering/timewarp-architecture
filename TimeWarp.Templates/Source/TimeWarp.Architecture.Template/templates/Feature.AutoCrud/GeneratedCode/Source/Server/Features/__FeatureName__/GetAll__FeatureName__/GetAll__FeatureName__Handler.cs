namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Data;
  using AutoMapper;
  using AutoMapper.QueryableExtensions;
  using Microsoft.EntityFrameworkCore;

  public class GetAll__FeatureName__Handler : IRequestHandler<GetAll__FeatureName__Request, GetAll__FeatureName__Response>
  {
    private readonly ApplicationDbContext DbContext;
    private IConfigurationProvider ConfigurationProvider;
    public GetAll__FeatureName__Handler
    (
        ApplicationDbContext aDbContext,
        IConfigurationProvider aConfigurationProvider
    )
    {
        ConfigurationProvider = aConfigurationProvider;
        DbContext = aDbContext;
    }

    public async Task<GetAll__FeatureName__Response> Handle
    (
      GetAll__FeatureName__Request aGetAll__FeatureName__Request,
      CancellationToken aCancellationToken
    )
    {
        GetAll__FeatureName__Response response;
        if (aGetAll__FeatureName__Request.PageIndex == 0 && aGetAll__FeatureName__Request.PageSize == 0)
        {
            response = new GetAll__FeatureName__Response() 
            {
                __FeatureName__s = await DbContext.__FeatureName__Entities.ProjectTo<__FeatureName__Dto>(ConfigurationProvider).ToListAsync()
            };
        } else
        {
            response = new GetAll__FeatureName__Response()
            {
                __FeatureName__s = await DbContext.__FeatureName__Entities.ProjectTo<__FeatureName__Dto>(ConfigurationProvider).Take(aGetAll__FeatureName__Request.PageSize).ToListAsync()
            };
        }
      return await Task.FromResult(response);
      }
  }
}

