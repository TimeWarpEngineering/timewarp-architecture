namespace __RootNamespace__.Features.__FeatureName__s
{
  using Microsoft.AspNetCore.Mvc;
  using Swashbuckle.AspNetCore.Annotations;
  using System.Net;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;

  public class GetAll__FeatureName__Endpoint : BaseEndpoint<GetAll__FeatureName__Request, GetAll__FeatureName__Response>
  {
    /// <summary>
    /// Your summary these comments will show in the Open API Docs
    /// </summary>
    /// <param name="aGetAll__FeatureName__Request"><see cref="GetAll__FeatureName__Request"/></param>
    /// <returns><see cref="GetAll__FeatureName__Response"/></returns>
    [HttpGet(GetAll__FeatureName__Request.RouteTemplate)]
    [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
    [ProducesResponseType(typeof(GetAll__FeatureName__Response), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public Task<IActionResult> Process(GetAll__FeatureName__Request aGetAll__FeatureName__Request) => 
      Send(aGetAll__FeatureName__Request);
  }
}
