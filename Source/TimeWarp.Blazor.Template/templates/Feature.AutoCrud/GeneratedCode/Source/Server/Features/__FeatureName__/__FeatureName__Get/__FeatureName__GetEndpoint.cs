namespace __RootNamespace__.Features.__FeatureName__s
{
  using Microsoft.AspNetCore.Mvc;
  using Swashbuckle.AspNetCore.Annotations;
  using System.Net;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;

  public class __FeatureName__GetEndpoint : BaseEndpoint<__FeatureName__GetRequest, __FeatureName__GetResponse>
  {
    /// <summary>
    /// Your summary these comments will show in the Open API Docs
    /// </summary>
    /// <param name="a__FeatureName__GetRequest"><see cref="__FeatureName__GetRequest"/></param>
    /// <returns><see cref="__FeatureName__GetResponse"/></returns>
    [HttpGet(__FeatureName__GetRequest.RouteTemplate)]
    [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
    [ProducesResponseType(typeof(__FeatureName__GetResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public Task<IActionResult> Process(__FeatureName__GetRequest a__FeatureName__GetRequest) => 
      Send(a__FeatureName__GetRequest);
  }
}
