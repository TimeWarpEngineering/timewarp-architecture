namespace __RootNamespace__.Features.__FeatureName__s
{
  using Microsoft.AspNetCore.Mvc;
  using Swashbuckle.AspNetCore.Annotations;
  using System.Net;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;

  public class Delete__FeatureName__Endpoint : BaseEndpoint<Delete__FeatureName__Request, Delete__FeatureName__Response>
  {
    /// <summary>
    /// Your summary these comments will show in the Open API Docs
    /// </summary>
    /// <param name="aDelete__FeatureName__Request"><see cref="Delete__FeatureName__Request"/></param>
    /// <returns><see cref="Delete__FeatureName__Response"/></returns>
    [HttpDelete(Delete__FeatureName__Request.RouteTemplate)]
    [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
    [ProducesResponseType(typeof(Delete__FeatureName__Response), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public Task<IActionResult> Process([FromQuery]Delete__FeatureName__Request aDelete__FeatureName__Request) => 
      Send(aDelete__FeatureName__Request);
  }
}
