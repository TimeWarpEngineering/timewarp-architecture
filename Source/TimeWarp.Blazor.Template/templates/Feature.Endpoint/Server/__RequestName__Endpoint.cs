namespace __RootNamespace__.Features.__FeatureName__s
{
  using Microsoft.AspNetCore.Mvc;
  using Swashbuckle.AspNetCore.Annotations;
  using System.Net;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;

  public class __RequestName__Endpoint : BaseEndpoint<__RequestName__Request, __RequestName__Response>
  {
    /// <summary>
    /// Your summary these comments will show in the Open API Docs
    /// </summary>
    /// <param name="a__RequestName__Request"><see cref="__RequestName__Request"/></param>
    /// <returns><see cref="__RequestName__Response"/></returns>
    [HttpGet(__RequestName__Request.RouteTemplate)]
    [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
    [ProducesResponseType(typeof(__RequestName__Response), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Process(__RequestName__Request a__RequestName__Request) => await Send(a__RequestName__Request);
  }
}
