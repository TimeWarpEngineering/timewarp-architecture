namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Data;


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
      __FeatureName__CreateRequest a__FeatureName__UpsertRequest,
      CancellationToken aCancellationToken
    )
    {
      __FeatureName__CreateResponse response { get; set; }
      if (!String.IsNullOrEmpty(a__FeatureName__CreateRequest.Id.ToString())
      {
        var itemInDb = DbContext.__FeatureName__Entities.SingleOrDefault(i => i.Id == a__FeatureName__UpsertRequest.Id);
        itemInDb.Name = a__FeatureName__UpsertRequest.Name;
        itemInDb.Description = a__FeatureName__UpsertRequest.Description;
        itemInDb.Price = a__FeatureName__UpsertRequest.Price;
        DbContext.SaveChanges();
        response = new __FeatureName__CreateResponse(a__FeatureName__CreateRequest.CorrelationId);
      } else {
        __FeatureName__Entity mapped__FeatureName__ = Mapper.Map<__FeatureName__Entity>(a__FeatureName__CreateRequest);
        DbContext.__FeatureName__Entities.Add(mapped__FeatureName__);
        DbContext.SaveChanges();
        response = new __FeatureName__CreateResponse(a__FeatureName__CreateRequest.CorrelationId);
      }
      return await Task.FromResult(response);
    }
  }
}
