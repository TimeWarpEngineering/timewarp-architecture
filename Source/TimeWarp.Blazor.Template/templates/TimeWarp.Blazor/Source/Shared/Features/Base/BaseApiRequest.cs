namespace TimeWarp.Blazor.Features.Bases
{
  public abstract class BaseApiRequest : BaseRequest
  {
    internal abstract string GetRoute();
  }
}
