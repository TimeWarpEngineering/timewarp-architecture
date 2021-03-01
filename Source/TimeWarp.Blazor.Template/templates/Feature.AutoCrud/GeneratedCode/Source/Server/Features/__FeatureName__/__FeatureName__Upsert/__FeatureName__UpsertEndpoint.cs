namespace __RootNamespace__.Features.__FeatureName__s
{
  using Microsoft.AspNetCore.Mvc;
  using Swashbuckle.AspNetCore.Annotations;
  using System.Net;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;

  public class __FeatureName__UpsertEndpoint : BaseEndpoint<__FeatureName__UpsertRequest, __FeatureName__UpsertResponse>
  {
    /// <summary>
    /// Your summary these comments will show in the Open API Docs
    /// </summary>
    /// <param name="a__FeatureName__UpsertRequest"><see cref="__FeatureName__UpsertRequest"/></param>
    /// <returns><see cref="__FeatureName__UpsertResponse"/></returns>
    [HttpPost(__FeatureName__UpsertRequest.RouteTemplate)]
    [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
    [ProducesResponseType(typeof(__FeatureName__UpsertResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public Task<IActionResult> Process([FromBody] __FeatureName__UpsertRequest a__FeatureName__UpsertRequest) => 
      Send(a__FeatureName__UpsertRequest);
  }
}
