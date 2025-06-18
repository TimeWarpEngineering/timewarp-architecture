namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Data;
  using __RootNamespace__.Models;

  public class Delete__FeatureName__Handler : IRequestHandler<Delete__FeatureName__Request, Delete__FeatureName__Response>
  {
    private readonly ApplicationDbContext DbContext;
    public Delete__FeatureName__Handler(ApplicationDbContext aDbContext)
    {
      DbContext = aDbContext;
    }
    public async Task<Delete__FeatureName__Response> Handle
    (
      Delete__FeatureName__Request aDelete__FeatureName__Request,
      CancellationToken aCancellationToken
    )
    {
      __FeatureName__Entity itemById = DbContext.__FeatureName__Entities.SingleOrDefault(i => i.Id.ToString().Contains(aDelete__FeatureName__Request.ItemId));
      DbContext.__FeatureName__Entities.Remove(itemById);
      DbContext.SaveChanges();
      var response = new Delete__FeatureName__Response();

      return await Task.FromResult(response);
    }
  }
}
