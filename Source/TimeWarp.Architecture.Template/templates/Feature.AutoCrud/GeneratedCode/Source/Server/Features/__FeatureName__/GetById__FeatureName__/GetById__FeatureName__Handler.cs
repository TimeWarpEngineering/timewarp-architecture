namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Data;
  using __RootNamespace__.Models;
  using AutoMapper;

  public class GetById__FeatureName__Handler : IRequestHandler<GetById__FeatureName__Request, GetById__FeatureName__Response>
  {
    private readonly ApplicationDbContext DbContext;
    private IMapper Mapper;
    public GetById__FeatureName__Handler
    (
        ApplicationDbContext aDbContext,
        IMapper aMapper
    )
    {
        Mapper = aMapper;
        DbContext = aDbContext;
    }

    public async Task<GetById__FeatureName__Response> Handle
    (
      GetById__FeatureName__Request aGetById__FeatureName__Request,
      CancellationToken aCancellationToken
    )
    {
        __FeatureName__Entity __FeatureName__ = DbContext.__FeatureName__Entities.Find(aGetById__FeatureName__Request.Id);
        var response = new GetById__FeatureName__Response() 
        {
          __FeatureName__ = Mapper.Map<__FeatureName__Dto>(__FeatureName__)
        };
        return await Task.FromResult(response);
    }
  }
}
