namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  
  public class __FeatureName__Delete : IRequestHandler<__FeatureName__DeleteRequest, __FeatureName__DeleteResponse>
  {
    private readonly ApplicationDbContext DbContext;
    public __FeatureName__Delete(ApplicationDbContext aDbContext)
    {
      DbContext = aDbContext;
    }
    public async Task<__FeatureName__DeleteResponse> Handle
    (
      __FeatureName__DeleteRequest a__FeatureName__DeleteRequest,
      CancellationToken aCancellationToken
    )
    {
      var itemById = DbContext.__FeatureName__Entities.SingleOrDefault(i => i.Id == a__FeatureName__DeleteRequest.ItemId);
      DbContext.__FeatureName__Entities.Remove(itemById);
      DbContext.SaveChanges();
      var response = new __FeatureName__DeleteResponse(__FeatureName__DeleteRequest.CorrelationId);

      return await Task.FromResult(response);
    }
  }
}
