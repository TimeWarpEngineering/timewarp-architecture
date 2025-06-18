namespace __RootNamespace__.Features.__FeatureName__s
{
  using Microsoft.AspNetCore.Mvc;
  using Swashbuckle.AspNetCore.Annotations;
  using System.Net;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;

  public class Upsert__FeatureName__Endpoint : BaseEndpoint<Upsert__FeatureName__Request, Upsert__FeatureName__Response>
  {
    /// <summary>
    /// Your summary these comments will show in the Open API Docs
    /// </summary>
    /// <param name="aUpsert__FeatureName__Request"><see cref="Upsert__FeatureName__Request"/></param>
    /// <returns><see cref="Upsert__FeatureName__Response"/></returns>
    [HttpPost(Upsert__FeatureName__Request.RouteTemplate)]
    [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
    [ProducesResponseType(typeof(Upsert__FeatureName__Response), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public Task<IActionResult> Process([FromBody]Upsert__FeatureName__Request aUpsert__FeatureName__Request) => 
      Send(aUpsert__FeatureName__Request);
  }
}
