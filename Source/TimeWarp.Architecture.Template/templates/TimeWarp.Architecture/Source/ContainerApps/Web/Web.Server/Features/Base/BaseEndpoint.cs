namespace TimeWarp.Architecture.Features.Bases;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

[ApiController]
[Produces("application/json")]
public class BaseEndpoint<TRequest, TResponse> : ControllerBase
  where TRequest : IRequest<TResponse>
  where TResponse : BaseResponse
{
  private ISender sender;

  protected ISender Sender => sender ??= HttpContext.RequestServices.GetService<ISender>();

  protected virtual async Task<IActionResult> Send(TRequest aRequest)
  {
    TResponse response = await Sender.Send(aRequest).ConfigureAwait(false);

    return Ok(response);
  }
}
