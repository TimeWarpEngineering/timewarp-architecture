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
  using __RootNamespace__.Models;


  public class Upsert__FeatureName__Handler : IRequestHandler<Upsert__FeatureName__Request, Upsert__FeatureName__Response>
  {
    private readonly ApplicationDbContext DbContext;
    private readonly IMapper Mapper;

    public Upsert__FeatureName__Handler(
    ApplicationDbContext aDbContext,
    IMapper aMapper
    )
    {
      Mapper = aMapper;
      DbContext = aDbContext;
    }

    public async Task<Upsert__FeatureName__Response> Handle
    (
      Upsert__FeatureName__Request aUpsert__FeatureName__Request,
      CancellationToken aCancellationToken
    )
    {
      Upsert__FeatureName__Response response;
      if (aUpsert__FeatureName__Request.Id != Guid.Empty)
      {
        __FeatureName__Entity itemInDb = DbContext.__FeatureName__Entities.SingleOrDefault(i => i.Id.ToString().Contains(aUpsert__FeatureName__Request.Id.ToString()));
        itemInDb.Name = aUpsert__FeatureName__Request.Name;
        itemInDb.Description = aUpsert__FeatureName__Request.Description;
        itemInDb.Price = aUpsert__FeatureName__Request.Price;
        DbContext.SaveChanges();
        response = new Upsert__FeatureName__Response(aUpsert__FeatureName__Request.CorrelationId);
      } else {
        __FeatureName__Entity mapped__FeatureName__ = Mapper.Map<__FeatureName__Entity>(aUpsert__FeatureName__Request);
        DbContext.__FeatureName__Entities.Add(mapped__FeatureName__);
        DbContext.SaveChanges();
        response = new Upsert__FeatureName__Response(aUpsert__FeatureName__Request.CorrelationId);
      }
      return await Task.FromResult(response);
    }
  }
}
