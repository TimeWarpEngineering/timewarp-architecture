namespace __RootNamespace__.Features.__FeatureName__s
{
  using Microsoft.AspNetCore.Mvc;
  using Swashbuckle.AspNetCore.Annotations;
  using System.Net;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;

  public class __FeatureName__CreateEndpoint : BaseEndpoint<__FeatureName__CreateRequest, __FeatureName__CreateResponse>
  {
    /// <summary>
    /// Your summary these comments will show in the Open API Docs
    /// </summary>
    /// <param name="a__FeatureName__CreateRequest"><see cref="__FeatureName__CreateRequest"/></param>
    /// <returns><see cref="__FeatureName__CreateResponse"/></returns>
    [HttpPost(__FeatureName__CreateRequest.RouteTemplate)]
    [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
    [ProducesResponseType(typeof(__FeatureName__CreateResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public Task<IActionResult> Process([FromBody]__FeatureName__CreateRequest a__FeatureName__CreateRequest) => 
      Send(a__FeatureName__CreateRequest);
  }
}
