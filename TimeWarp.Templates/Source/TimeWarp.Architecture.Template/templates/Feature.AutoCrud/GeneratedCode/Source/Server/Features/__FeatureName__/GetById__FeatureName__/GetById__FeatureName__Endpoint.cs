namespace __RootNamespace__.Features.__FeatureName__s
{
  using Microsoft.AspNetCore.Mvc;
  using Swashbuckle.AspNetCore.Annotations;
  using System.Net;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;

  public class GetById__FeatureName__Endpoint : BaseEndpoint<GetById__FeatureName__Request, GetById__FeatureName__Response>
  {
    /// <summary>
    /// Your summary these comments will show in the Open API Docs
    /// </summary>
    /// <param name="aGetById__FeatureName__Request"><see cref="GetById__FeatureName__Request"/></param>
    /// <returns><see cref="GetById__FeatureName__Response"/></returns>
    [HttpGet(GetById__FeatureName__Request.RouteTemplate)]
    [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
    [ProducesResponseType(typeof(GetById__FeatureName__Response), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public Task<IActionResult> Process([FromQuery]GetById__FeatureName__Request aGetById__FeatureName__Request) => 
      Send(aGetById__FeatureName__Request);
  }
}
